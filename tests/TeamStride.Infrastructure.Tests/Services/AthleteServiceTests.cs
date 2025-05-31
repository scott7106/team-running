using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using TeamStride.Application.Athletes.Dtos;
using TeamStride.Application.Athletes.Services;
using TeamStride.Application.Common.Services;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;
using TeamStride.Infrastructure.Services;

namespace TeamStride.Infrastructure.Tests.Services;

public class AthleteServiceTests : BaseSecuredTest
{
    private readonly Mock<IAuthorizationService> _mockAuthorizationService;
    private readonly Mock<ILogger<AthleteService>> _mockLogger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;

    public AthleteServiceTests()
    {
        _mockAuthorizationService = new Mock<IAuthorizationService>();
        _mockLogger = new Mock<ILogger<AthleteService>>();
        _userManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        var config = new MapperConfiguration(cfg => cfg.AddProfile<Mapping.MappingProfile>());
        _mapper = config.CreateMapper();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_AsTeamMember_WithValidId_ShouldReturnAthlete()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var athleteId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamMember, userId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamMember))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user = await CreateTestUserAsync("athlete@test.com", "John", "Doe");
        var athlete = await CreateTestAthleteAsync(user.Id, teamId, athleteId);

        // Act
        var result = await service.GetByIdAsync(athleteId);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(athleteId);
        result.FirstName.ShouldBe("Test");
        result.LastName.ShouldBe("Athlete");
        result.Email.ShouldBe("athlete@test.com");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var nonExistentId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamMember);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamMember))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => service.GetByIdAsync(nonExistentId));
        
        exception.Message.ShouldContain($"Athlete with ID {nonExistentId} not found");
    }

    [Fact]
    public async Task GetByIdAsync_WithoutTeamAccess_ShouldCallAuthorizationService()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var athleteId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamMember);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamMember))
            .ThrowsAsync(new UnauthorizedAccessException());

        var service = CreateService();

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => service.GetByIdAsync(athleteId));

        _mockAuthorizationService.Verify(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamMember), Times.Once);
    }

    #endregion

    #region GetByUserIdAsync Tests

    [Fact]
    public async Task GetByUserIdAsync_WithExistingUser_ShouldReturnAthlete()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamMember, userId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamMember))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user = await CreateTestUserAsync("athlete@test.com", "John", "Doe", userId);
        var athlete = await CreateTestAthleteAsync(user.Id, teamId);

        // Act
        var result = await service.GetByUserIdAsync(userId);

        // Assert
        result.ShouldNotBeNull();
        result!.UserId.ShouldBe(userId.ToString());
        result.FirstName.ShouldBe("Test");
        result.LastName.ShouldBe("Athlete");
    }

    [Fact]
    public async Task GetByUserIdAsync_WithNonExistentUser_ShouldReturnNull()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var nonExistentUserId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamMember);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamMember))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.GetByUserIdAsync(nonExistentUserId);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region GetTeamRosterAsync Tests

    [Fact]
    public async Task GetTeamRosterAsync_WithMultipleAthletes_ShouldReturnPaginatedList()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamMember);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamMember))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Create multiple athletes
        var user1 = await CreateTestUserAsync("athlete1@test.com", "Alice", "Anderson");
        var user2 = await CreateTestUserAsync("athlete2@test.com", "Bob", "Brown");
        var user3 = await CreateTestUserAsync("athlete3@test.com", "Charlie", "Clark");
        
        await CreateTestAthleteAsync(user1.Id, teamId);
        await CreateTestAthleteAsync(user2.Id, teamId);
        await CreateTestAthleteAsync(user3.Id, teamId);

        // Act
        var result = await service.GetTeamRosterAsync(pageNumber: 1, pageSize: 10);

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(3);
        result.TotalCount.ShouldBe(3);
        result.PageNumber.ShouldBe(1);
        
        // Should be ordered by last name, then first name (all athletes have "Athlete" as last name)
        result.Items.All(a => a.LastName == "Athlete").ShouldBeTrue();
    }

    [Fact]
    public async Task GetTeamRosterAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamMember);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamMember))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Create 5 athletes
        for (int i = 1; i <= 5; i++)
        {
            var user = await CreateTestUserAsync($"athlete{i}@test.com", $"Athlete{i:D2}", "User");
            await CreateTestAthleteAsync(user.Id, teamId);
        }

        // Act
        var result = await service.GetTeamRosterAsync(pageNumber: 2, pageSize: 2);

        // Assert
        result.PageNumber.ShouldBe(2);
        result.Items.Count.ShouldBe(2);
        result.TotalCount.ShouldBe(5);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_AsTeamAdmin_WithNewUser_ShouldCreateAthleteAndUser()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamAdmin, userId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Create team with Free tier (limit of 7 athletes)
        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        await CreateTestTeamAsync("Test Team", "test-team", owner.Id, teamId, tier: TeamTier.Free);

        var createDto = new CreateAthleteDto
        {
            Email = "newathlete@test.com",
            FirstName = "New",
            LastName = "Athlete",
            Role = AthleteRole.Athlete,
            JerseyNumber = "42",
            EmergencyContactName = "Parent Name",
            EmergencyContactPhone = "555-0123",
            DateOfBirth = DateTime.Today.AddYears(-16),
            Grade = "11"
        };

        // Act
        var result = await service.CreateAsync(createDto);

        // Assert
        result.ShouldNotBeNull();
        result.FirstName.ShouldBe("New");
        result.LastName.ShouldBe("Athlete");
        result.Email.ShouldBe("newathlete@test.com");
        result.Role.ShouldBe(AthleteRole.Athlete);
        result.JerseyNumber.ShouldBe("42");
        result.EmergencyContactName.ShouldBe("Parent Name");
        result.EmergencyContactPhone.ShouldBe("555-0123");
        result.DateOfBirth.ShouldBe(DateTime.Today.AddYears(-16));
        result.Grade.ShouldBe("11");

        // Verify user was created
        var user = await _userManager.FindByEmailAsync("newathlete@test.com");
        user.ShouldNotBeNull();
        user.FirstName.ShouldBe("New");
        user.LastName.ShouldBe("Athlete");
    }

    [Fact]
    public async Task CreateAsync_WithExistingUser_ShouldCreateAthleteForExistingUser()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamAdmin, userId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Create team
        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        await CreateTestTeamAsync("Test Team", "test-team", owner.Id, teamId, tier: TeamTier.Free);

        // Create existing user
        var existingUser = await CreateTestUserAsync("existing@test.com", "Existing", "User");

        var createDto = new CreateAthleteDto
        {
            Email = "existing@test.com",
            FirstName = "AthleteFirst", // This will be used for the athlete record
            LastName = "AthleteLast",   // This will be used for the athlete record
            Role = AthleteRole.Captain
        };

        // Act
        var result = await service.CreateAsync(createDto);

        // Assert
        result.ShouldNotBeNull();
        result.FirstName.ShouldBe("AthleteFirst"); // Should use athlete DTO values
        result.LastName.ShouldBe("AthleteLast");   // Should use athlete DTO values
        result.Email.ShouldBe("existing@test.com"); // Should use existing user's email
        result.Role.ShouldBe(AthleteRole.Captain);
    }

    [Fact]
    public async Task CreateAsync_WithProfile_ShouldCreateAthleteWithProfile()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamAdmin, userId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Create team
        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        await CreateTestTeamAsync("Test Team", "test-team", owner.Id, teamId, tier: TeamTier.Free);

        var createDto = new CreateAthleteDto
        {
            Email = "athlete@test.com",
            FirstName = "Test",
            LastName = "Athlete",
            Profile = new CreateAthleteProfileDto
            {
                PreferredEvents = "100m, 200m",
                PersonalBests = "100m: 10.5s, 200m: 21.2s",
                Goals = "Sub 10 in 100m",
                UniformSize = "M"
            }
        };

        // Act
        var result = await service.CreateAsync(createDto);

        // Assert
        result.ShouldNotBeNull();
        result.Profile.ShouldNotBeNull();
        result.Profile!.PreferredEvents.ShouldBe("100m, 200m");
        result.Profile.PersonalBests.ShouldBe("100m: 10.5s, 200m: 21.2s");
        result.Profile.Goals.ShouldBe("Sub 10 in 100m");
        result.Profile.UniformSize.ShouldBe("M");
    }

    [Fact]
    public async Task CreateAsync_WhenTeamAtAthleteLimit_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamAdmin, userId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Create team with Free tier (limit of 7 athletes)
        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        await CreateTestTeamAsync("Test Team", "test-team", owner.Id, teamId, tier: TeamTier.Free);

        // Add 7 athletes (at limit)
        for (int i = 1; i <= 7; i++)
        {
            var user = await CreateTestUserAsync($"athlete{i}@test.com", $"Athlete{i}", "User");
            await CreateTestAthleteAsync(user.Id, teamId);
            await CreateUserTeamRelationshipAsync(user.Id, teamId, TeamRole.TeamMember, MemberType.Athlete);
        }

        var createDto = new CreateAthleteDto
        {
            Email = "overload@test.com",
            FirstName = "Over",
            LastName = "Load"
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => service.CreateAsync(createDto));
        
        exception.Message.ShouldContain("Team has reached the maximum number of athletes");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamAdmin, userId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Create team
        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        await CreateTestTeamAsync("Test Team", "test-team", owner.Id, teamId, tier: TeamTier.Free);

        // Create existing athlete
        var existingUser = await CreateTestUserAsync("athlete@test.com", "Existing", "Athlete");
        await CreateTestAthleteAsync(existingUser.Id, teamId);

        var createDto = new CreateAthleteDto
        {
            Email = "athlete@test.com",
            FirstName = "Duplicate",
            LastName = "Athlete"
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => service.CreateAsync(createDto));
        
        exception.Message.ShouldContain("is already an athlete on this team");
    }

    [Fact]
    public async Task CreateAsync_AsNonAdmin_ShouldCallAuthorizationService()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamMember);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin))
            .ThrowsAsync(new UnauthorizedAccessException());

        var service = CreateService();

        var createDto = new CreateAthleteDto
        {
            Email = "athlete@test.com",
            FirstName = "Test",
            LastName = "Athlete"
        };

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => service.CreateAsync(createDto));

        _mockAuthorizationService.Verify(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithoutEmail_ShouldCreateAthleteWithoutUser()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamAdmin, userId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Create team
        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        await CreateTestTeamAsync("Test Team", "test-team", owner.Id, teamId, tier: TeamTier.Free);

        var createDto = new CreateAthleteDto
        {
            Email = null, // No email means no user account
            FirstName = "Walk-On",
            LastName = "Athlete",
            Role = AthleteRole.Athlete,
            JerseyNumber = "99"
        };

        // Act
        var result = await service.CreateAsync(createDto);

        // Assert
        result.ShouldNotBeNull();
        result.FirstName.ShouldBe("Walk-On");
        result.LastName.ShouldBe("Athlete");
        result.Email.ShouldBeNull(); // No user account means no email
        result.UserId.ShouldBeNull(); // No user account
        result.Role.ShouldBe(AthleteRole.Athlete);
        result.JerseyNumber.ShouldBe("99");

        // Verify no user was created
        var users = await _userManager.Users.ToListAsync();
        users.ShouldNotContain(u => u.FirstName == "Walk-On");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_AsTeamAdmin_WithValidData_ShouldUpdateAthlete()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var athleteId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamAdmin, userId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user = await CreateTestUserAsync("athlete@test.com", "John", "Doe");
        var athlete = await CreateTestAthleteAsync(user.Id, teamId, athleteId);

        var updateDto = new UpdateAthleteDto
        {
            Role = AthleteRole.Captain,
            JerseyNumber = "99",
            EmergencyContactName = "Updated Contact",
            EmergencyContactPhone = "555-9999",
            Grade = "12",
            HasPhysicalOnFile = true,
            HasWaiverSigned = true
        };

        // Act
        var result = await service.UpdateAsync(athleteId, updateDto);

        // Assert
        result.ShouldNotBeNull();
        result.Role.ShouldBe(AthleteRole.Captain);
        result.JerseyNumber.ShouldBe("99");
        result.EmergencyContactName.ShouldBe("Updated Contact");
        result.EmergencyContactPhone.ShouldBe("555-9999");
        result.Grade.ShouldBe("12");
        result.HasPhysicalOnFile.ShouldBeTrue();
        result.HasWaiverSigned.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateAsync_WithProfile_ShouldUpdateProfile()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var athleteId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamAdmin, userId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user = await CreateTestUserAsync("athlete@test.com", "John", "Doe");
        var athlete = await CreateTestAthleteAsync(user.Id, teamId, athleteId);

        var updateDto = new UpdateAthleteDto
        {
            Profile = new UpdateAthleteProfileDto
            {
                PreferredEvents = "400m, 800m",
                PersonalBests = "400m: 50.5s",
                Goals = "Sub 50 in 400m"
            }
        };

        // Act
        var result = await service.UpdateAsync(athleteId, updateDto);

        // Assert
        result.ShouldNotBeNull();
        result.Profile.ShouldNotBeNull();
        result.Profile!.PreferredEvents.ShouldBe("400m, 800m");
        result.Profile.PersonalBests.ShouldBe("400m: 50.5s");
        result.Profile.Goals.ShouldBe("Sub 50 in 400m");
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var nonExistentId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamAdmin);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var updateDto = new UpdateAthleteDto { Role = AthleteRole.Captain };

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => service.UpdateAsync(nonExistentId, updateDto));
        
        exception.Message.ShouldContain($"Athlete with ID {nonExistentId} not found");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_AsTeamAdmin_ShouldDeleteAthlete()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var athleteId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamAdmin, userId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner", userId);
        _ = await CreateTestTeamAsync("Team to Delete", "team-to-delete", owner.Id, teamId);
        
        var user = await CreateTestUserAsync("athlete@test.com", "John", "Doe");
        _ = await CreateTestAthleteAsync(user.Id, teamId, athleteId);
        await CreateUserTeamRelationshipAsync(user.Id, teamId, TeamRole.TeamMember, MemberType.Athlete);

        // Act
        await service.DeleteAsync(athleteId);

        // Assert
        var deletedAthlete = await DbContext.Athletes
            .FirstOrDefaultAsync(a => a.Id == athleteId);
        deletedAthlete.ShouldBeNull();

        var userTeam = await DbContext.UserTeams
            .FirstOrDefaultAsync(ut => ut.UserId == user.Id && ut.TeamId == teamId && ut.MemberType == MemberType.Athlete);
        userTeam.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var nonExistentId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamAdmin);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => service.DeleteAsync(nonExistentId));
        
        exception.Message.ShouldContain($"Athlete with ID {nonExistentId} not found");
    }

    #endregion

    #region UpdateRoleAsync Tests

    [Fact]
    public async Task UpdateRoleAsync_AsTeamAdmin_ShouldUpdateRole()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var athleteId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamAdmin);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user = await CreateTestUserAsync("athlete@test.com", "John", "Doe");
        var athlete = await CreateTestAthleteAsync(user.Id, teamId, athleteId);

        // Act
        var result = await service.UpdateRoleAsync(athleteId, AthleteRole.Captain);

        // Assert
        result.ShouldNotBeNull();
        result.Role.ShouldBe(AthleteRole.Captain);
    }

    #endregion

    #region UpdatePhysicalStatusAsync Tests

    [Fact]
    public async Task UpdatePhysicalStatusAsync_AsTeamAdmin_ShouldUpdateStatus()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var athleteId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamAdmin);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user = await CreateTestUserAsync("athlete@test.com", "John", "Doe");
        var athlete = await CreateTestAthleteAsync(user.Id, teamId, athleteId);

        // Act
        var result = await service.UpdatePhysicalStatusAsync(athleteId, true);

        // Assert
        result.ShouldNotBeNull();
        result.HasPhysicalOnFile.ShouldBeTrue();
    }

    #endregion

    #region UpdateWaiverStatusAsync Tests

    [Fact]
    public async Task UpdateWaiverStatusAsync_AsTeamAdmin_ShouldUpdateStatus()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var athleteId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamAdmin);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user = await CreateTestUserAsync("athlete@test.com", "John", "Doe");
        var athlete = await CreateTestAthleteAsync(user.Id, teamId, athleteId);

        // Act
        var result = await service.UpdateWaiverStatusAsync(athleteId, true);

        // Assert
        result.ShouldNotBeNull();
        result.HasWaiverSigned.ShouldBeTrue();
    }

    #endregion

    #region UpdateProfileAsync Tests

    [Fact]
    public async Task UpdateProfileAsync_AsTeamAdmin_ShouldUpdateProfile()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var athleteId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamAdmin);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user = await CreateTestUserAsync("athlete@test.com", "John", "Doe");
        var athlete = await CreateTestAthleteAsync(user.Id, teamId, athleteId);

        var profileDto = new UpdateAthleteProfileDto
        {
            PreferredEvents = "Long Distance",
            PersonalBests = "5k: 16:30",
            Goals = "Break 16 minutes in 5k"
        };

        // Act
        var result = await service.UpdateProfileAsync(athleteId, profileDto);

        // Assert
        result.ShouldNotBeNull();
        result.Profile.ShouldNotBeNull();
        result.Profile!.PreferredEvents.ShouldBe("Long Distance");
        result.Profile.PersonalBests.ShouldBe("5k: 16:30");
        result.Profile.Goals.ShouldBe("Break 16 minutes in 5k");
    }

    #endregion

    #region GetTeamCaptainsAsync Tests

    [Fact]
    public async Task GetTeamCaptainsAsync_WithCaptains_ShouldReturnCaptains()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamMember);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamMember))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Create athletes with different roles
        var captain1 = await CreateTestUserAsync("captain1@test.com", "Captain", "One");
        var captain2 = await CreateTestUserAsync("captain2@test.com", "Captain", "Two");
        var athlete = await CreateTestUserAsync("athlete@test.com", "Regular", "Athlete");
        
        await CreateTestAthleteAsync(captain1.Id, teamId, role: AthleteRole.Captain);
        await CreateTestAthleteAsync(captain2.Id, teamId, role: AthleteRole.Captain);
        await CreateTestAthleteAsync(athlete.Id, teamId, role: AthleteRole.Athlete);

        // Act
        var result = await service.GetTeamCaptainsAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
        result.All(c => c.Role == AthleteRole.Captain).ShouldBeTrue();
        result.All(c => c.FirstName == "Test" && c.LastName == "Athlete").ShouldBeTrue();
    }

    #endregion

    #region IsAthleteInTeamAsync Tests

    [Fact]
    public async Task IsAthleteInTeamAsync_WithAthleteInTeam_ShouldReturnTrue()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var athleteId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamMember);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamMember))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user = await CreateTestUserAsync("athlete@test.com", "John", "Doe");
        await CreateTestAthleteAsync(user.Id, teamId, athleteId);

        // Act
        var result = await service.IsAthleteInTeamAsync(athleteId);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsAthleteInTeamAsync_WithAthleteNotInTeam_ShouldReturnFalse()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var nonExistentAthleteId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamMember);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamMember))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.IsAthleteInTeamAsync(nonExistentAthleteId);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region Helper Methods

    private AthleteService CreateService()
    {
        return new AthleteService(
            DbContext,
            _mockAuthorizationService.Object,
            MockCurrentUserService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object);
    }

    private async Task<ApplicationUser> CreateTestUserAsync(string email, string firstName, string lastName, Guid? id = null)
    {
        var user = new ApplicationUser
        {
            Id = id ?? Guid.NewGuid(),
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true
        };

        await _userManager.CreateAsync(user);
        return user;
    }

    private async Task<Athlete> CreateTestAthleteAsync(
        Guid userId, 
        Guid teamId, 
        Guid? athleteId = null,
        AthleteRole role = AthleteRole.Athlete)
    {
        var athlete = new Athlete
        {
            Id = athleteId ?? Guid.NewGuid(),
            UserId = userId,
            FirstName = "Test",
            LastName = "Athlete",
            TeamId = teamId,
            Role = role,
            JerseyNumber = "1",
            HasPhysicalOnFile = false,
            HasWaiverSigned = false,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };

        DbContext.Athletes.Add(athlete);
        await DbContext.SaveChangesAsync();
        return athlete;
    }

    private async Task<Team> CreateTestTeamAsync(
        string name,
        string subdomain,
        Guid ownerId,
        Guid? teamId = null,
        TeamStatus status = TeamStatus.Active,
        TeamTier tier = TeamTier.Free)
    {
        var team = new Team
        {
            Id = teamId ?? Guid.NewGuid(),
            Name = name,
            Subdomain = subdomain,
            Status = status,
            Tier = tier,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = ownerId
        };

        DbContext.Teams.Add(team);
        await CreateUserTeamRelationshipAsync(ownerId, team.Id, TeamRole.TeamOwner, MemberType.Coach);
        await DbContext.SaveChangesAsync();
        return team;
    }

    private async Task<UserTeam> CreateUserTeamRelationshipAsync(
        Guid userId,
        Guid teamId,
        TeamRole role = TeamRole.TeamMember,
        MemberType memberType = MemberType.Coach)
    {
        var userTeam = new UserTeam
        {
            UserId = userId,
            TeamId = teamId,
            Role = role,
            MemberType = memberType,
            IsActive = true,
            JoinedOn = DateTime.UtcNow
        };

        DbContext.UserTeams.Add(userTeam);
        await DbContext.SaveChangesAsync();
        return userTeam;
    }

    #endregion
} 