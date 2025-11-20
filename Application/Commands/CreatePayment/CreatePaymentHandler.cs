using Application.DTOs;
using AutoMapper;
using Azure;
using KafkaClient.Service.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using Transational.Api.Domain.Common;
using Transational.Api.Domain.Entities;
using Transational.Api.Domain.Interfaces;

namespace Application.Commands.CreatePayment;

public class CreatePaymentHandler : IRequestHandler<CreatePaymentCommand, Result<PaymentResponse>>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IMapper _mapper;
    private readonly IKafkaClient _kafkaClient;
    private readonly ILogger<CreatePaymentHandler> _logger;
    public CreatePaymentHandler(IPaymentRepository paymentRepository, IMapper mapper, IKafkaClient kafkaClient, ILogger<CreatePaymentHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _mapper = mapper;
        _kafkaClient = kafkaClient;
        _logger = logger;
    }

    public async Task<Result<PaymentResponse>> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating payment for CustomerId: {CustomerId}, Amount: {Amount}", request.CustomerId, request.Amount);

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

            _logger.LogInformation("Payment created successfully with ExternalOperationId: {ExternalOperationId}", response.ExternalOperationId);
            
            return Result.Success(response);
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "Error creating payment");
            return Result.Failure<PaymentResponse>(ex.Message);
        }
    }
}
