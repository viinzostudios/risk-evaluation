using MediatR;
using Microsoft.AspNetCore.Mvc;
using Application.Commands.CreatePayment;
using Application.DTOs;
using Application.Queries.GetPayment;
using Transational.Api.Web.Models;

namespace Transational.Api.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IMediator mediator, ILogger<PaymentsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid payment request received");
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Creating payment for CustomerId: {CustomerId}, Amount: {Amount}",
            request.CustomerId, request.Amount);

        var command = new CreatePaymentCommand(
            request.CustomerId,
            request.ServiceProviderId,
            request.PaymentMethodId,
            request.Amount);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Failed to create payment: {Error}", result.Error);
            return BadRequest(new { message = "Failed to create payment", error = result.Error });
        }

        _logger.LogInformation("Payment created successfully with ExternalOperationId: {ExternalOperationId}",
            result.Value.ExternalOperationId);

        return CreatedAtAction(
            nameof(GetPayment),
            new { externalOperationId = result.Value.ExternalOperationId },
            result.Value);
    }

    [HttpGet("{externalOperationId:guid}")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPayment(Guid externalOperationId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting payment with ExternalOperationId: {ExternalOperationId}",
            externalOperationId);

        var query = new GetPaymentQuery(externalOperationId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Payment not found: {ExternalOperationId}", externalOperationId);
            return NotFound(new {
                message = $"Payment with external operation ID '{externalOperationId}' not found",
                externalOperationId
            });
        }

        return Ok(result.Value);
    }
}
