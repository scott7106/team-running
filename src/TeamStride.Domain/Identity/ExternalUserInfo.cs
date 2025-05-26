namespace TeamStride.Domain.Identity;

public class ExternalUserInfo
{
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string ProviderId { get; set; }
} 