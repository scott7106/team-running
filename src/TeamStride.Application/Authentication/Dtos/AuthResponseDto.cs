using TeamStride.Domain.Entities;

namespace TeamStride.Application.Authentication.Dtos;

public class TeamMembershipDto
{
    public required Guid TeamId { get; set; }
    public required TeamRole TeamRole { get; set; }
    public required MemberType MemberType { get; set; }
}

public class AuthResponseDto
{
    public required string Token { get; set; }
    public required string RefreshToken { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public bool IsGlobalAdmin { get; set; }
    public List<TeamMembershipDto> Teams { get; set; } = new();
    public bool RequiresEmailConfirmation { get; set; }
} 