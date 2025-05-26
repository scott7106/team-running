using Shouldly;
using TeamStride.Domain.Entities;
using Xunit;

namespace TeamStride.Infrastructure.Tests.Data;

/// <summary>
/// Integration tests for tenant data access operations.
/// Tests repository patterns and database interactions.
/// </summary>
public class TenantRepositoryTests : BaseIntegrationTest
{
    [Fact]
    public async Task AddTenant_ShouldPersistToDatabase()
    {
        // Arrange
        var tenant = new Tenant
        {
            Name = "Test Team",
            Subdomain = "testteam",
            Status = TenantStatus.Active,
            Tier = TenantTier.Free,
            CreatedOn = DateTime.UtcNow
        };

        // Act
        DbContext.Tenants.Add(tenant);
        await DbContext.SaveChangesAsync();

        // Assert
        var savedTenant = await DbContext.Tenants.FindAsync(tenant.Id);
        savedTenant.ShouldNotBeNull();
        savedTenant.Name.ShouldBe("Test Team");
        savedTenant.Subdomain.ShouldBe("testteam");
        savedTenant.Status.ShouldBe(TenantStatus.Active);
        savedTenant.Tier.ShouldBe(TenantTier.Free);
    }

    [Fact]
    public async Task GetTenantBySubdomain_WhenExists_ShouldReturnTenant()
    {
        // Arrange
        var tenant = new Tenant
        {
            Name = "Test Team",
            Subdomain = "testteam",
            Status = TenantStatus.Active,
            Tier = TenantTier.Free,
            CreatedOn = DateTime.UtcNow
        };
        DbContext.Tenants.Add(tenant);
        await DbContext.SaveChangesAsync();

        // Act
        var foundTenant = DbContext.Tenants
            .FirstOrDefault(t => t.Subdomain == "testteam");

        // Assert
        foundTenant.ShouldNotBeNull();
        foundTenant.Id.ShouldBe(tenant.Id);
        foundTenant.Name.ShouldBe("Test Team");
    }

    [Fact]
    public void GetTenantBySubdomain_WhenNotExists_ShouldReturnNull()
    {
        // Arrange
        // No tenant added

        // Act
        var foundTenant = DbContext.Tenants
            .FirstOrDefault(t => t.Subdomain == "nonexistent");

        // Assert
        foundTenant.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateTenant_ShouldPersistChanges()
    {
        // Arrange
        var tenant = new Tenant
        {
            Name = "Original Name",
            Subdomain = "testteam",
            Status = TenantStatus.Active,
            Tier = TenantTier.Free,
            CreatedOn = DateTime.UtcNow
        };
        DbContext.Tenants.Add(tenant);
        await DbContext.SaveChangesAsync();

        // Act
        tenant.Name = "Updated Name";
        tenant.PrimaryColor = "#FF0000";
        tenant.ModifiedOn = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();

        // Assert
        var updatedTenant = await DbContext.Tenants.FindAsync(tenant.Id);
        updatedTenant.ShouldNotBeNull();
        updatedTenant.Name.ShouldBe("Updated Name");
        updatedTenant.PrimaryColor.ShouldBe("#FF0000");
        updatedTenant.ModifiedOn.ShouldNotBeNull();
    }
} 