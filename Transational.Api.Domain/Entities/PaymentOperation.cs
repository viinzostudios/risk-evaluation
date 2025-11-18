using Transational.Api.Domain.Common;
using Transational.Api.Domain.Enums;

namespace Transational.Api.Domain.Entities;

/// <summary>
/// Payment operation aggregate root
/// </summary>
public class PaymentOperation : EntityBase
{
    public Guid ExternalOperationId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid ServiceProviderId { get; private set; }
    public int PaymentMethodId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; }

    // Private constructor for EF Core
    private PaymentOperation()
    {
    }

    /// <summary>
    /// Creates a new payment operation
    /// </summary>
    public PaymentOperation(
        Guid customerId,
        Guid serviceProviderId,
        int paymentMethodId,
        decimal amount)
    {
        // Domain validations
        Ensure.NotEmpty(customerId, nameof(customerId));
        Ensure.NotEmpty(serviceProviderId, nameof(serviceProviderId));
        Ensure.GreaterThanZero(paymentMethodId, nameof(paymentMethodId));
        Ensure.GreaterThanZero(amount, nameof(amount));

        // Business rule: Amount cannot exceed 2000 (will be rejected by risk service)
        // This is just a domain constraint, not blocking creation

        ExternalOperationId = Guid.NewGuid();
        CustomerId = customerId;
        ServiceProviderId = serviceProviderId;
        PaymentMethodId = paymentMethodId;
        Amount = amount;
        Status = PaymentStatus.Evaluating;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the payment status based on risk evaluation
    /// </summary>
    public void UpdateStatus(PaymentStatus newStatus)
    {
        if (Status == newStatus)
            return;

        // Business rule: Cannot change status if already in final state
        if (IsInFinalState())
            throw new DomainException($"Payment is already in final state: {Status}");

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if payment is in a final state (accepted or denied)
    /// </summary>
    public bool IsInFinalState() =>
        Status == PaymentStatus.Accepted || Status == PaymentStatus.Denied;
}
