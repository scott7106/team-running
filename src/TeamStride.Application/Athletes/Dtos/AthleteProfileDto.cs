namespace TeamStride.Application.Athletes.Dtos;

public class AthleteProfileDto
{
    public Guid Id { get; set; }
    public Guid AthleteId { get; set; }
    public string? PreferredEvents { get; set; }
    public string? PersonalBests { get; set; }
    public string? Goals { get; set; }
    public string? TrainingNotes { get; set; }
    public string? MedicalNotes { get; set; }
    public string? DietaryRestrictions { get; set; }
    public string? UniformSize { get; set; }
    public string? WarmupRoutine { get; set; }
}

public class CreateAthleteProfileDto
{
    public string? PreferredEvents { get; set; }
    public string? PersonalBests { get; set; }
    public string? Goals { get; set; }
    public string? TrainingNotes { get; set; }
    public string? MedicalNotes { get; set; }
    public string? DietaryRestrictions { get; set; }
    public string? UniformSize { get; set; }
    public string? WarmupRoutine { get; set; }
}

public class UpdateAthleteProfileDto
{
    public string? PreferredEvents { get; set; }
    public string? PersonalBests { get; set; }
    public string? Goals { get; set; }
    public string? TrainingNotes { get; set; }
    public string? MedicalNotes { get; set; }
    public string? DietaryRestrictions { get; set; }
    public string? UniformSize { get; set; }
    public string? WarmupRoutine { get; set; }
} 