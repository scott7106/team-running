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
        var mockTeamService = new MockTeamService();
        var mockCurrentUserService = new MockCurrentUserService();

        return new ApplicationDbContext(optionsBuilder.Options, mockTeamService, mockCurrentUserService);
    }
}

// Mock services for design-time
internal class MockTeamService : ITeamService
{
    public Guid CurrentTeamId => Guid.Empty;
    public string CurrentTeamName => "DesignTime";
    public string CurrentTeamSubdomain => "design-time";
    public void SetCurrentTeam(Guid teamId) { }
    public void SetCurrentTeam(string subdomain) { }
    public void ClearCurrentTeam() { }
}

internal class MockCurrentUserService : ICurrentUserService
{
    public Guid? UserId => Guid.Empty;
    public string UserName => "DesignTime";
    public string Email => "design.time@example.com";
    public string UserEmail => "design.time@example.com";
    public bool IsAuthenticated => true;
} 