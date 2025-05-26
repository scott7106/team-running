using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TeamStride.Infrastructure.Data;
using Xunit;

namespace TeamStride.Api.Tests;

/// <summary>
/// Base class for API integration tests.
/// Provides test server setup with SQLite in-memory database.
/// </summary>
public abstract class BaseApiTest : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    protected HttpClient Client { get; private set; }
    protected WebApplicationFactory<Program> Factory { get; private set; }
    protected ApplicationDbContext DbContext { get; private set; }

    protected BaseApiTest(WebApplicationFactory<Program> factory)
    {
        Factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add SQLite in-memory database for testing
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlite("DataSource=:memory:"));
            });
        });

        Client = Factory.CreateClient();
        
        // Get DbContext and ensure database is created
        using var scope = Factory.Services.CreateScope();
        DbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        DbContext.Database.OpenConnection();
        DbContext.Database.EnsureCreated();
    }

    public virtual void Dispose()
    {
        Client?.Dispose();
        DbContext?.Dispose();
        Factory?.Dispose();
    }

    /// <summary>
    /// Seeds the database with test data for API tests.
    /// Override this method in derived test classes to provide test-specific data.
    /// </summary>
    protected virtual async Task SeedTestDataAsync()
    {
        // Override in derived classes to seed specific test data
        await Task.CompletedTask;
    }

    /// <summary>
    /// Clears all data from the database.
    /// Useful for ensuring test isolation between API tests.
    /// </summary>
    protected async Task ClearDatabaseAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        
        await Task.CompletedTask;
    }
} 