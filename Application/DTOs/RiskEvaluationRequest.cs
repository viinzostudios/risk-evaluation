namespace Application.DTOs;

public class RiskEvaluationRequest
{
    public Guid ExternalOperationId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Amount { get; set; }
}
