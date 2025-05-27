using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using TeamStride.Application.Common.Models;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Application.Teams.Services;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;
using TeamStride.Domain.Interfaces;
using TeamStride.Infrastructure.Email;
using TeamStride.Infrastructure.Mapping;
using TeamStride.Infrastructure.Services;

namespace TeamStride.Infrastructure.Tests.Services;

public class TeamManagementServiceTests : BaseIntegrationTest
{
    private readonly TeamManagementService _teamManagementService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILogger<TeamManagementService>> _mockLogger;
    
    // Test data
    private readonly List<ApplicationUser> _testUsers;
    private readonly List<Team> _testTeams;
    private readonly List<UserTeam> _testUserTeams;

    public TeamManagementServiceTests()
    {
        // Get services from the service provider
        _userManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        // Create mapper
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
        
        // Create mocks
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<TeamManagementService>>();
        
        // Create the service
        _teamManagementService = new TeamManagementService(
            DbContext,
            _mapper,
            _userManager,
            MockCurrentUserService.Object,
            _mockEmailService.Object,
            _mockLogger.Object);
        
        // Setup test data
        var setupTask = SetupTestDataAsync();
        setupTask.Wait();
        _testUsers = setupTask.Result.users;
        _testTeams = setupTask.Result.teams;
        _testUserTeams = setupTask.Result.userTeams;
    }

    private async Task<(List<ApplicationUser> users, List<Team> teams, List<UserTeam> userTeams)> SetupTestDataAsync()
    {
        // Create test users
        var globalAdmin = new ApplicationUser
        {
            Email = "globaladmin@teamstride.com",
            UserName = "globaladmin@teamstride.com",
            FirstName = "Global",
            LastName = "Admin",
            IsActive = true,
            EmailConfirmed = true
        };

        var teamOwner1 = new ApplicationUser
        {
            Email = "owner1@example.com",
            UserName = "owner1@example.com",
            FirstName = "Team",
            LastName = "Owner1",
            IsActive = true,
            EmailConfirmed = true
        };

        var teamOwner2 = new ApplicationUser
        {
            Email = "owner2@example.com",
            UserName = "owner2@example.com",
            FirstName = "Team",
            LastName = "Owner2",
            IsActive = true,
            EmailConfirmed = true
        };

        var teamAdmin = new ApplicationUser
        {
            Email = "admin@example.com",
            UserName = "admin@example.com",
            FirstName = "Team",
            LastName = "Admin",
            IsActive = true,
            EmailConfirmed = true
        };

        var athlete = new ApplicationUser
        {
            Email = "athlete@example.com",
            UserName = "athlete@example.com",
            FirstName = "Team",
            LastName = "Athlete",
            IsActive = true,
            EmailConfirmed = true
        };

        // Create users using UserManager
        await _userManager.CreateAsync(globalAdmin);
        await _userManager.CreateAsync(teamOwner1);
        await _userManager.CreateAsync(teamOwner2);
        await _userManager.CreateAsync(teamAdmin);
        await _userManager.CreateAsync(athlete);

        // Set global admin status
        globalAdmin.SetGlobalAdmin(true);
        await _userManager.UpdateAsync(globalAdmin);

        var users = new List<ApplicationUser> { globalAdmin, teamOwner1, teamOwner2, teamAdmin, athlete };

        // Create test teams
        var team1 = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Alpha Team",
            Subdomain = "alpha",
            Status = TeamStatus.Active,
            Tier = TeamTier.Standard,
            PrimaryColor = "#FF0000",
            SecondaryColor = "#FFFFFF",
            ExpiresOn = DateTime.UtcNow.AddMonths(12),
            CreatedOn = DateTime.UtcNow.AddDays(-30)
        };

