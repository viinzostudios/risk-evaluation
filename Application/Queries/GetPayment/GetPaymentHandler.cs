using Application.DTOs;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Transational.Api.Domain.Common;
using Transational.Api.Domain.Interfaces;

namespace Application.Queries.GetPayment;

public class GetPaymentHandler : IRequestHandler<GetPaymentQuery, Result<PaymentResponse>>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetPaymentHandler> _logger;
    public GetPaymentHandler(IPaymentRepository paymentRepository, IMapper mapper, ILogger<GetPaymentHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<PaymentResponse>> Handle(
        GetPaymentQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting payment with ExternalOperationId: {ExternalOperationId}",
                request.ExternalOperationId);

            var payment = await _paymentRepository.GetByExternalIdAsync(
                request.ExternalOperationId,
                cancellationToken);

            if (payment == null)
            {
                _logger.LogWarning("Payment not found: {ExternalOperationId}", request.ExternalOperationId);
                return Result<PaymentResponse>.NotFound("Payment not found");
            }

            var response = _mapper.Map<PaymentResponse>(payment);

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment");
            return Result.Failure<PaymentResponse>(ex.Message);
        }
    }
}
