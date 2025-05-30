using TeamStride.Domain.Entities;

namespace TeamStride.Application.Users.Dtos;

/// <summary>
/// Summary information about a user's team membership.
/// </summary>
public class UserTeamSummaryDto
{
    public Guid UserTeamId { get; set; }
    public Guid TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string TeamSubdomain { get; set; } = string.Empty;
    public TeamRole Role { get; set; }
    public MemberType MemberType { get; set; }
    public bool IsActive { get; set; }
    public DateTime JoinedOn { get; set; }
} 