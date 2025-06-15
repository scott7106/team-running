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
using TeamStride.Domain.Interfaces;
using TeamStride.Infrastructure.Mapping;
using TeamStride.Infrastructure.Services;

namespace TeamStride.Infrastructure.Tests.Services;

public class StandardTeamServiceTests : BaseSecuredTest
{
    private readonly Mock<IAuthorizationService> _mockAuthorizationService;
    private readonly Mock<ILogger<StandardTeamService>> _mockLogger;
    private readonly Mock<ITeamManager> _mockTeamManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;

    public StandardTeamServiceTests()
    {
        _mockAuthorizationService = new Mock<IAuthorizationService>();
        _mockLogger = new Mock<ILogger<StandardTeamService>>();
        _mockTeamManager = new Mock<ITeamManager>();
        _userManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
    }

    #region GetTeamsAsync Tests

    [Fact]
    public async Task GetTeamsAsync_AsGlobalAdmin_ShouldReturnAllTeams()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        var service = CreateService();

        var owner1 = await CreateTestUserAsync("owner1@test.com", "Owner1", "User");
        var owner2 = await CreateTestUserAsync("owner2@test.com", "Owner2", "User");
        
        var team1 = await CreateTestTeamAsync("Team Alpha", "team-alpha", owner1.Id);
        var team2 = await CreateTestTeamAsync("Team Beta", "team-beta", owner2.Id);

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
        
        var service = CreateService();

        var myUser = await CreateTestUserAsync("user@test.com", "Test", "User", userId);
        var owner1 = await CreateTestUserAsync("owner1@test.com", "Owner1", "User");
        var owner2 = await CreateTestUserAsync("owner2@test.com", "Owner2", "User");
        
        var team1 = await CreateTestTeamAsync("My Team", "my-team", owner1.Id, teamId);
        var team2 = await CreateTestTeamAsync("Other Team", "other-team", owner2.Id);

