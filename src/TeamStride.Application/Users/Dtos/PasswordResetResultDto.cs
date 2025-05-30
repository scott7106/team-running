namespace TeamStride.Application.Users.Dtos;

/// <summary>
/// Result of a password reset operation.
/// </summary>
public class PasswordResetResultDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    
    /// <summary>
    /// The temporary password that was set. Only populated if explicitly requested.
    /// </summary>
    public string? TemporaryPassword { get; set; }
    
    /// <summary>
    /// Whether the user is required to change the password on next login.
    /// </summary>
    public bool RequirePasswordChange { get; set; }
    
    /// <summary>
    /// Whether the password was sent via email.
    /// </summary>
    public bool PasswordSentByEmail { get; set; }
    
    /// <summary>
    /// Whether the password was sent via SMS.
    /// </summary>
    public bool PasswordSentBySms { get; set; }
    
    /// <summary>
    /// Timestamp when the password was reset.
    /// </summary>
    public DateTime ResetTimestamp { get; set; }
} 