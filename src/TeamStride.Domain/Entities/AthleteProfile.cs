using TeamStride.Domain.Common;

namespace TeamStride.Domain.Entities;

public class AthleteProfile : AuditedEntity<Guid>
{
    public required Guid AthleteId { get; set; }
    public string? PreferredEvents { get; set; }
    public string? PersonalBests { get; set; }
    public string? Goals { get; set; }
    public string? TrainingNotes { get; set; }
    public string? MedicalNotes { get; set; }
    public string? DietaryRestrictions { get; set; }
    public string? UniformSize { get; set; }
    public string? WarmupRoutine { get; set; }

    // Navigation property
    public virtual Athlete Athlete { get; set; } = null!;
} 