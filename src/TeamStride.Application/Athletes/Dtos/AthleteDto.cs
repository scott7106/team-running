using System.ComponentModel.DataAnnotations;
using TeamStride.Domain.Entities;

namespace TeamStride.Application.Athletes.Dtos;

public class AthleteDto
{
    public Guid Id { get; set; }
    public string? UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public AthleteRole Role { get; set; }
    public Gender Gender { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public DateTime DateOfBirth { get; set; }
    public GradeLevel GradeLevel { get; set; }
    public bool HasPhysicalOnFile { get; set; }
    public bool HasWaiverSigned { get; set; }
    public AthleteProfileDto? Profile { get; set; }
} 