        var team2 = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Beta Team",
            Subdomain = "beta",
            Status = TeamStatus.Active,
            Tier = TeamTier.Premium,
            PrimaryColor = "#00FF00",
            SecondaryColor = "#000000",
            ExpiresOn = DateTime.UtcNow.AddMonths(6),
            CreatedOn = DateTime.UtcNow.AddDays(-60)
        };

        var team3 = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Gamma Team",
            Subdomain = "gamma",
            Status = TeamStatus.Suspended,
            Tier = TeamTier.Free,
            PrimaryColor = "#0000FF",
            SecondaryColor = "#FFFFFF",
            CreatedOn = DateTime.UtcNow.AddDays(-90)
        };

        DbContext.Teams.AddRange(team1, team2, team3);
        await DbContext.SaveChangesAsync();

        var teams = new List<Team> { team1, team2, team3 };

        // Create user-team relationships
        var userTeams = new List<UserTeam>
        {
            // Team 1 relationships
            new UserTeam
            {
                Id = Guid.NewGuid(),
                UserId = teamOwner1.Id,
                TeamId = team1.Id,
                Role = TeamRole.Host,
                IsDefault = true,
                IsActive = true,
                JoinedOn = DateTime.UtcNow.AddDays(-30)
            },
            new UserTeam
            {
                Id = Guid.NewGuid(),
                UserId = teamAdmin.Id,
                TeamId = team1.Id,
                Role = TeamRole.Admin,
                IsDefault = false,
                IsActive = true,
                JoinedOn = DateTime.UtcNow.AddDays(-25)
            },
            new UserTeam
            {
                Id = Guid.NewGuid(),
                UserId = athlete.Id,
                TeamId = team1.Id,
                Role = TeamRole.Athlete,
                IsDefault = false,
                IsActive = true,
                JoinedOn = DateTime.UtcNow.AddDays(-20)
            },
            // Team 2 relationships
            new UserTeam
            {
                Id = Guid.NewGuid(),
                UserId = teamOwner2.Id,
                TeamId = team2.Id,
                Role = TeamRole.Host,
                IsDefault = true,
                IsActive = true,
                JoinedOn = DateTime.UtcNow.AddDays(-60)
            },
            // Team 3 relationships
            new UserTeam
            {
                Id = Guid.NewGuid(),
                UserId = teamOwner1.Id,
                TeamId = team3.Id,
                Role = TeamRole.Host,
                IsDefault = false,
                IsActive = true,
                JoinedOn = DateTime.UtcNow.AddDays(-90)
            }
        };

        DbContext.UserTeams.AddRange(userTeams);
        await DbContext.SaveChangesAsync();

        return (users, teams, userTeams);
    }

    #region GetTeamsAsync Tests

    [Fact]
    public async Task GetTeamsAsync_AsGlobalAdmin_ReturnsAllTeams()
    {
        // Arrange
        var globalAdmin = _testUsers.First(u => u.IsGlobalAdmin);
        MockCurrentUserService.Setup(x => x.UserId).Returns(globalAdmin.Id);
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(true);

        // Act
        var result = await _teamManagementService.GetTeamsAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(3);
        result.TotalCount.ShouldBe(3);
        
        // Verify teams are ordered by name
        var teamsList = result.Items.ToList();
        teamsList[0].Name.ShouldBe("Alpha Team");
        teamsList[1].Name.ShouldBe("Beta Team");
        teamsList[2].Name.ShouldBe("Gamma Team");
        
        // Verify team details
        var alphaTeam = teamsList[0];
        alphaTeam.Subdomain.ShouldBe("alpha");
        alphaTeam.Status.ShouldBe(TeamStatus.Active);
        alphaTeam.Tier.ShouldBe(TeamTier.Standard);
        alphaTeam.Owner.ShouldNotBeNull();
        alphaTeam.Owner.Email.ShouldBe("owner1@example.com");
        alphaTeam.MemberCount.ShouldBe(3);
        alphaTeam.AthleteCount.ShouldBe(1);
        alphaTeam.AdminCount.ShouldBe(2); // Host + Admin
    }

    [Fact]
    public async Task GetTeamsAsync_AsNonGlobalAdmin_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var teamAdmin = _testUsers.First(u => u.Email == "admin@example.com");
        MockCurrentUserService.Setup(x => x.UserId).Returns(teamAdmin.Id);
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(false);

        // Act & Assert
        var exception = await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _teamManagementService.GetTeamsAsync());
        
        exception.Message.ShouldBe("Global admin access required");
    }

    [Fact]
    public async Task GetTeamsAsync_WithSearchQuery_ReturnsFilteredTeams()
    {
        // Arrange
        var globalAdmin = _testUsers.First(u => u.IsGlobalAdmin);
        MockCurrentUserService.Setup(x => x.UserId).Returns(globalAdmin.Id);
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(true);

        // Act
        var result = await _teamManagementService.GetTeamsAsync(searchQuery: "Alpha");

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(1);
        result.Items.First().Name.ShouldBe("Alpha Team");
    }

    [Fact]
    public async Task GetTeamsAsync_WithSubdomainSearch_ReturnsFilteredTeams()
    {
        // Arrange
        var globalAdmin = _testUsers.First(u => u.IsGlobalAdmin);
        MockCurrentUserService.Setup(x => x.UserId).Returns(globalAdmin.Id);
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(true);

        // Act
        var result = await _teamManagementService.GetTeamsAsync(searchQuery: "beta");

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(1);
        result.Items.First().Subdomain.ShouldBe("beta");
    }

    [Fact]
    public async Task GetTeamsAsync_WithStatusFilter_ReturnsFilteredTeams()
    {
        // Arrange
        var globalAdmin = _testUsers.First(u => u.IsGlobalAdmin);
        MockCurrentUserService.Setup(x => x.UserId).Returns(globalAdmin.Id);
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(true);

        // Act
        var result = await _teamManagementService.GetTeamsAsync(status: TeamStatus.Active);

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(2);
        result.Items.All(t => t.Status == TeamStatus.Active).ShouldBeTrue();
    }

    [Fact]
    public async Task GetTeamsAsync_WithTierFilter_ReturnsFilteredTeams()
    {
        // Arrange
        var globalAdmin = _testUsers.First(u => u.IsGlobalAdmin);
        MockCurrentUserService.Setup(x => x.UserId).Returns(globalAdmin.Id);
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(true);

        // Act
        var result = await _teamManagementService.GetTeamsAsync(tier: TeamTier.Premium);

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(1);
        result.Items.First().Tier.ShouldBe(TeamTier.Premium);
        result.Items.First().Name.ShouldBe("Beta Team");
    }

    [Fact]
    public async Task GetTeamsAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var globalAdmin = _testUsers.First(u => u.IsGlobalAdmin);
        MockCurrentUserService.Setup(x => x.UserId).Returns(globalAdmin.Id);
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(true);

        // Act
        var result = await _teamManagementService.GetTeamsAsync(pageNumber: 2, pageSize: 1);

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(1);
        result.PageNumber.ShouldBe(2);
        result.TotalCount.ShouldBe(3);
        result.HasNextPage.ShouldBeTrue();
        result.HasPreviousPage.ShouldBeTrue();
        result.Items.First().Name.ShouldBe("Beta Team"); // Second team alphabetically
    }

    [Fact]
    public async Task GetTeamsAsync_WithMultipleFilters_ReturnsCorrectResults()
    {
        // Arrange
        var globalAdmin = _testUsers.First(u => u.IsGlobalAdmin);
        MockCurrentUserService.Setup(x => x.UserId).Returns(globalAdmin.Id);
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(true);

        // Act
        var result = await _teamManagementService.GetTeamsAsync(
            searchQuery: "Team",
            status: TeamStatus.Active,
            tier: TeamTier.Standard);

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(1);
        result.Items.First().Name.ShouldBe("Alpha Team");
        result.Items.First().Status.ShouldBe(TeamStatus.Active);
        result.Items.First().Tier.ShouldBe(TeamTier.Standard);
    }

    [Fact]
    public async Task GetTeamsAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(true);
        
        // Clear all teams
        DbContext.Teams.RemoveRange(DbContext.Teams);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _teamManagementService.GetTeamsAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
        result.PageNumber.ShouldBe(1);
        result.TotalPages.ShouldBe(0);
        result.HasPreviousPage.ShouldBeFalse();
        result.HasNextPage.ShouldBeFalse();
    }

    #endregion

    #region GetTeamByIdAsync Tests

    [Fact]
    public async Task GetTeamByIdAsync_AsGlobalAdmin_ReturnsTeamDetails()
    {
        // Arrange
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(true);
        var team = _testTeams[0]; // Alpha Team

        // Act
        var result = await _teamManagementService.GetTeamByIdAsync(team.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(team.Id);
        result.Name.ShouldBe("Alpha Team");
        result.Subdomain.ShouldBe("alpha");
        result.Status.ShouldBe(TeamStatus.Active);
        result.Tier.ShouldBe(TeamTier.Standard);
        result.PrimaryColor.ShouldBe("#FF0000");
        result.SecondaryColor.ShouldBe("#FFFFFF");
        result.ExpiresOn.ShouldNotBeNull();
        result.CreatedOn.ShouldBeGreaterThan(DateTime.MinValue);
        
        // Verify owner details
        result.Owner.ShouldNotBeNull();
        result.Owner.Email.ShouldBe("owner1@example.com");
        result.Owner.FirstName.ShouldBe("Team");
        result.Owner.LastName.ShouldBe("Owner1");
        result.Owner.Role.ShouldBe(TeamRole.Host);
        result.Owner.IsOwner.ShouldBeTrue();
        
        // Verify member counts
        result.MemberCount.ShouldBe(3); // Owner + Admin + Athlete
        result.AthleteCount.ShouldBe(1);
        result.AdminCount.ShouldBe(2); // Host + Admin
        result.HasPendingOwnershipTransfer.ShouldBeFalse();
    }

    [Fact]
    public async Task GetTeamByIdAsync_AsTeamOwner_ReturnsOwnTeamDetails()
    {
        // Arrange
        var teamOwner = _testUsers[1]; // teamOwner1
        var team = _testTeams[0]; // Alpha Team
        MockCurrentUserService.Setup(x => x.UserId).Returns(teamOwner.Id);
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(false);

        // Act
        var result = await _teamManagementService.GetTeamByIdAsync(team.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(team.Id);
        result.Name.ShouldBe("Alpha Team");
        result.Owner.Email.ShouldBe("owner1@example.com");
    }

    [Fact]
    public async Task GetTeamByIdAsync_AsTeamAdmin_ReturnsOwnTeamDetails()
    {
        // Arrange
        var teamAdmin = _testUsers[3]; // teamAdmin
        var team = _testTeams[0]; // Alpha Team
        MockCurrentUserService.Setup(x => x.UserId).Returns(teamAdmin.Id);
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(false);

        // Act
        var result = await _teamManagementService.GetTeamByIdAsync(team.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(team.Id);
        result.Name.ShouldBe("Alpha Team");
    }

    [Fact]
    public async Task GetTeamByIdAsync_AsTeamAthlete_ReturnsOwnTeamDetails()
    {
        // Arrange
        var athlete = _testUsers[4]; // athlete
        var team = _testTeams[0]; // Alpha Team
        MockCurrentUserService.Setup(x => x.UserId).Returns(athlete.Id);
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(false);

        // Act
        var result = await _teamManagementService.GetTeamByIdAsync(team.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(team.Id);
        result.Name.ShouldBe("Alpha Team");
    }

    [Fact]
    public async Task GetTeamByIdAsync_AsNonMember_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var nonMember = _testUsers[2]; // teamOwner2 (not member of Alpha Team)
        var team = _testTeams[0]; // Alpha Team
        MockCurrentUserService.Setup(x => x.UserId).Returns(nonMember.Id);
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(false);

        // Act & Assert
        var exception = await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _teamManagementService.GetTeamByIdAsync(team.Id));
        
        exception.Message.ShouldBe("Access to this team is not authorized");
    }

    [Fact]
    public async Task GetTeamByIdAsync_WithInvalidTeamId_ThrowsInvalidOperationException()
    {
        // Arrange
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(true);
        var invalidTeamId = Guid.NewGuid();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _teamManagementService.GetTeamByIdAsync(invalidTeamId));
        
        exception.Message.ShouldBe($"Team with ID {invalidTeamId} not found");
    }

    [Fact]
    public async Task GetTeamByIdAsync_WithSuspendedTeam_ReturnsTeamDetails()
    {
        // Arrange
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(true);
        var suspendedTeam = _testTeams[2]; // Gamma Team (Suspended)

        // Act
        var result = await _teamManagementService.GetTeamByIdAsync(suspendedTeam.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(suspendedTeam.Id);
        result.Name.ShouldBe("Gamma Team");
        result.Status.ShouldBe(TeamStatus.Suspended);
        result.Tier.ShouldBe(TeamTier.Free);
    }

    [Fact]
    public async Task GetTeamByIdAsync_WithPremiumTeam_ReturnsCorrectTierDetails()
    {
        // Arrange
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(true);
        var premiumTeam = _testTeams[1]; // Beta Team (Premium)

        // Act
        var result = await _teamManagementService.GetTeamByIdAsync(premiumTeam.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(premiumTeam.Id);
        result.Name.ShouldBe("Beta Team");
        result.Tier.ShouldBe(TeamTier.Premium);
        result.PrimaryColor.ShouldBe("#00FF00");
        result.SecondaryColor.ShouldBe("#000000");
    }

    [Fact]
    public async Task GetTeamByIdAsync_AsInactiveTeamMember_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var teamOwner = _testUsers[1]; // teamOwner1
        var team = _testTeams[0]; // Alpha Team
        
        // Make the user-team relationship inactive
        var userTeam = await DbContext.UserTeams
            .FirstAsync(ut => ut.UserId == teamOwner.Id && ut.TeamId == team.Id);
        userTeam.IsActive = false;
        await DbContext.SaveChangesAsync();
        
        MockCurrentUserService.Setup(x => x.UserId).Returns(teamOwner.Id);
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(false);

        // Act & Assert
        var exception = await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _teamManagementService.GetTeamByIdAsync(team.Id));
        
        exception.Message.ShouldBe("Access to this team is not authorized");
    }

    #endregion

    #region GetTeamBySubdomainAsync Tests

    [Fact]
    public async Task GetTeamBySubdomainAsync_AsGlobalAdmin_ReturnsTeamDetails()
    {
        // Arrange
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(true);
        var subdomain = "alpha";

        // Act
        var result = await _teamManagementService.GetTeamBySubdomainAsync(subdomain);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Alpha Team");
        result.Subdomain.ShouldBe("alpha");
        result.Status.ShouldBe(TeamStatus.Active);
        result.Tier.ShouldBe(TeamTier.Standard);
        result.Owner.ShouldNotBeNull();
        result.Owner.Email.ShouldBe("owner1@example.com");
    }

    [Fact]
    public async Task GetTeamBySubdomainAsync_AsTeamMember_ReturnsTeamDetails()
    {
        // Arrange
        var teamOwner = _testUsers[1]; // teamOwner1
        MockCurrentUserService.Setup(x => x.UserId).Returns(teamOwner.Id);
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(false);
        var subdomain = "alpha";

        // Act
        var result = await _teamManagementService.GetTeamBySubdomainAsync(subdomain);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Alpha Team");
        result.Subdomain.ShouldBe("alpha");
        result.Owner.Email.ShouldBe("owner1@example.com");
    }

    [Fact]
    public async Task GetTeamBySubdomainAsync_AsNonMember_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var nonMember = _testUsers[2]; // teamOwner2 (not member of Alpha Team)
        MockCurrentUserService.Setup(x => x.UserId).Returns(nonMember.Id);
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(false);
        var subdomain = "alpha";

        // Act & Assert
        var exception = await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _teamManagementService.GetTeamBySubdomainAsync(subdomain));
        
        exception.Message.ShouldBe("Access to this team is not authorized");
    }

    [Fact]
    public async Task GetTeamBySubdomainAsync_WithInvalidSubdomain_ThrowsInvalidOperationException()
    {
        // Arrange
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(true);
        var invalidSubdomain = "nonexistent";

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _teamManagementService.GetTeamBySubdomainAsync(invalidSubdomain));
        
        exception.Message.ShouldBe("Team with subdomain 'nonexistent' not found");
    }

    [Fact]
    public async Task GetTeamBySubdomainAsync_WithSuspendedTeam_ReturnsTeamDetails()
    {
        // Arrange
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(true);
        var subdomain = "gamma"; // Gamma Team is suspended

        // Act
        var result = await _teamManagementService.GetTeamBySubdomainAsync(subdomain);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Gamma Team");
        result.Subdomain.ShouldBe("gamma");
        result.Status.ShouldBe(TeamStatus.Suspended);
        result.Tier.ShouldBe(TeamTier.Free);
    }

    [Fact]
    public async Task GetTeamBySubdomainAsync_WithExactCase_ReturnsTeamDetails()
    {
        // Arrange
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(true);
        var subdomain = "alpha"; // Exact case

        // Act
        var result = await _teamManagementService.GetTeamBySubdomainAsync(subdomain);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Alpha Team");
        result.Subdomain.ShouldBe("alpha");
    }

    #endregion
} 