namespace TeamStride.Domain.Common;

public abstract class TenantEntity<TKey> : Entity<TKey>, IHasTenant where TKey : struct
{
    public Guid? TenantId { get; set; }
} 