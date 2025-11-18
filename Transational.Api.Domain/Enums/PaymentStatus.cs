namespace Transational.Api.Domain.Enums;

/// <summary>
/// Payment operation status
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Payment is being evaluated by risk service
    /// </summary>
    Evaluating = 1,

    /// <summary>
    /// Payment was accepted
    /// </summary>
    Accepted = 2,

    /// <summary>
    /// Payment was denied
    /// </summary>
    Denied = 3
}
