using Shouldly;
using TeamStride.Domain.Entities;
using Xunit;

namespace TeamStride.Domain.Tests.Entities;

/// <summary>
/// Unit tests for the Team domain entity.
/// Tests business logic and entity behavior.
/// </summary>
public class TeamTests : BaseTest
{
    [Fact]
    public void CreateTeam_WithValidProperties_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var name = "Test Team";
        var subdomain = "testteam";
        var primaryColor = "#FF0000";
        var secondaryColor = "#00FF00";

        // Act
        var team = new Team
        {
            Name = name,
            Subdomain = subdomain,
            PrimaryColor = primaryColor,
            SecondaryColor = secondaryColor,
            Status = TeamStatus.Active,
            Tier = TeamTier.Free,
            CreatedOn = DateTime.UtcNow
        };

        // Assert
        team.Name.ShouldBe(name);
        team.Subdomain.ShouldBe(subdomain);
        team.PrimaryColor.ShouldBe(primaryColor);
        team.SecondaryColor.ShouldBe(secondaryColor);
        team.Status.ShouldBe(TeamStatus.Active);
        team.Tier.ShouldBe(TeamTier.Free);
        team.CreatedOn.ShouldBeInRange(DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Name_WithInvalidValue_ShouldAllowButNotRecommended(string invalidName)
    {
        // Arrange & Act
        var team = new Team { Name = invalidName };

        // Assert
        team.Name.ShouldBe(invalidName);
        // Note: In a real implementation, you might want validation in the setter or constructor
    }

    [Fact]
    public void Status_CanBeSetToAllValidValues()
    {
        // Arrange
        var team = new Team();

        // Act & Assert
        team.Status = TeamStatus.Active;
        team.Status.ShouldBe(TeamStatus.Active);

        team.Status = TeamStatus.Suspended;
        team.Status.ShouldBe(TeamStatus.Suspended);

        team.Status = TeamStatus.Expired;
        team.Status.ShouldBe(TeamStatus.Expired);

        team.Status = TeamStatus.PendingSetup;
        team.Status.ShouldBe(TeamStatus.PendingSetup);
    }

    [Fact]
    public void Tier_CanBeSetToAllValidValues()
    {
        // Arrange
        var team = new Team();

        // Act & Assert
        team.Tier = TeamTier.Free;
        team.Tier.ShouldBe(TeamTier.Free);

        team.Tier = TeamTier.Standard;
        team.Tier.ShouldBe(TeamTier.Standard);

        team.Tier = TeamTier.Premium;
        team.Tier.ShouldBe(TeamTier.Premium);
    }

    [Fact]
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var team = new Team();

        // Assert
        team.Name.ShouldBe(string.Empty);
        team.Subdomain.ShouldBe(string.Empty);
        team.PrimaryColor.ShouldBe("#000000");
        team.SecondaryColor.ShouldBe("#FFFFFF");
        team.LogoUrl.ShouldBeNull();
        team.Users.ShouldNotBeNull();
        team.Users.ShouldBeEmpty();
    }
} 