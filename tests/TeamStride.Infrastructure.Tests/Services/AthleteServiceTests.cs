using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shouldly;
using TeamStride.Application.Athletes.Dtos;
using TeamStride.Application.Common.Models;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;
using TeamStride.Domain.Interfaces;
using TeamStride.Infrastructure.Data;
using TeamStride.Infrastructure.Data.Extensions;
using TeamStride.Infrastructure.Services;
using AthleteProfileMapping = TeamStride.Infrastructure.Mapping.AthleteProfile;

namespace TeamStride.Infrastructure.Tests.Services;

public class AthleteServiceTests : BaseIntegrationTest
{
    private readonly AthleteService _athleteService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Guid _testTeamId;
    private readonly Guid _testUserId;

    public AthleteServiceTests()
    {
        _testTeamId = Guid.NewGuid();
        _testUserId = Guid.NewGuid();

        // Setup team service mock
        MockTeamService.Setup(x => x.CurrentTeamId).Returns(_testTeamId);
        MockCurrentUserService.Setup(x => x.UserId).Returns(_testUserId);

        // Setup AutoMapper with explicit type mapping
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Athlete, AthleteDto>()
                .ForMember(d => d.FirstName, o => o.MapFrom(s => s.User.FirstName))
                .ForMember(d => d.LastName, o => o.MapFrom(s => s.User.LastName))
                .ForMember(d => d.Email, o => o.MapFrom(s => s.User.Email))
                .ForMember(d => d.UserId, o => o.MapFrom(s => s.UserId.ToString()));

            cfg.CreateMap<CreateAthleteDto, ApplicationUser>()
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.Email));

            cfg.CreateMap<CreateAthleteDto, Athlete>()
                .ForMember(d => d.Profile, o => o.Ignore())
                .ForMember(d => d.User, o => o.Ignore());

            cfg.CreateMap<Domain.Entities.AthleteProfile, AthleteProfileDto>();
            cfg.CreateMap<CreateAthleteProfileDto, Domain.Entities.AthleteProfile>();
            cfg.CreateMap<UpdateAthleteProfileDto, Domain.Entities.AthleteProfile>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        });
        var mapper = config.CreateMapper();

        // Setup UserManager mock
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _mockMapper = new Mock<IMapper>();
        
        _athleteService = new AthleteService(
            DbContext,
            mapper,
            _mockUserManager.Object,
            MockTeamService.Object,
            MockCurrentUserService.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsAthleteDto()
    {
        // Arrange
        var athlete = await CreateTestAthleteAsync();

        // Act
        var result = await _athleteService.GetByIdAsync(athlete.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(athlete.Id);
        result.FirstName.ShouldBe(athlete.User.FirstName);
        result.LastName.ShouldBe(athlete.User.LastName);
        result.Email.ShouldBe(athlete.User.Email);
        result.Role.ShouldBe(athlete.Role);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _athleteService.GetByIdAsync(invalidId));
        
        exception.Message.ShouldContain($"Athlete with ID {invalidId} not found in current team");
    }

    [Fact]
    public async Task GetByIdAsync_WithDifferentTeam_ThrowsInvalidOperationException()
    {
        // Arrange
        var athlete = await CreateTestAthleteAsync();
        MockTeamService.Setup(x => x.CurrentTeamId).Returns(Guid.NewGuid()); // Different team

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _athleteService.GetByIdAsync(athlete.Id));
        
        exception.Message.ShouldContain($"Athlete with ID {athlete.Id} not found in current team");
    }

    #endregion

    #region GetByUserIdAsync Tests

    [Fact]
    public async Task GetByUserIdAsync_WithValidUserId_ReturnsAthleteDto()
    {
        // Arrange
        var athlete = await CreateTestAthleteAsync();

        // Act
        var result = await _athleteService.GetByUserIdAsync(athlete.UserId);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(athlete.Id);
        result.UserId.ShouldBe(athlete.UserId.ToString());
    }

    [Fact]
    public async Task GetByUserIdAsync_WithInvalidUserId_ReturnsNull()
    {
        // Arrange
        var invalidUserId = Guid.NewGuid();

        // Act
        var result = await _athleteService.GetByUserIdAsync(invalidUserId);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetByUserIdAsync_WithDifferentTeam_ReturnsNull()
    {
        // Arrange
        var athlete = await CreateTestAthleteAsync();
        MockTeamService.Setup(x => x.CurrentTeamId).Returns(Guid.NewGuid()); // Different team

        // Act
        var result = await _athleteService.GetByUserIdAsync(athlete.UserId);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region GetTeamRosterAsync Tests

    [Fact]
    public async Task GetTeamRosterAsync_WithDefaultPagination_ReturnsPaginatedList()
    {
        // Arrange
        await CreateMultipleTestAthletesAsync(5);

        // Act
        var result = await _athleteService.GetTeamRosterAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(5);
        result.TotalCount.ShouldBe(5);
        result.PageNumber.ShouldBe(1);
    }

    [Fact]
    public async Task GetTeamRosterAsync_WithCustomPagination_ReturnsPaginatedList()
    {
        // Arrange
        await CreateMultipleTestAthletesAsync(15);

        // Act
        var result = await _athleteService.GetTeamRosterAsync(pageNumber: 2, pageSize: 5);

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(5);
        result.TotalCount.ShouldBe(15);
        result.PageNumber.ShouldBe(2);
    }

    [Fact]
    public async Task GetTeamRosterAsync_OrdersByLastNameThenFirstName()
    {
        // Arrange
        var athlete1 = await CreateTestAthleteAsync("John", "Zebra");
        var athlete2 = await CreateTestAthleteAsync("Alice", "Alpha");
        var athlete3 = await CreateTestAthleteAsync("Bob", "Alpha");

        // Act
        var result = await _athleteService.GetTeamRosterAsync();

        // Assert
        result.Items.Count.ShouldBe(3);
        var itemsList = result.Items.ToList();
        itemsList[0].LastName.ShouldBe("Alpha");
        itemsList[0].FirstName.ShouldBe("Alice");
        itemsList[1].LastName.ShouldBe("Alpha");
        itemsList[1].FirstName.ShouldBe("Bob");
        itemsList[2].LastName.ShouldBe("Zebra");
        itemsList[2].FirstName.ShouldBe("John");
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesAthleteAndReturnsDto()
    {
        // Arrange
        var createDto = new CreateAthleteDto
        {
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            Role = AthleteRole.Athlete,
            JerseyNumber = "42"
        };

        var userId = Guid.NewGuid();
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser>(user => 
            {
                user.Id = userId;
                // Add the user to the context so it exists for the athlete creation
                DbContext.Users.Add(user);
                DbContext.SaveChanges();
            });

        // Act
        var result = await _athleteService.CreateAsync(createDto);

        // Assert
        result.ShouldNotBeNull();
        result.Email.ShouldBe(createDto.Email);
        result.FirstName.ShouldBe(createDto.FirstName);
        result.LastName.ShouldBe(createDto.LastName);
        result.Role.ShouldBe(createDto.Role);
        result.JerseyNumber.ShouldBe(createDto.JerseyNumber);

        // Verify athlete was saved to database
        var savedAthlete = await DbContext.Athletes.FirstOrDefaultAsync(a => a.JerseyNumber == "42");
        savedAthlete.ShouldNotBeNull();
        savedAthlete.TeamId.ShouldBe(_testTeamId);
    }

    [Fact]
    public async Task CreateAsync_WithProfile_CreatesAthleteWithProfile()
    {
        // Arrange
        var createDto = new CreateAthleteDto
        {
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            Profile = new CreateAthleteProfileDto
            {
                PreferredEvents = "100m, 200m",
                Goals = "Sub 11 seconds"
            }
        };

        var userId = Guid.NewGuid();
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser>(user => 
            {
                user.Id = userId;
                DbContext.Users.Add(user);
                DbContext.SaveChanges();
            });

        // Act
        var result = await _athleteService.CreateAsync(createDto);

        // Assert
        result.ShouldNotBeNull();
        result.Profile.ShouldNotBeNull();
        result.Profile.PreferredEvents.ShouldBe("100m, 200m");
        result.Profile.Goals.ShouldBe("Sub 11 seconds");
    }

    [Fact]
    public async Task CreateAsync_WhenUserCreationFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var createDto = new CreateAthleteDto
        {
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };

        var identityErrors = new[]
        {
            new IdentityError { Description = "Email already exists" }
        };

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _athleteService.CreateAsync(createDto));
        
        exception.Message.ShouldContain("Failed to create user");
        exception.Message.ShouldContain("Email already exists");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesAthleteAndReturnsDto()
    {
        // Arrange
        var athlete = await CreateTestAthleteAsync();
        var updateDto = new UpdateAthleteDto
        {
            Role = AthleteRole.Captain,
            JerseyNumber = "99",
            EmergencyContactName = "Jane Doe",
            EmergencyContactPhone = "555-1234",
            DateOfBirth = new DateTime(2000, 1, 1),
            Grade = "12",
            HasPhysicalOnFile = true,
            HasWaiverSigned = true
        };

        // Act
        var result = await _athleteService.UpdateAsync(athlete.Id, updateDto);

        // Assert
        result.ShouldNotBeNull();
        result.Role.ShouldBe(AthleteRole.Captain);
        result.JerseyNumber.ShouldBe("99");
        result.EmergencyContactName.ShouldBe("Jane Doe");
        result.EmergencyContactPhone.ShouldBe("555-1234");
        result.DateOfBirth.ShouldBe(new DateTime(2000, 1, 1));
        result.Grade.ShouldBe("12");
        result.HasPhysicalOnFile.ShouldBeTrue();
        result.HasWaiverSigned.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateAsync_WithPartialData_UpdatesOnlyProvidedFields()
    {
        // Arrange
        var athlete = await CreateTestAthleteAsync();
        var originalRole = athlete.Role;
        var updateDto = new UpdateAthleteDto
        {
            JerseyNumber = "88"
            // Only updating jersey number
        };

        // Act
        var result = await _athleteService.UpdateAsync(athlete.Id, updateDto);

        // Assert
        result.ShouldNotBeNull();
        result.JerseyNumber.ShouldBe("88");
        result.Role.ShouldBe(originalRole); // Should remain unchanged
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        var updateDto = new UpdateAthleteDto { JerseyNumber = "99" };

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _athleteService.UpdateAsync(invalidId, updateDto));
        
        exception.Message.ShouldContain($"Athlete with ID {invalidId} not found in current team");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_RemovesAthlete()
    {
        // Arrange
        var athlete = await CreateTestAthleteAsync();

        // Act
        await _athleteService.DeleteAsync(athlete.Id);

        // Assert
        var deletedAthlete = await DbContext.Athletes.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == athlete.Id);
        deletedAthlete.ShouldNotBeNull();
        deletedAthlete.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _athleteService.DeleteAsync(invalidId));
        
        exception.Message.ShouldContain($"Athlete with ID {invalidId} not found in current team");
    }

    #endregion

    #region UpdateRoleAsync Tests

    [Fact]
    public async Task UpdateRoleAsync_WithValidData_UpdatesRoleAndReturnsDto()
    {
        // Arrange
        var athlete = await CreateTestAthleteAsync();

        // Act
        var result = await _athleteService.UpdateRoleAsync(athlete.Id, AthleteRole.Captain);

        // Assert
        result.ShouldNotBeNull();
        result.Role.ShouldBe(AthleteRole.Captain);
    }

    [Fact]
    public async Task UpdateRoleAsync_WithInvalidId_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _athleteService.UpdateRoleAsync(invalidId, AthleteRole.Captain));
        
        exception.Message.ShouldContain($"Athlete with ID {invalidId} not found in current team");
    }

    #endregion

    #region UpdatePhysicalStatusAsync Tests

    [Fact]
    public async Task UpdatePhysicalStatusAsync_WithValidData_UpdatesStatusAndReturnsDto()
    {
        // Arrange
        var athlete = await CreateTestAthleteAsync();

        // Act
        var result = await _athleteService.UpdatePhysicalStatusAsync(athlete.Id, true);

        // Assert
        result.ShouldNotBeNull();
        result.HasPhysicalOnFile.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdatePhysicalStatusAsync_WithInvalidId_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _athleteService.UpdatePhysicalStatusAsync(invalidId, true));
        
        exception.Message.ShouldContain($"Athlete with ID {invalidId} not found in current team");
    }

    #endregion

    #region UpdateWaiverStatusAsync Tests

    [Fact]
    public async Task UpdateWaiverStatusAsync_WithValidData_UpdatesStatusAndReturnsDto()
    {
        // Arrange
        var athlete = await CreateTestAthleteAsync();

        // Act
        var result = await _athleteService.UpdateWaiverStatusAsync(athlete.Id, true);

        // Assert
        result.ShouldNotBeNull();
        result.HasWaiverSigned.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateWaiverStatusAsync_WithInvalidId_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _athleteService.UpdateWaiverStatusAsync(invalidId, true));
        
        exception.Message.ShouldContain($"Athlete with ID {invalidId} not found in current team");
    }

    #endregion

    #region UpdateProfileAsync Tests

    [Fact]
    public async Task UpdateProfileAsync_WithValidData_UpdatesProfileAndReturnsDto()
    {
        // Arrange
        var athlete = await CreateTestAthleteAsync();
        var profileDto = new UpdateAthleteProfileDto
        {
            PreferredEvents = "400m, 800m",
            Goals = "Break school record",
            TrainingNotes = "Focus on endurance"
        };

        // Act
        var result = await _athleteService.UpdateProfileAsync(athlete.Id, profileDto);

        // Assert
        result.ShouldNotBeNull();
        result.Profile.ShouldNotBeNull();
        result.Profile.PreferredEvents.ShouldBe("400m, 800m");
        result.Profile.Goals.ShouldBe("Break school record");
        result.Profile.TrainingNotes.ShouldBe("Focus on endurance");
    }

    [Fact]
    public async Task UpdateProfileAsync_WithInvalidId_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        var profileDto = new UpdateAthleteProfileDto { Goals = "Test goal" };

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _athleteService.UpdateProfileAsync(invalidId, profileDto));
        
        exception.Message.ShouldContain($"Athlete with ID {invalidId} not found in current team");
    }

    #endregion

    #region GetTeamCaptainsAsync Tests

    [Fact]
    public async Task GetTeamCaptainsAsync_WithCaptains_ReturnsCaptainsOnly()
    {
        // Arrange
        await CreateTestAthleteAsync("John", "Doe", AthleteRole.Athlete);
        await CreateTestAthleteAsync("Jane", "Smith", AthleteRole.Captain);
        await CreateTestAthleteAsync("Bob", "Johnson", AthleteRole.Captain);

        // Act
        var result = await _athleteService.GetTeamCaptainsAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
        result.All(a => a.Role == AthleteRole.Captain).ShouldBeTrue();
    }

    [Fact]
    public async Task GetTeamCaptainsAsync_WithNoCaptains_ReturnsEmptyList()
    {
        // Arrange
        await CreateTestAthleteAsync("John", "Doe", AthleteRole.Athlete);

        // Act
        var result = await _athleteService.GetTeamCaptainsAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(0);
    }

    #endregion

    #region IsAthleteInTeamAsync Tests

    [Fact]
    public async Task IsAthleteInTeamAsync_WithValidAthleteInTeam_ReturnsTrue()
    {
        // Arrange
        var athlete = await CreateTestAthleteAsync();

        // Act
        var result = await _athleteService.IsAthleteInTeamAsync(athlete.Id);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsAthleteInTeamAsync_WithAthleteNotInTeam_ReturnsFalse()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var result = await _athleteService.IsAthleteInTeamAsync(invalidId);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task IsAthleteInTeamAsync_WithAthleteInDifferentTeam_ReturnsFalse()
    {
        // Arrange
        var athlete = await CreateTestAthleteAsync();
        MockTeamService.Setup(x => x.CurrentTeamId).Returns(Guid.NewGuid()); // Different team

        // Act
        var result = await _athleteService.IsAthleteInTeamAsync(athlete.Id);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region Global Query Filter Tests

    [Fact]
    public async Task GlobalQueryFilter_SoftDeletedAthletes_AreNotReturnedByDefault()
    {
        // Arrange
        var athlete = await CreateTestAthleteAsync();
        
        // Soft delete the athlete
        await _athleteService.DeleteAsync(athlete.Id);

        // Act
        var result = await DbContext.Athletes.ToListAsync();

        // Assert
        result.ShouldNotContain(a => a.Id == athlete.Id);
    }

    [Fact]
    public async Task GlobalQueryFilter_SoftDeletedAthletes_CanBeRetrievedWithIgnoreQueryFilters()
    {
        // Arrange
        var athlete = await CreateTestAthleteAsync();
        
        // Soft delete the athlete
        await _athleteService.DeleteAsync(athlete.Id);

        // Act
        var result = await DbContext.Athletes.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == athlete.Id);

        // Assert
        result.ShouldNotBeNull();
        result.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public async Task GlobalQueryFilter_OnlyDeletedExtension_ReturnsOnlySoftDeletedAthletes()
    {
        // Arrange
        var activeAthlete = await CreateTestAthleteAsync("Active", "Athlete");
        var deletedAthlete = await CreateTestAthleteAsync("Deleted", "Athlete");
        
        // Soft delete one athlete
        await _athleteService.DeleteAsync(deletedAthlete.Id);

        // Act
        var result = await DbContext.Athletes.OnlyDeleted().ToListAsync();

        // Assert
        result.Count().ShouldBe(1);
        result.First().Id.ShouldBe(deletedAthlete.Id);
        result.First().IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public async Task GlobalQueryFilter_IncludeDeletedExtension_ReturnsBothActiveAndDeletedAthletes()
    {
        // Arrange
        var activeAthlete = await CreateTestAthleteAsync("Active", "Athlete");
        var deletedAthlete = await CreateTestAthleteAsync("Deleted", "Athlete");
        
        // Soft delete one athlete
        await _athleteService.DeleteAsync(deletedAthlete.Id);

        // Act
        var result = await DbContext.Athletes.IncludeDeleted().ToListAsync();

        // Assert
        result.Count().ShouldBe(2);
        result.ShouldContain(a => a.Id == activeAthlete.Id && !a.IsDeleted);
        result.ShouldContain(a => a.Id == deletedAthlete.Id && a.IsDeleted);
    }

    [Fact]
    public async Task GlobalQueryFilter_AthleteProfile_SoftDeletedProfilesAreNotReturnedByDefault()
    {
        // Arrange
        var athlete = await CreateTestAthleteAsync();
        var profile = athlete.Profile;
        
        // Manually soft delete the profile
        profile.IsDeleted = true;
        profile.DeletedOn = DateTime.UtcNow;
        profile.DeletedBy = _testUserId;
        await DbContext.SaveChangesAsync();

        // Act
        var result = await DbContext.AthleteProfiles.ToListAsync();

        // Assert
        result.ShouldNotContain(p => p.Id == profile.Id);
    }

    [Fact]
    public async Task GlobalQueryFilter_UserTeam_SoftDeletedUserTeamsAreNotReturnedByDefault()
    {
        // Arrange
        // Create a test athlete first (which creates the necessary User and Team relationships)
        var athlete = await CreateTestAthleteAsync();
        
        // Create a UserTeam using the existing user and team
        var userTeam = new UserTeam
        {
            Id = Guid.NewGuid(),
            UserId = athlete.UserId,
            TeamId = athlete.TeamId,
            Role = TeamRole.Athlete,
            IsDefault = false, // Set to false to avoid conflicts with existing default
            IsActive = true,
            JoinedOn = DateTime.UtcNow,
            CreatedOn = DateTime.UtcNow
        };

        DbContext.UserTeams.Add(userTeam);
        await DbContext.SaveChangesAsync();

        // Soft delete the user team
        userTeam.IsDeleted = true;
        userTeam.DeletedOn = DateTime.UtcNow;
        userTeam.DeletedBy = athlete.UserId;
        await DbContext.SaveChangesAsync();

        // Act
        var result = await DbContext.UserTeams.ToListAsync();

        // Assert
        result.ShouldNotContain(ut => ut.Id == userTeam.Id);
    }

    [Fact]
    public async Task GlobalQueryFilter_Team_SoftDeletedTeamsAreNotReturnedByDefault()
    {
        // Arrange
        // Create a test athlete first (which creates the necessary User and Team relationships)
        var athlete = await CreateTestAthleteAsync();
        
        // Create a new team for testing
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Test Team for Deletion",
            Subdomain = "test-delete",
            Status = TeamStatus.Active,
            Tier = TeamTier.Free,
            CreatedOn = DateTime.UtcNow
        };

        DbContext.Teams.Add(team);
        await DbContext.SaveChangesAsync();

        // Soft delete the team
        team.IsDeleted = true;
        team.DeletedOn = DateTime.UtcNow;
        team.DeletedBy = athlete.UserId;
        await DbContext.SaveChangesAsync();

        // Act
        var result = await DbContext.Teams.ToListAsync();

        // Assert
        result.ShouldNotContain(t => t.Id == team.Id);
    }

    [Fact]
    public async Task GlobalQueryFilter_ApplicationUser_SoftDeletedUsersAreNotReturnedByDefault()
    {
        // Arrange
        // Create a test athlete first to have a valid user for DeletedBy
        var athlete = await CreateTestAthleteAsync();
        
        // Create a new user for testing
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test-delete@example.com",
            FirstName = "Test",
            LastName = "User",
            UserName = "test-delete@example.com",
            IsActive = true,
            Status = UserStatus.Active,
            CreatedOn = DateTime.UtcNow
        };

        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        // Soft delete the user
        user.IsDeleted = true;
        user.DeletedOn = DateTime.UtcNow;
        user.DeletedBy = athlete.UserId;
        await DbContext.SaveChangesAsync();

        // Act
        var result = await DbContext.Users.ToListAsync();

        // Assert
        result.ShouldNotContain(u => u.Id == user.Id);
    }

    #endregion

    #region Helper Methods

    private async Task<Athlete> CreateTestAthleteAsync(
        string firstName = "John", 
        string lastName = "Doe", 
        AthleteRole role = AthleteRole.Athlete)
    {
        // Ensure the test team exists
        var existingTeam = await DbContext.Teams.FindAsync(_testTeamId);
        if (existingTeam == null)
        {
            var team = new Team
            {
                Id = _testTeamId,
                Name = "Test Team",
                Subdomain = "test",
                Status = TeamStatus.Active,
                Tier = TeamTier.Free,
                CreatedOn = DateTime.UtcNow
            };
            DbContext.Teams.Add(team);
            await DbContext.SaveChangesAsync();
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = $"{firstName.ToLower()}.{lastName.ToLower()}@example.com",
            FirstName = firstName,
            LastName = lastName,
            UserName = $"{firstName.ToLower()}.{lastName.ToLower()}@example.com"
        };

        var athlete = new Athlete
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Role = role,
            TeamId = _testTeamId,
            User = user,
            Profile = new Domain.Entities.AthleteProfile
            {
                Id = Guid.NewGuid(),
                AthleteId = Guid.NewGuid(), // Will be set correctly when athlete is created
                TeamId = _testTeamId
            }
        };

        athlete.Profile.AthleteId = athlete.Id;

        DbContext.Users.Add(user);
        DbContext.Athletes.Add(athlete);
        DbContext.AthleteProfiles.Add(athlete.Profile);
        await DbContext.SaveChangesAsync();

        return athlete;
    }

    private async Task<List<Athlete>> CreateMultipleTestAthletesAsync(int count)
    {
        var athletes = new List<Athlete>();
        
        for (int i = 0; i < count; i++)
        {
            var athlete = await CreateTestAthleteAsync($"FirstName{i}", $"LastName{i}");
            athletes.Add(athlete);
        }

        return athletes;
    }

    #endregion
} 