        // Create user relationship to "My Team"
        await CreateUserTeamRelationshipAsync(myUser.Id, team1.Id, TeamRole.TeamMember);
        
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
        var service = CreateService();

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => service.GetTeamsAsync());
    }

    [Fact]
    public async Task GetTeamsAsync_WithSearchQuery_ShouldFilterResults()
    {
        // Arrange
        SetupGlobalAdminContext();
        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        await CreateTestTeamAsync("Alpha Team", "alpha-team", owner.Id);
        await CreateTestTeamAsync("Beta Squad", "beta-squad", owner.Id);
        await CreateTestTeamAsync("Gamma Group", "gamma-group", owner.Id);

        // Act
        var result = await service.GetTeamsAsync(searchQuery: "alpha");

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items.First().Name.ShouldBe("Alpha Team");
    }

    [Fact]
    public async Task GetTeamsAsync_WithStatusFilter_ShouldFilterResults()
    {
        // Arrange
        SetupGlobalAdminContext();
        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        await CreateTestTeamAsync("Active Team", "active-team", owner.Id, status: TeamStatus.Active);
        await CreateTestTeamAsync("Suspended Team", "suspended-team", owner.Id, status: TeamStatus.Suspended);

        // Act
        var result = await service.GetTeamsAsync(status: TeamStatus.Active);

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items.First().Name.ShouldBe("Active Team");
    }

    [Fact]
    public async Task GetTeamsAsync_WithTierFilter_ShouldFilterResults()
    {
        // Arrange
        SetupGlobalAdminContext();
        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        await CreateTestTeamAsync("Free Team", "free-team", owner.Id, tier: TeamTier.Free);
        await CreateTestTeamAsync("Premium Team", "premium-team", owner.Id, tier: TeamTier.Premium);

        // Act
        var result = await service.GetTeamsAsync(tier: TeamTier.Premium);

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items.First().Name.ShouldBe("Premium Team");
    }

    [Fact]
    public async Task GetTeamsAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        SetupGlobalAdminContext();
        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        for (int i = 1; i <= 5; i++)
        {
            await CreateTestTeamAsync($"Team {i:D2}", $"team-{i:D2}", owner.Id);
        }

        // Act
        var result = await service.GetTeamsAsync(pageNumber: 2, pageSize: 2);

        // Assert
        result.PageNumber.ShouldBe(2);
        result.TotalCount.ShouldBe(5);
        result.Items.Count.ShouldBe(2);
        result.Items.First().Name.ShouldBe("Team 03"); // Ordered by name
    }

    #endregion

    #region GetTeamByIdAsync Tests

    [Fact]
    public async Task GetTeamByIdAsync_AsTeamOwner_ShouldReturnTeam()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamOwner, userId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamMember))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner", userId);
        await CreateTestTeamAsync("Test Team", "test-team", owner.Id, teamId);

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

        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner");
        var athlete = await CreateTestUserAsync("athlete@test.com", "Athlete", "User", athleteId);
        
        await CreateTestTeamAsync("Test Team", "test-team", owner.Id, teamId);
        await CreateUserTeamRelationshipAsync(athleteId, teamId, TeamRole.TeamMember, MemberType.Athlete);

        // Act
        var result = await service.GetTeamByIdAsync(teamId);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Test Team");
    }

    [Fact]
    public async Task GetTeamByIdAsync_WithoutAccess_ShouldCallAuthorizationService()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var service = CreateService();

        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamMember))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => service.GetTeamByIdAsync(teamId));

        _mockAuthorizationService.Verify(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamMember), Times.Once);
    }

    [Fact]
    public async Task GetTeamByIdAsync_TeamNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamMember))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => service.GetTeamByIdAsync(teamId));
    }

    #endregion

    #region GetTeamBySubdomainAsync Tests

    [Fact]
    public async Task GetTeamBySubdomainAsync_WithValidSubdomain_ShouldReturnTeam()
    {
        // Arrange
        SetupGlobalAdminContext();
        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner");
        var team = await CreateTestTeamAsync("Test Team", "test-team", owner.Id);

        _mockTeamManager.Setup(x => x.GetTeamBySubdomainAsync("test-team"))
            .ReturnsAsync(team);

        // Act
        var result = await service.GetTeamBySubdomainAsync("test-team");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Test Team");
        result.Subdomain.ShouldBe("test-team");
        _mockTeamManager.Verify(x => x.GetTeamBySubdomainAsync("test-team"), Times.Once);
    }

    [Fact]
    public async Task GetTeamBySubdomainAsync_AsAnonymousUser_ShouldReturnTeam()
    {
        // Arrange
        SetupAnonymousContext();
        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner");
        var team = await CreateTestTeamAsync("Public Team", "public-team", owner.Id);

        _mockTeamManager.Setup(x => x.GetTeamBySubdomainAsync("public-team"))
            .ReturnsAsync(team);

        // Act
        var result = await service.GetTeamBySubdomainAsync("public-team");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Public Team");
        _mockTeamManager.Verify(x => x.GetTeamBySubdomainAsync("public-team"), Times.Once);
    }

    [Fact]
    public async Task GetTeamBySubdomainAsync_AsStandardUserWithoutAccess_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupStandardUserContext(teamId, TeamRole.TeamMember, userId);
        
        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner");
        var restrictedTeam = await CreateTestTeamAsync("Restricted Team", "restricted-team", owner.Id);

        // Mock TeamManager to return the team
        _mockTeamManager.Setup(x => x.GetTeamBySubdomainAsync("restricted-team"))
            .ReturnsAsync(restrictedTeam);

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => service.GetTeamBySubdomainAsync("restricted-team"));
    }

    [Fact]
    public async Task GetTeamBySubdomainAsync_WithEmptySubdomain_ShouldThrowArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => service.GetTeamBySubdomainAsync(""));
    }

    [Fact]
    public async Task GetTeamBySubdomainAsync_WithNonExistentSubdomain_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var service = CreateService();

        _mockTeamManager.Setup(x => x.GetTeamBySubdomainAsync("non-existent"))
            .ThrowsAsync(new InvalidOperationException("Team not found"));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => service.GetTeamBySubdomainAsync("non-existent"));
    }

    #endregion



    #region UpdateTeamAsync Tests

    [Fact]
    public async Task UpdateTeamAsync_AsTeamOwner_WithValidData_ShouldUpdateTeam()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupStandardUserContext(teamId, TeamRole.TeamOwner, userId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamOwnershipAsync(teamId))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner", userId);
        await CreateTestTeamAsync("Original Team", "original-team", owner.Id, teamId);

        var dto = new UpdateTeamDto
        {
            Name = "Updated Team"
        };

        // Act
        var result = await service.UpdateTeamAsync(teamId, dto);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Updated Team");
    }

    [Fact]
    public async Task UpdateTeamAsync_AsNonOwner_ShouldCallAuthorizationService()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        SetupStandardUserContext(teamId, TeamRole.TeamAdmin);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin))
            .ThrowsAsync(new UnauthorizedAccessException());

        var service = CreateService();

        // Create a team so the authorization check can be reached
        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner");
        await CreateTestTeamAsync("Test Team", "test-team", owner.Id, teamId);

        var dto = new UpdateTeamDto { Name = "Updated Team" };

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => service.UpdateTeamAsync(teamId, dto));

        _mockAuthorizationService.Verify(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin), Times.Once);
    }

    #endregion



    #region IsSubdomainAvailableAsync Tests

    [Fact]
    public async Task IsSubdomainAvailableAsync_WithAvailableSubdomain_ShouldReturnTrue()
    {
        // Arrange
        var service = CreateService();

        _mockTeamManager.Setup(x => x.IsSubdomainAvailableAsync("available-subdomain", null))
            .ReturnsAsync(true);

        // Act
        var result = await service.IsSubdomainAvailableAsync("available-subdomain");

        // Assert
        result.ShouldBeTrue();
        _mockTeamManager.Verify(x => x.IsSubdomainAvailableAsync("available-subdomain", null), Times.Once);
    }

    [Fact]
    public async Task IsSubdomainAvailableAsync_WithTakenSubdomain_ShouldReturnFalse()
    {
        // Arrange
        var service = CreateService();

        _mockTeamManager.Setup(x => x.IsSubdomainAvailableAsync("taken-subdomain", null))
            .ReturnsAsync(false);

        // Act
        var result = await service.IsSubdomainAvailableAsync("taken-subdomain");

        // Assert
        result.ShouldBeFalse();
        _mockTeamManager.Verify(x => x.IsSubdomainAvailableAsync("taken-subdomain", null), Times.Once);
    }

    [Fact]
    public async Task IsSubdomainAvailableAsync_WithDeletedTeamSubdomain_ShouldReturnTrue()
    {
        // Arrange
        var service = CreateService();

        _mockTeamManager.Setup(x => x.IsSubdomainAvailableAsync("deleted-subdomain", null))
            .ReturnsAsync(true);

        // Act
        var result = await service.IsSubdomainAvailableAsync("deleted-subdomain");

        // Assert
        result.ShouldBeTrue();
        _mockTeamManager.Verify(x => x.IsSubdomainAvailableAsync("deleted-subdomain", null), Times.Once);
    }

    [Fact]
    public async Task IsSubdomainAvailableAsync_WithEmptySubdomain_ShouldThrowArgumentException()
    {
        // Arrange
        var service = CreateService();

        _mockTeamManager.Setup(x => x.IsSubdomainAvailableAsync("", null))
            .ThrowsAsync(new ArgumentException("Subdomain cannot be empty"));

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => service.IsSubdomainAvailableAsync(""));
    }

    #endregion

    #region UpdateSubdomainAsync Tests

    [Fact]
    public async Task UpdateSubdomainAsync_AsTeamOwner_WithAvailableSubdomain_ShouldUpdateSubdomain()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupStandardUserContext(teamId, TeamRole.TeamOwner, userId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamOwnershipAsync(teamId))
            .Returns(Task.CompletedTask);

        // Mock TeamManager subdomain normalization and availability check
        _mockTeamManager.Setup(x => x.NormalizeSubdomain("new-subdomain"))
            .Returns("new-subdomain");
        _mockTeamManager.Setup(x => x.IsSubdomainAvailableAsync("new-subdomain", teamId))
            .ReturnsAsync(true);

        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner", userId);
        await CreateTestTeamAsync("Test Team", "old-subdomain", owner.Id, teamId);
        await CreateUserTeamRelationshipAsync(userId, teamId, TeamRole.TeamOwner);

        // Act
        var result = await service.UpdateSubdomainAsync(teamId, "new-subdomain");

        // Assert
        result.ShouldNotBeNull();
        result.Subdomain.ShouldBe("new-subdomain");
        _mockTeamManager.Verify(x => x.IsSubdomainAvailableAsync("new-subdomain", teamId), Times.Once);
    }

    [Fact]
    public async Task UpdateSubdomainAsync_WithTakenSubdomain_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupStandardUserContext(teamId, TeamRole.TeamOwner, userId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamOwnershipAsync(teamId))
            .Returns(Task.CompletedTask);

        // Mock TeamManager subdomain normalization and availability check
        _mockTeamManager.Setup(x => x.NormalizeSubdomain("taken-subdomain"))
            .Returns("taken-subdomain");
        _mockTeamManager.Setup(x => x.IsSubdomainAvailableAsync("taken-subdomain", teamId))
            .ReturnsAsync(false);

        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner", userId);
        await CreateTestTeamAsync("Team 1", "team-1", owner.Id, teamId);
        await CreateUserTeamRelationshipAsync(userId, teamId, TeamRole.TeamOwner);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => service.UpdateSubdomainAsync(teamId, "taken-subdomain"));
        
        _mockTeamManager.Verify(x => x.IsSubdomainAvailableAsync("taken-subdomain", teamId), Times.Once);
    }

    #endregion

    #region GetTierLimitsAsync Tests

    [Fact]
    public async Task GetTierLimitsAsync_WithFreeTier_ShouldReturnFreeLimits()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetTierLimitsAsync(TeamTier.Free);

        // Assert
        result.ShouldNotBeNull();
        result.Tier.ShouldBe(TeamTier.Free);
        result.MaxAthletes.ShouldBe(7);
        result.MaxAdmins.ShouldBe(2);
        result.MaxCoaches.ShouldBe(2);
        result.AllowCustomBranding.ShouldBeFalse();
        result.AllowAdvancedReporting.ShouldBeFalse();
    }

    [Fact]
    public async Task GetTierLimitsAsync_WithStandardTier_ShouldReturnStandardLimits()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetTierLimitsAsync(TeamTier.Standard);

        // Assert
        result.ShouldNotBeNull();
        result.Tier.ShouldBe(TeamTier.Standard);
        result.MaxAthletes.ShouldBe(30);
        result.MaxAdmins.ShouldBe(5);
        result.MaxCoaches.ShouldBe(5);
        result.AllowCustomBranding.ShouldBeFalse();
        result.AllowAdvancedReporting.ShouldBeFalse();
    }

    [Fact]
    public async Task GetTierLimitsAsync_WithPremiumTier_ShouldReturnPremiumLimits()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetTierLimitsAsync(TeamTier.Premium);

        // Assert
        result.ShouldNotBeNull();
        result.Tier.ShouldBe(TeamTier.Premium);
        result.MaxAthletes.ShouldBe(int.MaxValue);
        result.MaxAdmins.ShouldBe(int.MaxValue);
        result.MaxCoaches.ShouldBe(int.MaxValue);
        result.AllowCustomBranding.ShouldBeTrue();
        result.AllowAdvancedReporting.ShouldBeTrue();
    }

    #endregion

    #region CanAddAthleteAsync Tests

    [Fact]
    public async Task CanAddAthleteAsync_FreeTierBelowLimit_ShouldReturnTrue()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupStandardUserContext(teamId, TeamRole.TeamOwner, userId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamMember))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner", userId);
        await CreateTestTeamAsync("Free Team", "free-team", owner.Id, teamId, tier: TeamTier.Free);

        // Add 3 athletes (below limit of 7)
        for (int i = 1; i <= 3; i++)
        {
            var athlete = await CreateTestUserAsync($"athlete{i}@test.com", "Athlete", $"{i}");
            await CreateUserTeamRelationshipAsync(athlete.Id, teamId, TeamRole.TeamMember, MemberType.Athlete);
        }

        // Act
        var result = await service.CanAddAthleteAsync(teamId);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task CanAddAthleteAsync_FreeTierAtLimit_ShouldReturnFalse()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupStandardUserContext(teamId, TeamRole.TeamOwner, userId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamMember))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner", userId);
        await CreateTestTeamAsync("Free Team", "free-team", owner.Id, teamId, tier: TeamTier.Free);

        // Add 7 athletes (at limit)
        for (int i = 1; i <= 7; i++)
        {
            var athlete = await CreateTestUserAsync($"athlete{i}@test.com", "Athlete", $"{i}");
            await CreateUserTeamRelationshipAsync(athlete.Id, teamId, TeamRole.TeamMember, MemberType.Athlete);
        }

        // Act
        var result = await service.CanAddAthleteAsync(teamId);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task CanAddAthleteAsync_PremiumTier_ShouldReturnTrue()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupStandardUserContext(teamId, TeamRole.TeamOwner, userId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamMember))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner", userId);
        await CreateTestTeamAsync("Premium Team", "premium-team", owner.Id, teamId, tier: TeamTier.Premium);

        // Act
        var result = await service.CanAddAthleteAsync(teamId);

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region GetTeamMembersAsync Tests

    [Fact]
    public async Task GetTeamMembersAsync_AsTeamAdmin_ShouldReturnMembers()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupStandardUserContext(teamId, TeamRole.TeamAdmin, userId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamMember))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner");
        var admin = await CreateTestUserAsync("admin@test.com", "Team", "Admin", userId);
        var athlete = await CreateTestUserAsync("athlete@test.com", "Team", "Athlete");
        
        await CreateTestTeamAsync("Test Team", "test-team", owner.Id, teamId);
        await CreateUserTeamRelationshipAsync(admin.Id, teamId, TeamRole.TeamAdmin, MemberType.Coach);
        await CreateUserTeamRelationshipAsync(athlete.Id, teamId, TeamRole.TeamMember, MemberType.Athlete);

        // Act
        var result = await service.GetTeamMembersAsync(teamId);

        // Assert
        result.Items.Count.ShouldBe(3);
        result.Items.ShouldContain(m => m.Email == "owner@test.com" && m.Role == TeamRole.TeamOwner);
        result.Items.ShouldContain(m => m.Email == "admin@test.com" && m.Role == TeamRole.TeamAdmin);
        result.Items.ShouldContain(m => m.Email == "athlete@test.com" && m.Role == TeamRole.TeamMember);
    }

    [Fact]
    public async Task GetTeamMembersAsync_WithRoleFilter_ShouldFilterResults()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupStandardUserContext(teamId, TeamRole.TeamAdmin, userId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamMember))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner");
        var athlete = await CreateTestUserAsync("athlete@test.com", "Team", "Athlete");
        
        await CreateTestTeamAsync("Test Team", "test-team", owner.Id, teamId);
        await CreateUserTeamRelationshipAsync(athlete.Id, teamId, TeamRole.TeamMember, MemberType.Athlete);

        // Act
        var result = await service.GetTeamMembersAsync(teamId, role: TeamRole.TeamMember);

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items.First().Email.ShouldBe("athlete@test.com");
        result.Items.First().Role.ShouldBe(TeamRole.TeamMember);
    }

    #endregion

    #region UpdateMemberRoleAsync Tests

    [Fact]
    public async Task UpdateMemberRoleAsync_AsTeamOwner_ShouldUpdateRole()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamOwner, ownerId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner", ownerId);
        var member = await CreateTestUserAsync("member@test.com", "Team", "Member", memberId);
        
        await CreateTestTeamAsync("Test Team", "test-team", owner.Id, teamId);
        await CreateUserTeamRelationshipAsync(memberId, teamId, TeamRole.TeamMember);

        // Act
        var result = await service.UpdateMemberRoleAsync(teamId, memberId, TeamRole.TeamAdmin);

        // Assert
        result.ShouldNotBeNull();
        result.Role.ShouldBe(TeamRole.TeamAdmin);
        result.Email.ShouldBe("member@test.com");
    }

    [Fact]
    public async Task UpdateMemberRoleAsync_TryingToChangeOwnerRole_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamOwner, ownerId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner", ownerId);
        await CreateTestTeamAsync("Test Team", "test-team", owner.Id, teamId);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => service.UpdateMemberRoleAsync(teamId, ownerId, TeamRole.TeamMember));
    }

    #endregion

    #region RemoveMemberAsync Tests

    [Fact]
    public async Task RemoveMemberAsync_AsTeamAdmin_ShouldRemoveMember()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamAdmin, adminId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner");
        var admin = await CreateTestUserAsync("admin@test.com", "Team", "Admin", adminId);
        var member = await CreateTestUserAsync("member@test.com", "Team", "Member", memberId);
        
        await CreateTestTeamAsync("Test Team", "test-team", owner.Id, teamId);
        await CreateUserTeamRelationshipAsync(adminId, teamId, TeamRole.TeamAdmin);
        var userTeam = await CreateUserTeamRelationshipAsync(memberId, teamId, TeamRole.TeamMember);

        // Act
        await service.RemoveMemberAsync(teamId, memberId);

        // Assert
        var updatedUserTeam = await DbContext.UserTeams.FindAsync(userTeam.Id);
        updatedUserTeam!.IsActive.ShouldBeFalse();
        updatedUserTeam.ModifiedOn.ShouldNotBeNull();
    }

    [Fact]
    public async Task RemoveMemberAsync_TryingToRemoveOwner_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamOwner, ownerId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner", ownerId);
        await CreateTestTeamAsync("Test Team", "test-team", owner.Id, teamId);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => service.RemoveMemberAsync(teamId, ownerId));
    }

    [Fact]
    public async Task RemoveMemberAsync_NonExistentMember_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var nonExistentId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamAdmin, adminId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner");
        await CreateTestTeamAsync("Test Team", "test-team", owner.Id, teamId);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => service.RemoveMemberAsync(teamId, nonExistentId));
    }

    #endregion

    #region InitiateOwnershipTransferAsync Tests

    [Fact]
    public async Task InitiateOwnershipTransferAsync_AsTeamOwner_WithValidData_ShouldCreateTransfer()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamOwner, ownerId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamOwnershipAsync(teamId))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner", ownerId);
        await CreateTestTeamAsync("Test Team", "test-team", owner.Id, teamId);

        var dto = new InitiateOwnershipTransferDto
        {
            NewOwnerEmail = "newowner@test.com",
            NewOwnerFirstName = "New",
            NewOwnerLastName = "Owner",
            Message = "Transfer ownership to new owner"
        };

        // Act
        var result = await service.InitiateOwnershipTransferAsync(teamId, dto);

        // Assert
        result.ShouldNotBeNull();
        result.NewOwnerEmail.ShouldBe("newowner@test.com");
        result.Status.ShouldBe(OwnershipTransferStatus.Pending);
        result.TransferToken.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task InitiateOwnershipTransferAsync_WithExistingPendingTransfer_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamOwner, ownerId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamOwnershipAsync(teamId))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner", ownerId);
        await CreateTestTeamAsync("Test Team", "test-team", owner.Id, teamId);

        // Create existing pending transfer
        var existingTransfer = new OwnershipTransfer
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            InitiatedByUserId = ownerId,
            NewOwnerEmail = "existing@test.com",
            Status = OwnershipTransferStatus.Pending,
            TransferToken = "existing-token",
            ExpiresOn = DateTime.UtcNow.AddDays(7),
            CreatedOn = DateTime.UtcNow
        };
        DbContext.OwnershipTransfers.Add(existingTransfer);
        await DbContext.SaveChangesAsync();

        var dto = new InitiateOwnershipTransferDto
        {
            NewOwnerEmail = "newowner@test.com",
            NewOwnerFirstName = "New",
            NewOwnerLastName = "Owner"
        };

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => service.InitiateOwnershipTransferAsync(teamId, dto));
    }

    [Fact]
    public async Task InitiateOwnershipTransferAsync_WithNullDto_ShouldThrowArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => service.InitiateOwnershipTransferAsync(Guid.NewGuid(), null!));
    }

    #endregion

    #region CompleteOwnershipTransferAsync Tests

    [Fact]
    public async Task CompleteOwnershipTransferAsync_WithValidToken_ShouldCompleteTransfer()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var oldOwnerId = Guid.NewGuid();
        var newOwnerId = Guid.NewGuid();
        var transferToken = Guid.NewGuid().ToString("N");
        
        SetupStandardUserContext(teamId, TeamRole.TeamMember, newOwnerId);

        var service = CreateService();

        var oldOwner = await CreateTestUserAsync("oldowner@test.com", "Old", "Owner", oldOwnerId);
        var newOwner = await CreateTestUserAsync("newowner@test.com", "New", "Owner", newOwnerId);
        var team = await CreateTestTeamAsync("Test Team", "test-team", oldOwner.Id, teamId);

        // Create ownership transfer
        var transfer = new OwnershipTransfer
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            InitiatedByUserId = oldOwnerId,
            NewOwnerEmail = "newowner@test.com",
            Status = OwnershipTransferStatus.Pending,
            TransferToken = transferToken,
            ExpiresOn = DateTime.UtcNow.AddDays(7),
            CreatedOn = DateTime.UtcNow,
            Team = team
        };
        DbContext.OwnershipTransfers.Add(transfer);

        await DbContext.SaveChangesAsync();

        // Act
        var result = await service.CompleteOwnershipTransferAsync(transferToken);

        // Assert
        result.ShouldNotBeNull();
        result.Owner.ShouldNotBeNull();
        result.Owner.Email.ShouldBe("newowner@test.com");

        // Check transfer status
        var updatedTransfer = await DbContext.OwnershipTransfers.FindAsync(transfer.Id);
        updatedTransfer.ShouldNotBeNull();
        updatedTransfer.Status.ShouldBe(OwnershipTransferStatus.Completed);
        updatedTransfer.CompletedOn.ShouldNotBeNull();
    }

    [Fact]
    public async Task CompleteOwnershipTransferAsync_WithInvalidToken_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => service.CompleteOwnershipTransferAsync("invalid-token"));
    }

    [Fact]
    public async Task CompleteOwnershipTransferAsync_WithExpiredToken_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var oldOwnerId = Guid.NewGuid();
        var newOwnerId = Guid.NewGuid();
        var transferToken = Guid.NewGuid().ToString("N");
        
        SetupStandardUserContext(teamId, TeamRole.TeamMember, newOwnerId);

        var service = CreateService();

        var oldOwner = await CreateTestUserAsync("oldowner@test.com", "Old", "Owner", oldOwnerId);
        var newOwner = await CreateTestUserAsync("newowner@test.com", "New", "Owner", newOwnerId);
        var team = await CreateTestTeamAsync("Test Team", "test-team", oldOwner.Id, teamId);

        // Create expired ownership transfer
        var transfer = new OwnershipTransfer
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            InitiatedByUserId = oldOwnerId,
            NewOwnerEmail = "newowner@test.com",
            Status = OwnershipTransferStatus.Pending,
            TransferToken = transferToken,
            ExpiresOn = DateTime.UtcNow.AddDays(-1), // Expired
            CreatedOn = DateTime.UtcNow.AddDays(-8),
            Team = team
        };
        DbContext.OwnershipTransfers.Add(transfer);
        await DbContext.SaveChangesAsync();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => service.CompleteOwnershipTransferAsync(transferToken));
    }

    [Fact]
    public async Task CompleteOwnershipTransferAsync_AsWrongUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var oldOwnerId = Guid.NewGuid();
        var newOwnerId = Guid.NewGuid();
        var wrongUserId = Guid.NewGuid();
        var transferToken = Guid.NewGuid().ToString("N");
        
        SetupStandardUserContext(teamId, TeamRole.TeamMember, wrongUserId);

        var service = CreateService();

        var oldOwner = await CreateTestUserAsync("oldowner@test.com", "Old", "Owner", oldOwnerId);
        var newOwner = await CreateTestUserAsync("newowner@test.com", "New", "Owner", newOwnerId);
        var wrongUser = await CreateTestUserAsync("wrong@test.com", "Wrong", "User", wrongUserId);
        var team = await CreateTestTeamAsync("Test Team", "test-team", oldOwner.Id, teamId);

        // Create ownership transfer
        var transfer = new OwnershipTransfer
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            InitiatedByUserId = oldOwnerId,
            NewOwnerEmail = "newowner@test.com",
            Status = OwnershipTransferStatus.Pending,
            TransferToken = transferToken,
            ExpiresOn = DateTime.UtcNow.AddDays(7),
            CreatedOn = DateTime.UtcNow,
            Team = team
        };
        DbContext.OwnershipTransfers.Add(transfer);
        await DbContext.SaveChangesAsync();

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => service.CompleteOwnershipTransferAsync(transferToken));
    }

    #endregion

    #region CancelOwnershipTransferAsync Tests

    [Fact]
    public async Task CancelOwnershipTransferAsync_AsTeamOwner_ShouldCancelTransfer()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var transferId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamOwner, ownerId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamOwnershipAsync(teamId))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner", ownerId);
        var team = await CreateTestTeamAsync("Test Team", "test-team", owner.Id, teamId);

        // Create ownership transfer
        var transfer = new OwnershipTransfer
        {
            Id = transferId,
            TeamId = teamId,
            InitiatedByUserId = ownerId,
            NewOwnerEmail = "newowner@test.com",
            Status = OwnershipTransferStatus.Pending,
            TransferToken = "transfer-token",
            ExpiresOn = DateTime.UtcNow.AddDays(7),
            CreatedOn = DateTime.UtcNow,
            Team = team
        };
        DbContext.OwnershipTransfers.Add(transfer);
        await DbContext.SaveChangesAsync();

        // Act
        await service.CancelOwnershipTransferAsync(transferId);

        // Assert
        var cancelledTransfer = await DbContext.OwnershipTransfers.FindAsync(transferId);
        cancelledTransfer.ShouldNotBeNull();
        cancelledTransfer.Status.ShouldBe(OwnershipTransferStatus.Cancelled);
        cancelledTransfer.ModifiedOn.ShouldNotBeNull();
    }

    [Fact]
    public async Task CancelOwnershipTransferAsync_WithNonExistentTransfer_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => service.CancelOwnershipTransferAsync(Guid.NewGuid()));
    }

    #endregion

    #region GetPendingTransfersAsync Tests

    [Fact]
    public async Task GetPendingTransfersAsync_AsTeamOwner_ShouldReturnPendingTransfers()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamOwner, ownerId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner", ownerId);
        var team = await CreateTestTeamAsync("Test Team", "test-team", owner.Id, teamId);

        // Create pending transfers
        var transfer1 = new OwnershipTransfer
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            InitiatedByUserId = ownerId,
            NewOwnerEmail = "newowner1@test.com",
            Status = OwnershipTransferStatus.Pending,
            TransferToken = "token1",
            ExpiresOn = DateTime.UtcNow.AddDays(7),
            CreatedOn = DateTime.UtcNow,
            Team = team
        };

        var transfer2 = new OwnershipTransfer
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            InitiatedByUserId = ownerId,
            NewOwnerEmail = "newowner2@test.com",
            Status = OwnershipTransferStatus.Completed,
            TransferToken = "token2",
            ExpiresOn = DateTime.UtcNow.AddDays(7),
            CreatedOn = DateTime.UtcNow,
            Team = team
        };

        DbContext.OwnershipTransfers.AddRange(transfer1, transfer2);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await service.GetPendingTransfersAsync(teamId);

        // Assert
        result.Count().ShouldBe(1);
        result.First().NewOwnerEmail.ShouldBe("newowner1@test.com");
        result.First().Status.ShouldBe(OwnershipTransferStatus.Pending);
    }

    #endregion

    #region UpdateSubscriptionAsync Tests

    [Fact]
    public async Task UpdateSubscriptionAsync_AsTeamOwner_WithValidData_ShouldUpdateSubscription()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamOwner, ownerId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamOwnershipAsync(teamId))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner", ownerId);
        await CreateTestTeamAsync("Test Team", "test-team", owner.Id, teamId, tier: TeamTier.Free);

        var dto = new UpdateSubscriptionDto
        {
            NewTier = TeamTier.Premium,
            ExpiresOn = DateTime.UtcNow.AddYears(1)
        };

        // Act
        var result = await service.UpdateSubscriptionAsync(teamId, dto);

        // Assert
        result.ShouldNotBeNull();
        result.Tier.ShouldBe(TeamTier.Premium);
        result.ExpiresOn.ShouldNotBeNull();
    }

    [Fact]
    public async Task UpdateSubscriptionAsync_AsNonOwner_ShouldCallAuthorizationService()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        SetupStandardUserContext(teamId, TeamRole.TeamMember);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamOwnershipAsync(teamId))
            .ThrowsAsync(new UnauthorizedAccessException());

        var service = CreateService();

        var dto = new UpdateSubscriptionDto
        {
            NewTier = TeamTier.Premium
        };

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => service.UpdateSubscriptionAsync(teamId, dto));

        _mockAuthorizationService.Verify(x => x.RequireTeamOwnershipAsync(teamId), Times.Once);
    }

    #endregion

    #region UpdateBrandingAsync Tests

    [Fact]
    public async Task UpdateBrandingAsync_AsTeamOwner_WithValidData_ShouldUpdateBranding()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamOwner, ownerId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner", ownerId);
        await CreateTestTeamAsync("Test Team", "test-team", owner.Id, teamId, tier: TeamTier.Premium);

        var dto = new UpdateTeamBrandingDto
        {
            LogoUrl = "https://example.com/logo.png",
            PrimaryColor = "#FF0000",
            SecondaryColor = "#0000FF"
        };

        // Act
        var result = await service.UpdateBrandingAsync(teamId, dto);

        // Assert
        result.ShouldNotBeNull();
        result.LogoUrl.ShouldBe("https://example.com/logo.png");
        result.PrimaryColor.ShouldBe("#FF0000");
        result.SecondaryColor.ShouldBe("#0000FF");
    }

    [Fact]
    public async Task UpdateBrandingAsync_WithFreeTier_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        
        SetupStandardUserContext(teamId, TeamRole.TeamOwner, ownerId);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner", ownerId);
        await CreateTestTeamAsync("Test Team", "test-team", owner.Id, teamId, tier: TeamTier.Free);

        var dto = new UpdateTeamBrandingDto
        {
            LogoUrl = "https://example.com/logo.png",
            PrimaryColor = "#FF0000",
            SecondaryColor = "#0000FF"
        };

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => service.UpdateBrandingAsync(teamId, dto));
    }

    [Fact]
    public async Task UpdateBrandingAsync_AsNonOwner_ShouldCallAuthorizationService()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        SetupStandardUserContext(teamId, TeamRole.TeamMember);
        
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin))
            .ThrowsAsync(new UnauthorizedAccessException());

        var service = CreateService();

        // Create a team so the authorization check can be reached
        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner");
        await CreateTestTeamAsync("Test Team", "test-team", owner.Id, teamId);

        var dto = new UpdateTeamBrandingDto
        {
            PrimaryColor = "#FF0000"
        };

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => service.UpdateBrandingAsync(teamId, dto));

        _mockAuthorizationService.Verify(x => x.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin), Times.Once);
    }

    #endregion

    #region Helper Methods

    private StandardTeamService CreateService()
    {
        return new StandardTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            MockCurrentUserService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            _mockTeamManager.Object);
    }

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
            IsActive = true,
            Status = UserStatus.Active,
            SecurityStamp = Guid.NewGuid().ToString("D"),
            CreatedOn = DateTime.UtcNow
        };

        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();
        return user;
    }

    private async Task<Team> CreateTestTeamAsync(
        string name, 
        string subdomain, 
        Guid ownerId, 
        Guid? id = null,
        TeamStatus status = TeamStatus.Active,
        TeamTier tier = TeamTier.Free)
    {
        var teamId = id ?? Guid.NewGuid();
        var team = new Team
        {
            Id = teamId,
            Name = name,
            Subdomain = subdomain,
            OwnerId = ownerId,
            Status = status,
            Tier = tier,
            PrimaryColor = "#000000",
            SecondaryColor = "#FFFFFF",
            CreatedOn = DateTime.UtcNow
        };

        DbContext.Teams.Add(team);
        await DbContext.SaveChangesAsync();
        
        // Always create the owner relationship for the team
        await CreateUserTeamRelationshipAsync(ownerId, teamId, TeamRole.TeamOwner, MemberType.Coach);
        
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
            CreatedOn = DateTime.UtcNow
        };

        DbContext.UserTeams.Add(userTeam);
        await DbContext.SaveChangesAsync();
        return userTeam;
    }

    #endregion
} 