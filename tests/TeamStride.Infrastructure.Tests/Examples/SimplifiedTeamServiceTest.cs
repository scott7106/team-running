using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using TeamStride.Application.Common.Services;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;
using TeamStride.Infrastructure.Mapping;
using TeamStride.Infrastructure.Services;

namespace TeamStride.Infrastructure.Tests.Examples;

/// <summary>
/// Example test class demonstrating the simplified testing approach
/// Compare this to the existing TeamServiceTests to see the difference
/// </summary>
public class SimplifiedTeamServiceTest : BaseSecuredTest
{
    private readonly Mock<IAuthorizationService> _mockAuthorizationService;
    private readonly Mock<ILogger<TeamService>> _mockLogger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;

    public SimplifiedTeamServiceTest()
    {
        _mockAuthorizationService = new Mock<IAuthorizationService>();
        _mockLogger = new Mock<ILogger<TeamService>>();
        _userManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
    }

    [Fact]
    public async Task GetTeamsAsync_AsGlobalAdmin_ShouldReturnAllTeams()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        var service = new TeamService(
            DbContext,
            _mockAuthorizationService.Object,
            MockCurrentUserService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object);

        var owner1 = await CreateTestUserAsync("owner1@test.com", "Owner1", "User");
        var owner2 = await CreateTestUserAsync("owner2@test.com", "Owner2", "User");
        
        var team1 = await CreateTestTeamAsync("Team Alpha", "team-alpha", owner1.Id);
        var team2 = await CreateTestTeamAsync("Team Beta", "team-beta", owner2.Id);

        await CreateUserTeamRelationshipAsync(owner1.Id, team1.Id, TeamRole.TeamOwner, MemberType.Coach);
        await CreateUserTeamRelationshipAsync(owner2.Id, team2.Id, TeamRole.TeamOwner, MemberType.Coach);

        // Act
        var result = await service.GetTeamsAsync();

        // Assert
        result.Items.Count.ShouldBe(2);
        result.Items.ShouldContain(t => t.Name == "Team Alpha");
        result.Items.ShouldContain(t => t.Name == "Team Beta");
    }

    [Fact]
    public async Task GetTeamsAsync_AsStandardUser_ShouldReturnOnlyAccessibleTeams()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamMember, userId);
        
        var service = new TeamService(
            DbContext,
            _mockAuthorizationService.Object,
            MockCurrentUserService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object);

        var myUser = await CreateTestUserAsync("user@test.com", "Test", "User", userId);
        var owner1 = await CreateTestUserAsync("owner1@test.com", "Owner1", "User");
        var owner2 = await CreateTestUserAsync("owner2@test.com", "Owner2", "User");
        
        var team1 = await CreateTestTeamAsync("My Team", "my-team", owner1.Id, teamId);
        var team2 = await CreateTestTeamAsync("Other Team", "other-team", owner2.Id);

        // Create user relationship to "My Team"
        await CreateUserTeamRelationshipAsync(owner1.Id, team1.Id, TeamRole.TeamOwner);
        await CreateUserTeamRelationshipAsync(owner2.Id, team2.Id, TeamRole.TeamOwner);
        await CreateUserTeamRelationshipAsync(myUser.Id, team1.Id, TeamRole.TeamOwner);
        
        // Act
        var result = await service.GetTeamsAsync();

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items.First().Name.ShouldBe("My Team");
    }

    [Fact]
    public async Task GetTeamsAsync_AsAnonymousUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        SetupAnonymousContext();
        
        var service = new TeamService(
            DbContext,
            _mockAuthorizationService.Object,
            MockCurrentUserService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object);

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => service.GetTeamsAsync());
    }

    [Fact]
    public async Task GetTeamByIdAsync_AsTeamOwner_ShouldReturnTeam()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamOwner, userId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamMember))
            .Returns(Task.CompletedTask);

        var service = new TeamService(
            DbContext,
            _mockAuthorizationService.Object,
            MockCurrentUserService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object);

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner", userId);
        await CreateTestTeamAsync("Test Team", "test-team", owner.Id, teamId);
        await CreateUserTeamRelationshipAsync(userId, teamId, TeamRole.TeamOwner);

        // Act
        var result = await service.GetTeamByIdAsync(teamId);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Test Team");
        result.Owner.ShouldNotBeNull();
        result.Owner.Email.ShouldBe("owner@test.com");
    }

    [Fact]
    public async Task GetTeamByIdAsync_AsAthlete_WithTeamAccess_ShouldReturnTeam()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var athleteId = Guid.NewGuid();
        
        SetupAthleteContext(teamId, athleteId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamMember))
            .Returns(Task.CompletedTask);

        var service = new TeamService(
            DbContext,
            _mockAuthorizationService.Object,
            MockCurrentUserService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object);

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner");
        var athlete = await CreateTestUserAsync("athlete@test.com", "Athlete", "User", athleteId);
        
        await CreateTestTeamAsync("Test Team", "test-team", owner.Id, teamId);
        await CreateUserTeamRelationshipAsync(owner.Id, teamId, TeamRole.TeamOwner);
        await CreateUserTeamRelationshipAsync(athleteId, teamId, TeamRole.TeamMember, MemberType.Athlete);

        // Act
        var result = await service.GetTeamByIdAsync(teamId);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Test Team");
    }

    // Helper methods
    private async Task<ApplicationUser> CreateTestUserAsync(string email, string firstName, string lastName, Guid? id = null)
    {
        var user = new ApplicationUser
        {
            Id = id ?? Guid.NewGuid(),
            Email = email,
            NormalizedEmail = email.ToUpper(),
            UserName = email,
            NormalizedUserName = email.ToUpper(),
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true,
            Status = UserStatus.Active,
            CreatedOn = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, "TestPassword123!");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return user;
    }

    private async Task<Team> CreateTestTeamAsync(string name, string subdomain, Guid ownerId, Guid? id = null)
    {
        var team = new Team
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Subdomain = subdomain,
            OwnerId = ownerId,
            Tier = TeamTier.Free,
            Status = TeamStatus.Active,
            PrimaryColor = "#000000",
            SecondaryColor = "#FFFFFF",
            CreatedOn = DateTime.UtcNow
        };

        DbContext.Teams.Add(team);
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
            JoinedOn = DateTime.UtcNow,
            CreatedOn = DateTime.UtcNow
        };

        DbContext.UserTeams.Add(userTeam);
        await DbContext.SaveChangesAsync();
        return userTeam;
    }
} 