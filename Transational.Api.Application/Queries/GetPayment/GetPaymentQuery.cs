using MediatR;
using Transational.Api.Application.DTOs;
using Transational.Api.Domain.Common;

namespace Transational.Api.Application.Queries.GetPayment;

public record GetPaymentQuery(Guid ExternalOperationId) : IRequest<Result<PaymentResponse>>;
