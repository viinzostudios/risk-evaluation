namespace Transational.Api.Domain.Common;

/// <summary>
/// Base class for all entities with common properties
/// </summary>
public abstract class EntityBase
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
