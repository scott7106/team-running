using Shouldly;
using TeamStride.Domain.Entities;
using Xunit;

namespace TeamStride.Domain.Tests.Entities;

/// <summary>
/// Unit tests for the Tenant domain entity.
/// Tests business logic and entity behavior.
/// </summary>
public class TenantTests : BaseTest
{
    [Fact]
    public void CreateTenant_WithValidProperties_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var name = "Test Team";
        var subdomain = "testteam";
        var primaryColor = "#FF0000";
        var secondaryColor = "#00FF00";

        // Act
        var tenant = new Tenant
        {
            Name = name,
            Subdomain = subdomain,
            PrimaryColor = primaryColor,
            SecondaryColor = secondaryColor,
            Status = TenantStatus.Active,
            Tier = TenantTier.Free,
            CreatedOn = DateTime.UtcNow
        };

        // Assert
        tenant.Name.ShouldBe(name);
        tenant.Subdomain.ShouldBe(subdomain);
        tenant.PrimaryColor.ShouldBe(primaryColor);
        tenant.SecondaryColor.ShouldBe(secondaryColor);
        tenant.Status.ShouldBe(TenantStatus.Active);
        tenant.Tier.ShouldBe(TenantTier.Free);
        tenant.CreatedOn.ShouldBeInRange(DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Name_WithInvalidValue_ShouldAllowButNotRecommended(string invalidName)
    {
        // Arrange & Act
        var tenant = new Tenant { Name = invalidName };

        // Assert
        tenant.Name.ShouldBe(invalidName);
        // Note: In a real implementation, you might want validation in the setter or constructor
    }

    [Fact]
    public void Status_CanBeSetToAllValidValues()
    {
        // Arrange
        var tenant = new Tenant();

        // Act & Assert
        tenant.Status = TenantStatus.Active;
        tenant.Status.ShouldBe(TenantStatus.Active);

        tenant.Status = TenantStatus.Suspended;
        tenant.Status.ShouldBe(TenantStatus.Suspended);

        tenant.Status = TenantStatus.Expired;
        tenant.Status.ShouldBe(TenantStatus.Expired);

        tenant.Status = TenantStatus.PendingSetup;
        tenant.Status.ShouldBe(TenantStatus.PendingSetup);
    }

    [Fact]
    public void Tier_CanBeSetToAllValidValues()
    {
        // Arrange
        var tenant = new Tenant();

        // Act & Assert
        tenant.Tier = TenantTier.Free;
        tenant.Tier.ShouldBe(TenantTier.Free);

        tenant.Tier = TenantTier.Standard;
        tenant.Tier.ShouldBe(TenantTier.Standard);

        tenant.Tier = TenantTier.Premium;
        tenant.Tier.ShouldBe(TenantTier.Premium);
    }

    [Fact]
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var tenant = new Tenant();

        // Assert
        tenant.Name.ShouldBe(string.Empty);
        tenant.Subdomain.ShouldBe(string.Empty);
        tenant.PrimaryColor.ShouldBe("#000000");
        tenant.SecondaryColor.ShouldBe("#FFFFFF");
        tenant.LogoUrl.ShouldBeNull();
        tenant.Users.ShouldNotBeNull();
        tenant.Users.ShouldBeEmpty();
    }
} 