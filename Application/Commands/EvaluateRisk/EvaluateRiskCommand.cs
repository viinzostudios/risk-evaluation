using Application.DTOs;
using MediatR;
using Transational.Api.Domain.Common;

namespace Application.Commands.EvaluateRisk;

public record EvaluateRiskCommand(
    Guid ExternalOperationId,
    Guid CustomerId,
    decimal Amount) : IRequest<Result<RiskEvaluationResponse>>;
