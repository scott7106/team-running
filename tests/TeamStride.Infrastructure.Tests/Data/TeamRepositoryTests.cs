using Shouldly;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace TeamStride.Infrastructure.Tests.Data;

/// <summary>
/// Integration tests for team data access operations.
/// Tests repository patterns and database interactions.
/// </summary>
public class TeamRepositoryTests : BaseIntegrationTest
{
    [Fact]
    public async Task AddTeam_ShouldPersistToDatabase()
    {
        // Arrange
        var team = new Team
        {
            Name = "Test Team",
            Subdomain = "testteam",
            Status = TeamStatus.Active,
            Tier = TeamTier.Free,
            CreatedOn = DateTime.UtcNow
        };

        // Act
        DbContext.Teams.Add(team);
        await DbContext.SaveChangesAsync();

        // Assert
        var savedTeam = await DbContext.Teams.FindAsync(team.Id);
        savedTeam.ShouldNotBeNull();
        savedTeam.Name.ShouldBe("Test Team");
        savedTeam.Subdomain.ShouldBe("testteam");
        savedTeam.Status.ShouldBe(TeamStatus.Active);
        savedTeam.Tier.ShouldBe(TeamTier.Free);
    }

    [Fact]
    public async Task GetTeamBySubdomain_WhenExists_ShouldReturnTeam()
    {
        // Arrange
        var userId = Guid.NewGuid();
        MockCurrentUserService.Setup(x => x.UserId).Returns(userId);

        // Create the user first
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testuser@example.com",
            Email = "testuser@example.com",
            EmailConfirmed = true,
            FirstName = "Test",
            LastName = "User"
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var team = new Team
        {
            Name = "Test Team",
            Subdomain = "testteam",
            Status = TeamStatus.Active,
            Tier = TeamTier.Free,
            CreatedOn = DateTime.UtcNow,
            Users = new List<UserTeam>()
        };
        
        var userTeam = new UserTeam
        {
            UserId = userId,
            User = user,
            Role = TeamRole.TeamMember,
            MemberType = MemberType.Coach,
            IsActive = true,
            JoinedOn = DateTime.UtcNow
        };
        
        team.Users.Add(userTeam);
        DbContext.Teams.Add(team);
        await DbContext.SaveChangesAsync();

        // Act
        var foundTeam = DbContext.Teams
            .FirstOrDefault(t => t.Subdomain == "testteam");

        // Assert
        foundTeam.ShouldNotBeNull();
        foundTeam.Id.ShouldBe(team.Id);
        foundTeam.Name.ShouldBe("Test Team");
    }

    [Fact]
    public void GetTeamBySubdomain_WhenNotExists_ShouldReturnNull()
    {
        // Arrange
        // No team added

        // Act
        var foundTeam = DbContext.Teams
            .FirstOrDefault(t => t.Subdomain == "nonexistent");

        // Assert
        foundTeam.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateTeam_ShouldPersistChanges()
    {
        // Arrange
        var team = new Team
        {
            Name = "Original Name",
            Subdomain = "testteam",
            Status = TeamStatus.Active,
            Tier = TeamTier.Free,
            CreatedOn = DateTime.UtcNow
        };
        DbContext.Teams.Add(team);
        await DbContext.SaveChangesAsync();

        // Act
        team.Name = "Updated Name";
        team.PrimaryColor = "#FF0000";
        team.ModifiedOn = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();

        // Assert
        var updatedTeam = await DbContext.Teams.FindAsync(team.Id);
        updatedTeam.ShouldNotBeNull();
        updatedTeam.Name.ShouldBe("Updated Name");
        updatedTeam.PrimaryColor.ShouldBe("#FF0000");
        updatedTeam.ModifiedOn.ShouldNotBeNull();
    }



    [Fact]
    public async Task GetTeamBySubdomain_WhenUserIsTeamMember_ShouldReturnTeam()
    {
        // Arrange
        var userId = Guid.NewGuid();
        MockCurrentUserService.Setup(x => x.UserId).Returns(userId);

        // Create the user first
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testuser@example.com",
            Email = "testuser@example.com",
            EmailConfirmed = true,
            FirstName = "Test",
            LastName = "User"
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var team = new Team
        {
            Name = "Test Team",
            Subdomain = "testteam",
            Status = TeamStatus.Active,
            Tier = TeamTier.Free,
            CreatedOn = DateTime.UtcNow,
            Users = new List<UserTeam>()
        };
        
        var userTeam = new UserTeam
        {
            UserId = userId,
            User = user,
            Role = TeamRole.TeamMember,
            IsActive = true,
            JoinedOn = DateTime.UtcNow
        };
        
        team.Users.Add(userTeam);
        DbContext.Teams.Add(team);
        await DbContext.SaveChangesAsync();

        // Act
        var foundTeam = DbContext.Teams
            .Include(t => t.Users)
            .FirstOrDefault(t => t.Subdomain == "testteam");

        // Assert
        foundTeam.ShouldNotBeNull();
        foundTeam.Id.ShouldBe(team.Id);
        foundTeam.Name.ShouldBe("Test Team");
    }
} 