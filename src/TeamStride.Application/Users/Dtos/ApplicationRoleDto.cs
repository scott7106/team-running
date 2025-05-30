namespace TeamStride.Application.Users.Dtos;

/// <summary>
/// DTO representing an application role.
/// </summary>
public class ApplicationRoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? NormalizedName { get; set; }
    
    /// <summary>
    /// Number of users currently assigned to this role.
    /// </summary>
    public int UserCount { get; set; }
} 