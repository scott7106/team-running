using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using TeamStride.Domain.Interfaces;
using TeamStride.Domain.Entities;

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
        var mockCurrentTeamService = new MockCurrentTeamService();
        var mockCurrentUserService = new MockCurrentUserService();

        return new ApplicationDbContext(optionsBuilder.Options, mockCurrentUserService);
    }
}

// Mock services for design-time
internal class MockCurrentTeamService : ICurrentTeamService
{
    public Guid TeamId => Guid.Empty;
    public string GetSubdomain => "design-time";
    public bool IsTeamSet => false;
    public TeamRole? TeamRole => null;
    public MemberType? MemberType => null;
    public void SetTeamId(Guid teamId) { }
    public void SetTeamSubdomain(string subdomain) { }
    public Task<bool> SetTeamFromSubdomainAsync(string subdomain) => Task.FromResult(false);
    public bool SetTeamFromJwtClaims() => false;
    public void ClearTeam() { }
    public bool IsTeamOwner => false;
    public bool IsTeamAdmin => false;
    public bool IsTeamMember => false;
    public bool CanAccessCurrentTeam() => false;
    public bool HasMinimumTeamRole(TeamRole minimumRole) => false;
    public bool CanAccessTeam(Guid teamId) => false;
}

internal class MockCurrentUserService : ICurrentUserService
{
    public Guid? UserId => Guid.Empty;
    public string? UserEmail => "design.time@example.com";
    public string? FirstName => "Design";
    public string? LastName => "Time";
    public bool IsAuthenticated => true;
    
    // Simplified Authorization Model Properties
    public bool IsGlobalAdmin => false;
    
    // Helper Methods for Role Checking
    public bool CanAccessTeam(Guid teamId) => true; // Design-time mock allows access
    public bool HasMinimumTeamRole(TeamRole minimumRole) => true; // Design-time mock allows access
    public Guid? CurrentTeamId => Guid.Empty;
} 