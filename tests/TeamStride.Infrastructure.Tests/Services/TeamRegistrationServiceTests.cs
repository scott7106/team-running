using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Application.Common.Services;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;
using TeamStride.Domain.Interfaces;
using TeamStride.Infrastructure.Data;
using TeamStride.Infrastructure.Mapping;
using TeamStride.Infrastructure.Services;
using Shouldly;
using Microsoft.AspNetCore.Identity;
using Xunit;
using TeamStride.Application.Teams.Services;

namespace TeamStride.Infrastructure.Tests.Services;

public class TeamRegistrationServiceTests : BaseIntegrationTest
{
    private readonly TeamRegistrationService _service;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IAuthorizationService> _mockAuthorizationService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILogger<TeamRegistrationService>> _mockLogger;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;

    public TeamRegistrationServiceTests()
    {
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockAuthorizationService = new Mock<IAuthorizationService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<TeamRegistrationService>>();
        
        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);

        // Mock CreateAsync to always succeed and add user to context
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .Returns<ApplicationUser, string>((user, pwd) => {
                DbContext.Users.Add(user);
                DbContext.SaveChanges();
                return Task.FromResult(IdentityResult.Success);
            });

        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        var mapper = config.CreateMapper();

        _service = new TeamRegistrationService(
            DbContext,
            mapper,
            _mockCurrentUserService.Object,
            _mockAuthorizationService.Object,
            _mockEmailService.Object,
            _mockLogger.Object,
            _mockUserManager.Object);

