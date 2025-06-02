using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TeamStride.Infrastructure.Data;

public class DevelopmentTestDataSeederHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DevelopmentTestDataSeederHostedService> _logger;

    public DevelopmentTestDataSeederHostedService(
        IServiceProvider serviceProvider,
        ILogger<DevelopmentTestDataSeederHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<DevelopmentTestDataSeeder>();
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding development test data.");
            // Don't rethrow in development to avoid crashing the app
            // The seeder will log the error and the app can continue
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
} 