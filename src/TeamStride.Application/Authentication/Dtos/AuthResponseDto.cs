namespace TeamStride.Application.Authentication.Dtos;

public class AuthResponseDto
{
    public required string Token { get; set; }
    public required string RefreshToken { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string TenantId { get; set; }
    public required string Role { get; set; }
    public bool RequiresEmailConfirmation { get; set; }
} 