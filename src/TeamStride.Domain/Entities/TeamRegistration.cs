using TeamStride.Domain.Common;
using TeamStride.Domain.Identity;

namespace TeamStride.Domain.Entities;

public class TeamRegistration : AuditedEntity<Guid>
{
    public Guid TeamId { get; set; }
    public Guid? UserId { get; set; }
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string EmergencyContactName { get; set; } = null!;
    public string EmergencyContactPhone { get; set; } = null!;
    public bool CodeOfConductAccepted { get; set; }
    public DateTime CodeOfConductAcceptedOn { get; set; }
    public RegistrationStatus Status { get; set; }
    
    // Navigation properties
    public virtual Team Team { get; set; } = null!;
    public virtual ApplicationUser? User { get; set; }
    public virtual ICollection<AthleteRegistration> Athletes { get; set; } = new List<AthleteRegistration>();
}

public enum RegistrationStatus
{
    Pending,
    Approved,
    Rejected,
    Waitlisted
} 