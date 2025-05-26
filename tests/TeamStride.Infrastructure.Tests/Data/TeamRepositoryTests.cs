using Shouldly;
using TeamStride.Domain.Entities;
using Xunit;

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
        var team = new Team
        {
            Name = "Test Team",
            Subdomain = "testteam",
            Status = TeamStatus.Active,
            Tier = TeamTier.Free,
            CreatedOn = DateTime.UtcNow
        };
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
} 