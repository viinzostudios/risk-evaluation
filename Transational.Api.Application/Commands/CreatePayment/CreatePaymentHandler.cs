using AutoMapper;
using MediatR;
using Transational.Api.Application.DTOs;
using Transational.Api.Domain.Common;
using Transational.Api.Domain.Entities;
using Transational.Api.Domain.Interfaces;

namespace Transational.Api.Application.Commands.CreatePayment;

public class CreatePaymentHandler : IRequestHandler<CreatePaymentCommand, Result<PaymentResponse>>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IMapper _mapper;

    public CreatePaymentHandler(IPaymentRepository paymentRepository, IMapper mapper)
    {
        _paymentRepository = paymentRepository;
        _mapper = mapper;
    }

    public async Task<Result<PaymentResponse>> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {

        var payment = _mapper.Map<Payment>(request);
        payment.ExternalOperationId = Guid.NewGuid().ToByteArray();
        payment.PaymentStatusId = 1; // evaluating
        payment.CreatedAt = DateTime.UtcNow;

        var createdPayment = await _paymentRepository.AddAsync(payment, cancellationToken);
        var response = _mapper.Map<PaymentResponse>(createdPayment);

        return Result.Success(response);
    }
}
