using TeamStride.Application.Users.Dtos;

namespace TeamStride.Application.Tenants.Dtos;

public class UserTenantDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime JoinedOn { get; set; }
    
    // Navigation properties
    public UserDto? User { get; set; }
    public TenantDto? Tenant { get; set; }
} 