        // Setup default current user
        var userId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);

        // Setup default authorization behavior
        _mockAuthorizationService
            .Setup(x => x.CanManageRegistrationAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);
    }

    [Fact]
    public async Task CreateRegistrationWindow_WhenValid_ReturnsWindow()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var dto = new CreateRegistrationWindowDto
        {
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(7),
            MaxRegistrations = 50,
            RegistrationPasscode = "test123"
        };

        // Act
        var result = await _service.CreateRegistrationWindowAsync(team.Id, dto);

        // Assert
        result.ShouldNotBeNull();
        result.TeamId.ShouldBe(team.Id);
        result.TeamName.ShouldBe(team.Name);
        result.StartDate.ShouldBe(dto.StartDate);
        result.EndDate.ShouldBe(dto.EndDate);
        result.MaxRegistrations.ShouldBe(dto.MaxRegistrations);
        result.RegistrationPasscode.ShouldBe(dto.RegistrationPasscode);
        result.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateRegistrationWindow_WhenNotAuthorized_ThrowsException()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var dto = new CreateRegistrationWindowDto
        {
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(7),
            MaxRegistrations = 50,
            RegistrationPasscode = "test123"
        };

        _mockAuthorizationService
            .Setup(x => x.CanManageRegistrationAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            _service.CreateRegistrationWindowAsync(team.Id, dto));
    }

    [Fact]
    public async Task CreateRegistrationWindow_WhenDatesInvalid_ThrowsException()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var dto = new CreateRegistrationWindowDto
        {
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(1),
            MaxRegistrations = 50,
            RegistrationPasscode = "test123"
        };

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() =>
            _service.CreateRegistrationWindowAsync(team.Id, dto));
    }

    [Fact]
    public async Task CreateRegistrationWindow_WhenOverlapping_ThrowsException()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var existingWindow = new TeamRegistrationWindow
        {
            TeamId = team.Id,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(7),
            MaxRegistrations = 50,
            RegistrationPasscode = "test123",
            IsActive = true,
            CreatedOn = DateTime.UtcNow
        };
        DbContext.TeamRegistrationWindows.Add(existingWindow);
        await DbContext.SaveChangesAsync();

        var dto = new CreateRegistrationWindowDto
        {
            StartDate = DateTime.UtcNow.AddDays(3),
            EndDate = DateTime.UtcNow.AddDays(10),
            MaxRegistrations = 50,
            RegistrationPasscode = "test456"
        };

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() =>
            _service.CreateRegistrationWindowAsync(team.Id, dto));
    }

    [Fact]
    public async Task SubmitRegistration_WhenValid_SendsConfirmationEmail()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var window = new TeamRegistrationWindow
        {
            TeamId = team.Id,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(7),
            MaxRegistrations = 50,
            RegistrationPasscode = "test123",
            IsActive = true,
            CreatedOn = DateTime.UtcNow
        };
        DbContext.TeamRegistrationWindows.Add(window);
        await DbContext.SaveChangesAsync();

        var dto = new SubmitRegistrationDto
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            EmergencyContactName = "Emergency Contact",
            EmergencyContactPhone = "1234567890",
            CodeOfConductAccepted = true,
            RegistrationPasscode = "test123",
            Athletes = new List<AthleteRegistrationDto>
            {
                new()
                {
                    FirstName = "Athlete",
                    LastName = "One",
                    Birthdate = DateTime.UtcNow.AddYears(-10),
                    GradeLevel = "5th"
                }
            }
        };

        // Act
        var result = await _service.SubmitRegistrationAsync(team.Id, dto);

        // Assert
        result.ShouldNotBeNull();
        result.TeamId.ShouldBe(team.Id);
        result.TeamName.ShouldBe(team.Name);
        result.Email.ShouldBe(dto.Email);
        result.FirstName.ShouldBe(dto.FirstName);
        result.LastName.ShouldBe(dto.LastName);
        result.EmergencyContactName.ShouldBe(dto.EmergencyContactName);
        result.EmergencyContactPhone.ShouldBe(dto.EmergencyContactPhone);
        result.CodeOfConductAccepted.ShouldBeTrue();
        result.Status.ShouldBe(RegistrationStatus.Pending);
        result.Athletes.ShouldNotBeNull();
        result.Athletes.Count.ShouldBe(1);
        _mockEmailService.Verify(x => x.SendRegistrationConfirmationAsync(It.IsAny<TeamRegistrationDto>()), Times.Once);
    }

    [Fact]
    public async Task SubmitRegistration_WhenNoWindow_ThrowsException()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var dto = new SubmitRegistrationDto
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            EmergencyContactName = "Emergency Contact",
            EmergencyContactPhone = "1234567890",
            CodeOfConductAccepted = true,
            RegistrationPasscode = "test123",
            Athletes = new List<AthleteRegistrationDto>()
        };

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() =>
            _service.SubmitRegistrationAsync(team.Id, dto));
    }

    [Fact]
    public async Task SubmitRegistration_WhenInvalidPasscode_ThrowsException()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var window = new TeamRegistrationWindow
        {
            TeamId = team.Id,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(7),
            MaxRegistrations = 50,
            RegistrationPasscode = "test123",
            IsActive = true,
            CreatedOn = DateTime.UtcNow
        };
        DbContext.TeamRegistrationWindows.Add(window);
        await DbContext.SaveChangesAsync();

        var dto = new SubmitRegistrationDto
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            EmergencyContactName = "Emergency Contact",
            EmergencyContactPhone = "1234567890",
            CodeOfConductAccepted = true,
            RegistrationPasscode = "wrongpass",
            Athletes = new List<AthleteRegistrationDto>()
        };

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() =>
            _service.SubmitRegistrationAsync(team.Id, dto));
    }

    [Fact]
    public async Task UpdateRegistrationStatus_WhenValid_SendsStatusUpdateEmail()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var registration = new TeamRegistration
        {
            TeamId = team.Id,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            EmergencyContactName = "Emergency Contact",
            EmergencyContactPhone = "1234567890",
            CodeOfConductAccepted = true,
            CodeOfConductAcceptedOn = DateTime.UtcNow,
            Status = RegistrationStatus.Pending,
            CreatedOn = DateTime.UtcNow
        };
        DbContext.TeamRegistrations.Add(registration);
        await DbContext.SaveChangesAsync();

        var dto = new UpdateRegistrationStatusDto
        {
            Status = RegistrationStatus.Approved
        };

        // Act
        var result = await _service.UpdateRegistrationStatusAsync(team.Id, registration.Id, dto);

        // Assert
        result.ShouldNotBeNull();
        result.Status.ShouldBe(RegistrationStatus.Approved);
        result.TeamName.ShouldBe(team.Name);
        _mockEmailService.Verify(x => x.SendRegistrationStatusUpdateAsync(It.IsAny<TeamRegistrationDto>()), Times.Once);
    }

    [Fact]
    public async Task UpdateRegistrationStatus_WhenNotAuthorized_ThrowsException()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var registration = new TeamRegistration
        {
            TeamId = team.Id,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            EmergencyContactName = "Emergency Contact",
            EmergencyContactPhone = "1234567890",
            CodeOfConductAccepted = true,
            Status = RegistrationStatus.Pending,
            CreatedOn = DateTime.UtcNow
        };
        DbContext.TeamRegistrations.Add(registration);
        await DbContext.SaveChangesAsync();

        var dto = new UpdateRegistrationStatusDto { Status = RegistrationStatus.Approved };

        _mockAuthorizationService
            .Setup(x => x.RequireTeamAccessAsync(team.Id, TeamRole.TeamAdmin))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            _service.UpdateRegistrationStatusAsync(team.Id, registration.Id, dto));
    }

    [Fact]
    public async Task GetRegistrations_ReturnsAllRegistrations()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var registrations = new List<TeamRegistration>
        {
            new()
            {
                TeamId = team.Id,
                Email = "test1@example.com",
                FirstName = "Test",
                LastName = "One",
                EmergencyContactName = "Emergency Contact 1",
                EmergencyContactPhone = "1234567890",
                CodeOfConductAccepted = true,
                CodeOfConductAcceptedOn = DateTime.UtcNow,
                Status = RegistrationStatus.Pending,
                CreatedOn = DateTime.UtcNow
            },
            new()
            {
                TeamId = team.Id,
                Email = "test2@example.com",
                FirstName = "Test",
                LastName = "Two",
                EmergencyContactName = "Emergency Contact 2",
                EmergencyContactPhone = "0987654321",
                CodeOfConductAccepted = true,
                CodeOfConductAcceptedOn = DateTime.UtcNow,
                Status = RegistrationStatus.Waitlisted,
                CreatedOn = DateTime.UtcNow
            }
        };
        DbContext.TeamRegistrations.AddRange(registrations);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetRegistrationsAsync(team.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldAllBe(r => r.TeamId == team.Id);
        result.ShouldAllBe(r => r.TeamName == team.Name);
    }

    [Fact]
    public async Task GetWaitlist_ReturnsOnlyWaitlistedRegistrations()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var registrations = new List<TeamRegistration>
        {
            new()
            {
                TeamId = team.Id,
                Email = "test1@example.com",
                FirstName = "Test",
                LastName = "One",
                EmergencyContactName = "Emergency Contact 1",
                EmergencyContactPhone = "1234567890",
                CodeOfConductAccepted = true,
                CodeOfConductAcceptedOn = DateTime.UtcNow,
                Status = RegistrationStatus.Pending,
                CreatedOn = DateTime.UtcNow
            },
            new()
            {
                TeamId = team.Id,
                Email = "test2@example.com",
                FirstName = "Test",
                LastName = "Two",
                EmergencyContactName = "Emergency Contact 2",
                EmergencyContactPhone = "0987654321",
                CodeOfConductAccepted = true,
                CodeOfConductAcceptedOn = DateTime.UtcNow,
                Status = RegistrationStatus.Waitlisted,
                CreatedOn = DateTime.UtcNow
            }
        };
        DbContext.TeamRegistrations.AddRange(registrations);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetWaitlistAsync(team.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].Status.ShouldBe(RegistrationStatus.Waitlisted);
        result[0].TeamName.ShouldBe(team.Name);
    }

    private async Task<Team> CreateTestTeamAsync()
    {
        var team = new Team
        {
            Name = "Test Team",
            Subdomain = "test-team",
            Status = TeamStatus.Active,
            Tier = TeamTier.Free,
            CreatedOn = DateTime.UtcNow
        };
        DbContext.Teams.Add(team);
        await DbContext.SaveChangesAsync();
        return team;
    }
} 