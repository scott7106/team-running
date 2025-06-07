using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using TeamStride.Application.Common.Services;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;
using TeamStride.Infrastructure.Services;

namespace TeamStride.Infrastructure.Tests.Services;

public class GlobalAdminDashboardServiceTests : BaseSecuredTest
{
    private readonly Mock<IAuthorizationService> _mockAuthorizationService;
    private readonly Mock<ILogger<GlobalAdminDashboardService>> _mockLogger;
    private readonly UserManager<ApplicationUser> _userManager;

    public GlobalAdminDashboardServiceTests()
    {
        _mockAuthorizationService = new Mock<IAuthorizationService>();
        _mockLogger = new Mock<ILogger<GlobalAdminDashboardService>>();
        _userManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    }

    private GlobalAdminDashboardService CreateService()
    {
        return new GlobalAdminDashboardService(
            DbContext,
            _userManager,
            _mockAuthorizationService.Object);
    }

    [Fact]
    public async Task GetDashboardStatsAsync_ShouldReturnCorrectCounts()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Create test data
        var globalAdminRole = await CreateTestRoleAsync("GlobalAdmin");

        // Create users
        var user1 = await CreateTestUserAsync("user1@test.com", "User", "One");
        var user2 = await CreateTestUserAsync("user2@test.com", "User", "Two");
        var globalAdmin = await CreateTestUserAsync("admin@test.com", "Admin", "User");

        // Make one user a global admin
        DbContext.UserRoles.Add(new IdentityUserRole<Guid>
        {
            UserId = globalAdmin.Id,
            RoleId = globalAdminRole.Id
        });

        // Create teams
        var activeTeam1 = await CreateTestTeamAsync("Team 1", "team1", user1.Id);
        var activeTeam2 = await CreateTestTeamAsync("Team 2", "team2", user2.Id);
        var inactiveTeam = await CreateTestTeamAsync("Inactive Team", "inactive", user1.Id, status: TeamStatus.Suspended);
        var deletedTeam = await CreateTestTeamAsync("Deleted Team", "deleted", user2.Id);
        
        // Mark one team as deleted
        deletedTeam.IsDeleted = true;
        DbContext.Teams.Update(deletedTeam);
        
        await DbContext.SaveChangesAsync();

        // Act
        var result = await service.GetDashboardStatsAsync();

        // Assert
        result.ShouldNotBeNull();
        result.ActiveTeamsCount.ShouldBe(2); // Only active, non-deleted teams
        result.TotalUsersCount.ShouldBe(3); // All non-deleted users
        result.GlobalAdminsCount.ShouldBe(1); // Only the global admin
    }

    [Fact]
    public async Task GetDashboardStatsAsync_WhenNotGlobalAdmin_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        SetupStandardUserContext(teamId);
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .ThrowsAsync(new UnauthorizedAccessException("Global admin privileges required"));

        var service = CreateService();

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => service.GetDashboardStatsAsync());
    }

    [Fact]
    public async Task GetDashboardStatsAsync_WithNoData_ShouldReturnZeroCounts()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.GetDashboardStatsAsync();

        // Assert
        result.ShouldNotBeNull();
        result.ActiveTeamsCount.ShouldBe(0);
        result.TotalUsersCount.ShouldBe(0);
        result.GlobalAdminsCount.ShouldBe(0);
    }

    private async Task<ApplicationUser> CreateTestUserAsync(
        string email, 
        string firstName, 
        string lastName, 
        Guid? id = null)
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
            SecurityStamp = Guid.NewGuid().ToString()
        };

        var result = await _userManager.CreateAsync(user, "TempPassword123!");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

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
        var team = new Team
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Subdomain = subdomain,
            Status = status,
            Tier = tier,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = ownerId
        };

        DbContext.Teams.Add(team);
        await DbContext.SaveChangesAsync();

        return team;
    }

    private async Task<ApplicationRole> CreateTestRoleAsync(string roleName)
    {
        var role = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = roleName,
            NormalizedName = roleName.ToUpper()
        };

        DbContext.Roles.Add(role);
        await DbContext.SaveChangesAsync();

        return role;
    }
} 