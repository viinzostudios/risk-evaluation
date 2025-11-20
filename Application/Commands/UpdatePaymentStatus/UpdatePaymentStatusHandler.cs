using MediatR;
using Microsoft.Extensions.Logging;
using Transational.Api.Domain.Common;
using Transational.Api.Domain.Interfaces;

namespace Application.Commands.UpdatePaymentStatus;

/// <summary>
/// Handler that updates payment status based on risk evaluation response
/// Maps string status ("accepted"/"denied") to PaymentStatusId (2/3)
/// </summary>
public class UpdatePaymentStatusHandler : IRequestHandler<UpdatePaymentStatusCommand, Result<bool>>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<UpdatePaymentStatusHandler> _logger;

    public UpdatePaymentStatusHandler(
        IPaymentRepository paymentRepository,
        ILogger<UpdatePaymentStatusHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(
        UpdatePaymentStatusCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Updating payment status for {ExternalOperationId} to {Status}",
            request.ExternalOperationId,
            request.Status);

        // Find payment by ExternalOperationId
        var payment = await _paymentRepository.GetByExternalIdAsync(
            request.ExternalOperationId,
            cancellationToken);

        if (payment == null)
        {
            _logger.LogWarning(
                "Payment not found with ExternalOperationId: {ExternalOperationId}",
                request.ExternalOperationId);
            return Result<bool>.NotFound("Payment not found");
        }

        // Map string status to PaymentStatusId
        // 1 = evaluating, 2 = accepted, 3 = denied
        var newStatusId = request.Status.ToLower() switch
        {
            "accepted" => 2,
            "denied" => 3,
            _ => throw new ArgumentException($"Invalid status: {request.Status}")
        };

        // Update payment status
        payment.PaymentStatusId = newStatusId;
        payment.UpdatedAt = DateTime.UtcNow;

        await _paymentRepository.UpdateAsync(payment, cancellationToken);

        _logger.LogInformation(
            "Payment {ExternalOperationId} updated successfully to status {StatusId}",
            request.ExternalOperationId,
            newStatusId);

        return Result.Success(true);
    }
}
