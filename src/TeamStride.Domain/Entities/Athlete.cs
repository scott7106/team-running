using TeamStride.Domain.Common;
using TeamStride.Domain.Identity;

namespace TeamStride.Domain.Entities;

public class Athlete : AuditedTenantEntity<Guid>
{
    public required Guid UserId { get; set; }
    public AthleteRole Role { get; set; }
    public string? JerseyNumber { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Grade { get; set; }
    public bool HasPhysicalOnFile { get; set; }
    public bool HasWaiverSigned { get; set; }

    // Navigation properties
    public virtual ApplicationUser User { get; set; } = null!;
    public virtual AthleteProfile Profile { get; set; } = null!;
}

public enum AthleteRole
{
    Athlete,
    Captain
} 