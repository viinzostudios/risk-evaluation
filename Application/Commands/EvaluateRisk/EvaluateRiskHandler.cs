using Application.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;
using Transational.Api.Domain.Common;
using Transational.Api.Domain.Interfaces;

namespace Application.Commands.EvaluateRisk;

public class EvaluateRiskHandler : IRequestHandler<EvaluateRiskCommand, Result<RiskEvaluationResponse>>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<EvaluateRiskHandler> _logger;

    public EvaluateRiskHandler(
        IPaymentRepository paymentRepository,
        ILogger<EvaluateRiskHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _logger = logger;
    }

    public async Task<Result<RiskEvaluationResponse>> Handle(
        EvaluateRiskCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Evaluating risk for payment {ExternalOperationId}, Customer {CustomerId}, Amount {Amount}",
            request.ExternalOperationId,
            request.CustomerId,
            request.Amount);

        var status = await EvaluateAsync(request, cancellationToken);

        var response = new RiskEvaluationResponse
        {
            ExternalOperationId = request.ExternalOperationId,
            Status = status
        };

        _logger.LogInformation(
            "Risk evaluation completed for payment {ExternalOperationId}: {Status}",
            request.ExternalOperationId,
            status);

        return Result.Success(response);
    }

    private async Task<string> EvaluateAsync(
        EvaluateRiskCommand request,
        CancellationToken cancellationToken)
    {
        // Rule 1: Amount > 2000 -> denied
        if (request.Amount > 2000)
        {
            _logger.LogInformation(
                "Payment {ExternalOperationId} denied: Amount {Amount} exceeds limit of 2000",
                request.ExternalOperationId,
                request.Amount);
            return "denied";
        }

        // Rule 2: Daily accumulated amount per customer > 5000 -> denied
        var dailyTotal = await _paymentRepository.GetDailyTotalByCustomerAsync(
            request.CustomerId,
            DateTime.UtcNow.Date,
            cancellationToken);

        if (dailyTotal + request.Amount > 5000)
        {
            _logger.LogInformation(
                "Payment {ExternalOperationId} denied: Daily total {DailyTotal} + Amount {Amount} exceeds limit of 5000",
                request.ExternalOperationId,
                dailyTotal,
                request.Amount);
            return "denied";
        }

        // All rules passed -> accepted
        _logger.LogInformation(
            "Payment {ExternalOperationId} accepted: Amount {Amount}, Daily total {DailyTotal}",
            request.ExternalOperationId,
            request.Amount,
            dailyTotal);

        return "accepted";
    }
}
