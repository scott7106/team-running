using TeamStride.Domain.Common;

namespace TeamStride.Domain.Entities;

public class AthleteRegistration : AuditedEntity<Guid>
{
    public Guid RegistrationId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateTime Birthdate { get; set; }
    public string GradeLevel { get; set; } = null!;
    
    // Navigation properties
    public virtual TeamRegistration Registration { get; set; } = null!;
} 