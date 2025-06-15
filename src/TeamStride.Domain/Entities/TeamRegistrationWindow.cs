using TeamStride.Domain.Common;

namespace TeamStride.Domain.Entities;

public class TeamRegistrationWindow : AuditedEntity<Guid>
{
    public Guid TeamId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int MaxRegistrations { get; set; }
    public string RegistrationPasscode { get; set; } = null!;
    public bool IsActive { get; set; }
    
    // Navigation properties
    public virtual Team Team { get; set; } = null!;
    public virtual ICollection<TeamRegistration> Registrations { get; set; } = new List<TeamRegistration>();
} 