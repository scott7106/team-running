using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;
using TeamStride.Infrastructure.Services;

namespace TeamStride.Infrastructure.Tests.Services;

public class TenantSwitcherServiceTests : BaseSecuredTest
{
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<TenantSwitcherService>> _mockLogger;

    public TenantSwitcherServiceTests()
    {
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<TenantSwitcherService>>();
    }

    private TenantSwitcherService CreateService()
    {
        return new TenantSwitcherService(
            DbContext,
            MockCurrentUserService.Object,
            _mockMapper.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetUserTenantsAsync_WhenUserNotAuthenticated_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        SetupAnonymousContext();
        var service = CreateService();

        // Act & Assert
        var exception = await Should.ThrowAsync<UnauthorizedAccessException>(
            () => service.GetUserTenantsAsync());
        
        exception.Message.ShouldBe("User is not authenticated");
    }

    [Fact]
    public async Task GetUserTenantsAsync_WhenUserHasNoTeams_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupStandardUserContext(Guid.NewGuid(), TeamRole.TeamMember, userId);
        
        var service = CreateService();

        // Act
        var result = await service.GetUserTenantsAsync();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetUserTenantsAsync_WhenUserHasActiveTeams_ReturnsCorrectTenants()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var team1Id = Guid.NewGuid();
        var team2Id = Guid.NewGuid();

        SetupStandardUserContext(team1Id, TeamRole.TeamMember, userId);
        var service = CreateService();

        // Create test data
        var user = new ApplicationUser 
        { 
            Id = userId, 
            Email = "test@example.com", 
            UserName = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Status = UserStatus.Active,
            IsActive = true
        };
        var team1 = new Team
        {
            Id = team1Id,
            Name = "Team Alpha",
            Subdomain = "alpha",
            PrimaryColor = "#FF0000",
            SecondaryColor = "#FFFFFF",
            Status = TeamStatus.Active,
            IsDeleted = false,
            OwnerId = userId
        };
        var team2 = new Team
        {
            Id = team2Id,
            Name = "Team Beta",
            Subdomain = "beta",
            PrimaryColor = "#0000FF",
            SecondaryColor = "#FFFFFF",
            Status = TeamStatus.Active,
            IsDeleted = false,
            OwnerId = userId
        };

        var userTeam1 = new UserTeam
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TeamId = team1Id,
            User = user,
            Team = team1,
            IsActive = true,
            Role = TeamRole.TeamMember
        };
        var userTeam2 = new UserTeam
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TeamId = team2Id,
            User = user,
            Team = team2,
            IsActive = true,
            Role = TeamRole.TeamAdmin
        };

        await DbContext.Users.AddAsync(user);
        await DbContext.Teams.AddRangeAsync(team1, team2);
        await DbContext.UserTeams.AddRangeAsync(userTeam1, userTeam2);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await service.GetUserTenantsAsync();

        // Assert
        result.ShouldNotBeNull();
        var tenants = result.ToList();
        tenants.Count.ShouldBe(2);

        // Should be ordered by name (Alpha before Beta)
        tenants[0].TeamId.ShouldBe(team1Id);
        tenants[0].TeamName.ShouldBe("Team Alpha");
        tenants[0].Subdomain.ShouldBe("alpha");
        tenants[0].PrimaryColor.ShouldBe("#FF0000");
        tenants[0].SecondaryColor.ShouldBe("#FFFFFF");

        tenants[1].TeamId.ShouldBe(team2Id);
        tenants[1].TeamName.ShouldBe("Team Beta");
        tenants[1].Subdomain.ShouldBe("beta");
        tenants[1].PrimaryColor.ShouldBe("#0000FF");
        tenants[1].SecondaryColor.ShouldBe("#FFFFFF");
    }

    [Fact]
    public async Task GetUserTenantsAsync_ExcludesInactiveUserTeams()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        SetupStandardUserContext(teamId, TeamRole.TeamMember, userId);
        var service = CreateService();

        var user = new ApplicationUser 
        { 
            Id = userId, 
            Email = "test@example.com", 
            UserName = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Status = UserStatus.Active,
            IsActive = true
        };
        var team = new Team
        {
            Id = teamId,
            Name = "Team Test",
            Subdomain = "test",
            Status = TeamStatus.Active,
            IsDeleted = false,
            OwnerId = userId
        };

        var inactiveUserTeam = new UserTeam
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TeamId = teamId,
            User = user,
            Team = team,
            IsActive = false, // Inactive user team
            Role = TeamRole.TeamMember
        };

        await DbContext.Users.AddAsync(user);
        await DbContext.Teams.AddAsync(team);
        await DbContext.UserTeams.AddAsync(inactiveUserTeam);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await service.GetUserTenantsAsync();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetUserTenantsAsync_ExcludesDeletedTeams()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        SetupStandardUserContext(teamId, TeamRole.TeamMember, userId);
        var service = CreateService();

        var user = new ApplicationUser 
        { 
            Id = userId, 
            Email = "test@example.com", 
            UserName = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Status = UserStatus.Active,
            IsActive = true
        };
        var deletedTeam = new Team
        {
            Id = teamId,
            Name = "Deleted Team",
            Subdomain = "deleted",
            Status = TeamStatus.Active,
            IsDeleted = true, // Deleted team
            OwnerId = userId
        };

        var userTeam = new UserTeam
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TeamId = teamId,
            User = user,
            Team = deletedTeam,
            IsActive = true,
            Role = TeamRole.TeamMember
        };

        await DbContext.Users.AddAsync(user);
        await DbContext.Teams.AddAsync(deletedTeam);
        await DbContext.UserTeams.AddAsync(userTeam);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await service.GetUserTenantsAsync();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetUserTenantsAsync_ExcludesInactiveTeams()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var teamId = Guid.NewGuid();

        SetupStandardUserContext(teamId, TeamRole.TeamMember, userId);
        var service = CreateService();

        var user = new ApplicationUser 
        { 
            Id = userId, 
            Email = "test@example.com", 
            UserName = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Status = UserStatus.Active,
            IsActive = true
        };
        var inactiveTeam = new Team
        {
            Id = teamId,
            Name = "Inactive Team",
            Subdomain = "inactive",
            Status = TeamStatus.Suspended, // Inactive team
            IsDeleted = false,
            OwnerId = userId
        };

        var userTeam = new UserTeam
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TeamId = teamId,
            User = user,
            Team = inactiveTeam,
            IsActive = true,
            Role = TeamRole.TeamMember
        };

        await DbContext.Users.AddAsync(user);
        await DbContext.Teams.AddAsync(inactiveTeam);
        await DbContext.UserTeams.AddAsync(userTeam);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await service.GetUserTenantsAsync();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}