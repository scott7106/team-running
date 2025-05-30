using TeamStride.Domain.Identity;

namespace TeamStride.Application.Users.Dtos;

/// <summary>
/// DTO for deleted users that can be recovered.
/// </summary>
public class DeletedUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserStatus Status { get; set; }
    public List<string> ApplicationRoles { get; set; } = new();
    public bool IsGlobalAdmin { get; set; }
    
    // Deletion information
    public DateTime DeletedOn { get; set; }
    public Guid? DeletedBy { get; set; }
    public string? DeletedByName { get; set; }
    
    // Original creation information
    public DateTime CreatedOn { get; set; }
    public DateTime? LastLoginOn { get; set; }
    
    // Team memberships at time of deletion
    public int TeamCount { get; set; }
    public List<string> TeamNames { get; set; } = new();
} 