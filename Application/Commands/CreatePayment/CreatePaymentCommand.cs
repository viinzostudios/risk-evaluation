using MediatR;
using Application.DTOs;
using Transational.Api.Domain.Common;

namespace Application.Commands.CreatePayment;

public record CreatePaymentCommand(
    Guid CustomerId,
    Guid ServiceProviderId,
    int PaymentMethodId,
    decimal Amount) : IRequest<Result<PaymentResponse>>;
