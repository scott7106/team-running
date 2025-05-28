using TeamStride.Domain.Entities;

namespace TeamStride.Application.Teams.Dtos;

public class TeamMemberDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public TeamRole Role { get; set; }
    public DateTime JoinedOn { get; set; }
    public bool IsActive { get; set; }
    public bool IsOwner { get; set; }
} 