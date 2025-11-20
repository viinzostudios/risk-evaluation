using Application.DTOs;
using AutoMapper;
using KafkaClient.Service.Interfaces;
using MediatR;
using Transational.Api.Domain.Common;
using Transational.Api.Domain.Entities;
using Transational.Api.Domain.Interfaces;

namespace Application.Commands.CreatePayment;

public class CreatePaymentHandler : IRequestHandler<CreatePaymentCommand, Result<PaymentResponse>>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IMapper _mapper;
    private readonly IKafkaClient _kafkaClient;

    public CreatePaymentHandler(IPaymentRepository paymentRepository, IMapper mapper, IKafkaClient kafkaClient)
    {
        _paymentRepository = paymentRepository;
        _mapper = mapper;
        _kafkaClient = kafkaClient;
    }

    public async Task<Result<PaymentResponse>> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = _mapper.Map<Payment>(request);
        var externalOperationId = Guid.NewGuid();
        payment.ExternalOperationId = externalOperationId.ToByteArray();
        payment.PaymentStatusId = 1; // evaluating
        payment.CreatedAt = DateTime.UtcNow;

        var createdPayment = await _paymentRepository.AddAsync(payment, cancellationToken);
        var response = _mapper.Map<PaymentResponse>(createdPayment);

        var riskRequest = new RiskEvaluationRequest
        {
            ExternalOperationId = externalOperationId,
            CustomerId = request.CustomerId,
            Amount = request.Amount
        };

        await _kafkaClient.PublishAsync("risk-evaluation-request", riskRequest);

        return Result.Success(response);
    }
}
