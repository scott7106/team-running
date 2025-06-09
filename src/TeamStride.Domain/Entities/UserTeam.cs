using TeamStride.Domain.Common;
using TeamStride.Domain.Identity;
namespace TeamStride.Domain.Entities;

public class UserTeam : AuditedEntity<Guid>
{
    public Guid UserId { get; set; }
    public Guid TeamId { get; set; }
    public TeamRole Role { get; set; }
    public MemberType MemberType { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public DateTime JoinedOn { get; set; }
    
    // Navigation properties
    public virtual Team? Team { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;
}

/// <summary>
/// Simplified 3-tier authorization model for access control
/// </summary>
public enum TeamRole
{   
    /// <summary>
    /// Team owner with full control over the team, including ownership transfer and billing
    /// </summary>
    TeamOwner,
    
    /// <summary>
    /// Team administrator with management access but cannot transfer ownership or manage billing
    /// </summary>
    TeamAdmin,
    
    /// <summary>
    /// Team member with view-only access, permissions determined by MemberType
    /// </summary>
    TeamMember
}

/// <summary>
/// Business logic classification separate from authorization roles
/// </summary>
public enum MemberType
{
    /// <summary>
    /// Coaching staff with access to manage team activities, athletes, and training plans
    /// </summary>
    Coach,
    
    /// <summary>
    /// Team athlete with access to view training plans, schedules, and personal results
    /// </summary>
    Athlete,
    
    /// <summary>
    /// Parent/guardian with access to view rosters, schedules, and payment information
    /// </summary>
    Parent
} 