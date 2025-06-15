using System;
using System.Collections.Generic;
using TeamStride.Domain.Entities;

namespace TeamStride.Application.Teams.Dtos;

public class TeamRegistrationDto
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public required string TeamName { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string EmergencyContactName { get; set; }
    public required string EmergencyContactPhone { get; set; }
    public bool CodeOfConductAccepted { get; set; }
    public DateTime CodeOfConductAcceptedOn { get; set; }
    public RegistrationStatus Status { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public required ICollection<AthleteRegistrationDto> Athletes { get; set; }
}

public class SubmitRegistrationDto
{
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string EmergencyContactName { get; set; }
    public required string EmergencyContactPhone { get; set; }
    public bool CodeOfConductAccepted { get; set; }
    public required string RegistrationPasscode { get; set; }
    public required List<AthleteRegistrationDto> Athletes { get; set; }
}

public class UpdateRegistrationStatusDto
{
    public RegistrationStatus Status { get; set; }
}

public class AthleteRegistrationDto
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateTime Birthdate { get; set; }
    public required string GradeLevel { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class CreateRegistrationDto
{
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string EmergencyContactName { get; set; }
    public required string EmergencyContactPhone { get; set; }
    public required string RegistrationPasscode { get; set; }
    public bool CodeOfConductAccepted { get; set; }
    public required ICollection<AthleteRegistrationDto> Athletes { get; set; }
} 