using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TeamStride.Domain.Identity;
using TeamStride.Domain.Interfaces;
using TeamStride.Infrastructure.Data;
using Xunit;

namespace TeamStride.Infrastructure.Tests;

/// <summary>
/// Base class for infrastructure integration tests.
/// Provides SQLite in-memory database setup for testing repositories and data access.
/// </summary>
public abstract class BaseIntegrationTest : IDisposable
{
    protected ApplicationDbContext DbContext { get; private set; }
    protected IServiceProvider ServiceProvider { get; private set; }
    protected Mock<ICurrentTeamService> MockCurrentTeamService { get; set; }
    protected Mock<ICurrentUserService> MockCurrentUserService { get; set; }

    protected BaseIntegrationTest()
    {
        var services = new ServiceCollection();
        
        // Create mocks for required services
        MockCurrentTeamService = new Mock<ICurrentTeamService>();
        MockCurrentUserService = new Mock<ICurrentUserService>();
        
        // Setup default mock behavior
        MockCurrentTeamService.Setup(x => x.TeamId).Returns(Guid.NewGuid());
        MockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
        MockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        
        // Register mocked services
        services.AddSingleton(MockCurrentTeamService.Object);
        services.AddSingleton(MockCurrentUserService.Object);
        
        // Add logging services
        services.AddLogging();
        
        // Configure SQLite in-memory database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite("DataSource=:memory:"));

        // Add Identity services
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            // Configure identity options for testing
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 1;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        ServiceProvider = services.BuildServiceProvider();
        DbContext = ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Ensure database is created
        DbContext.Database.OpenConnection();
        DbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        DbContext.Database.CloseConnection();
        DbContext.Dispose();
    }

    /// <summary>
    /// Seeds the database with test data for a specific test.
    /// Override this method in derived test classes to provide test-specific data.
    /// </summary>
    protected virtual void SeedTestData()
    {
        // Override in derived classes to seed specific test data
    }

    /// <summary>
    /// Clears all data from the database.
    /// Useful for ensuring test isolation.
    /// </summary>
    protected void ClearDatabase()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Database.EnsureCreated();
    }
} 