namespace Application.DTOs;

public class RiskEvaluationResponse
{
    public Guid ExternalOperationId { get; set; }
    public string Status { get; set; } = string.Empty; // "accepted" or "denied"
}
