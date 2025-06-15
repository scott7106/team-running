using System;

namespace TeamStride.Application.Teams.Dtos;

public class TeamRegistrationWindowDto
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public required string TeamName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int MaxRegistrations { get; set; }
    public required string RegistrationPasscode { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? ModifiedOn { get; set; }
}

public class CreateRegistrationWindowDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int MaxRegistrations { get; set; }
    public required string RegistrationPasscode { get; set; }
}

public class UpdateRegistrationWindowDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int MaxRegistrations { get; set; }
    public required string RegistrationPasscode { get; set; }
} 