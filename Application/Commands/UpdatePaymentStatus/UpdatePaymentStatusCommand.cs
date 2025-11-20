using MediatR;
using Transational.Api.Domain.Common;

namespace Application.Commands.UpdatePaymentStatus;

public record UpdatePaymentStatusCommand(
    Guid ExternalOperationId,
    string Status) : IRequest<Result<bool>>;
