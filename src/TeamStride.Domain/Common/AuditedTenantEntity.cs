namespace TeamStride.Domain.Common;

public abstract class AuditedTenantEntity<TKey> : AuditedEntity<TKey>, IHasTenant where TKey : struct
{
    public Guid? TenantId { get; set; }
} 