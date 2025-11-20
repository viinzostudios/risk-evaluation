namespace Transational.Api.Domain.Entities;

public partial class Payment
{
    public int Id { get; set; }

    public byte[] ExternalOperationId { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public byte[] CustomerId { get; set; } = null!;

    public byte[] ServiceProviderId { get; set; } = null!;

    public decimal Amount { get; set; }

    public int PaymentMethodId { get; set; }

    public int PaymentStatusId { get; set; }

    public virtual PaymentMethod PaymentMethod { get; set; } = null!;

    public virtual PaymentStatus PaymentStatus { get; set; } = null!;
}
