using TeamStride.Application.Users.Dtos;

namespace TeamStride.Application.Teams.Dtos;

public class UserTeamDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TeamId { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime JoinedOn { get; set; }
    
    // Navigation properties
    public UserDto? User { get; set; }
    public TeamDto? Team { get; set; }
} 