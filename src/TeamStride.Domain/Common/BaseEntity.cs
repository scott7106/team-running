namespace TeamStride.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }
} 