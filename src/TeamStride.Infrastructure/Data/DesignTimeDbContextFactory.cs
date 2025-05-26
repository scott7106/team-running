using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using TeamStride.Domain.Interfaces;
using System;
using System.IO;

namespace TeamStride.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../TeamStride.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));

        // Create mock services for design-time
        var mockTenantService = new MockTenantService();
        var mockCurrentUserService = new MockCurrentUserService();

        return new ApplicationDbContext(optionsBuilder.Options, mockTenantService, mockCurrentUserService);
    }
}

// Mock services for design-time
internal class MockTenantService : ITenantService
{
    public Guid CurrentTenantId => Guid.Empty;
    public string CurrentTenantName => "DesignTime";
    public string CurrentTenantSubdomain => "design-time";
    public void SetCurrentTenant(Guid tenantId) { }
    public void SetCurrentTenant(string subdomain) { }
    public void ClearCurrentTenant() { }
}

internal class MockCurrentUserService : ICurrentUserService
{
    public Guid? UserId => Guid.Empty;
    public string UserName => "DesignTime";
    public string Email => "design.time@example.com";
    public string UserEmail => "design.time@example.com";
    public bool IsAuthenticated => true;
} 