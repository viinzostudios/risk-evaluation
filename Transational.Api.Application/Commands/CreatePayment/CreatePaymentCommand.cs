using MediatR;
using Transational.Api.Application.DTOs;
using Transational.Api.Domain.Common;

namespace Transational.Api.Application.Commands.CreatePayment;

public record CreatePaymentCommand(
    Guid CustomerId,
    Guid ServiceProviderId,
    int PaymentMethodId,
    decimal Amount) : IRequest<Result<PaymentResponse>>;
