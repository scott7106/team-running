using TeamStride.Domain.Entities;

namespace TeamStride.Application.Authentication.Dtos;

public class AuthResponseDto
{
    public required string Token { get; set; }
    public required string RefreshToken { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public Guid? TeamId { get; set; }
    public TeamRole? Role { get; set; }
    public bool RequiresEmailConfirmation { get; set; }
} 