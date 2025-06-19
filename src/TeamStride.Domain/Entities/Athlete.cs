using TeamStride.Domain.Common;
using TeamStride.Domain.Identity;

namespace TeamStride.Domain.Entities;

public class Athlete : AuditedTeamEntity<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public AthleteRole Role { get; set; }
    public Gender Gender { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public DateTime DateOfBirth { get; set; }
    public GradeLevel GradeLevel { get; set; }
    public bool HasPhysicalOnFile { get; set; }
    public bool HasWaiverSigned { get; set; }

    // Navigation properties
    public virtual ApplicationUser? User { get; set; }
    public virtual AthleteProfile? Profile { get; set; }
}

public enum AthleteRole
{
    Athlete,
    Captain
}

public enum Gender
{
    Female = 1,
    Male = 2,
    NS = 3
}

public enum GradeLevel
{
    K = 0,
    First = 1,
    Second = 2,
    Third = 3,
    Fourth = 4,
    Fifth = 5,
    Sixth = 6,
    Seventh = 7,
    Eighth = 8,
    Ninth = 9,
    Tenth = 10,
    Eleventh = 11,
    Twelfth = 12,
    Other = 13,
    Redshirt = 20,
    Freshman = 21,
    Sophomore = 22,
    Junior = 23,
    Senior = 24
} 