using AutoMapper;
using MediatR;
using Application.DTOs;
using Transational.Api.Domain.Common;
using Transational.Api.Domain.Interfaces;
using KafkaClient.Service.Interfaces;

namespace Application.Queries.GetPayment;

public class GetPaymentHandler : IRequestHandler<GetPaymentQuery, Result<PaymentResponse>>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IMapper _mapper;

    public GetPaymentHandler(IPaymentRepository paymentRepository, IMapper mapper)
    {
        _paymentRepository = paymentRepository;
        _mapper = mapper;
    }

    public async Task<Result<PaymentResponse>> Handle(
        GetPaymentQuery request,
        CancellationToken cancellationToken)
    {
        // Get payment from database
        var payment = await _paymentRepository.GetByExternalIdAsync(
            request.ExternalOperationId,
            cancellationToken);

        if (payment == null)
        {
            return Result<PaymentResponse>.NotFound("Payment not found");
        }

        // Map to response DTO
        var response = _mapper.Map<PaymentResponse>(payment);

        return Result.Success(response);
    }
}
