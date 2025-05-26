namespace TeamStride.Domain.Interfaces;

public interface ITenantService
{
    Guid CurrentTenantId { get; }
    string? CurrentTenantSubdomain { get; }
    void SetCurrentTenant(Guid tenantId);
    void SetCurrentTenant(string subdomain);
    void ClearCurrentTenant();
} 