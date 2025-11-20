using MediatR;
using Application.DTOs;
using Transational.Api.Domain.Common;

namespace Application.Queries.GetPayment;

public record GetPaymentQuery(Guid ExternalOperationId) : IRequest<Result<PaymentResponse>>;
