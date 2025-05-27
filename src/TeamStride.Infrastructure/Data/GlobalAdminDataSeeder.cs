using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;

namespace TeamStride.Infrastructure.Data;

public class GlobalAdminDataSeeder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly GlobalAdminConfiguration _config;
    private readonly ILogger<GlobalAdminDataSeeder> _logger;

    public GlobalAdminDataSeeder(
        IServiceProvider serviceProvider,
        IOptions<GlobalAdminConfiguration> config,
        ILogger<GlobalAdminDataSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _config = config.Value;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_config.Email) || string.IsNullOrEmpty(_config.Password))
            {
                _logger.LogInformation("No global admin configuration found. Skipping global admin seeding.");
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Check if global admin already exists
            var existingAdmin = await userManager.FindByEmailAsync(_config.Email);
            if (existingAdmin != null)
            {
                _logger.LogInformation("Global admin already exists. Skipping global admin seeding.");
                return;
            }

            // Create global admin user
            var globalAdmin = new ApplicationUser
            {
                UserName = _config.Email,
                Email = _config.Email,
                FirstName = _config.FirstName ?? "Global",
                LastName = _config.LastName ?? "Admin",
                DefaultTeamId = null,
                EmailConfirmed = true,
                IsActive = true,
                Status = UserStatus.Active,
                CreatedOn = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(globalAdmin, _config.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors);
                throw new Exception($"Failed to create global admin user: {errors}");
            }

            // Set global admin status using the new method
            globalAdmin.SetGlobalAdmin(true);
            await userManager.UpdateAsync(globalAdmin);

            // Create global admin role
            var globalAdminRole = new ApplicationRole
            {
                Name = "GlobalAdmin",
                Description = "Global admin role"
            };

            if (!await roleManager.RoleExistsAsync(globalAdminRole.Name)) {
                await roleManager.CreateAsync(globalAdminRole);
            }

            // Add global admin user to global admin role
            await userManager.AddToRoleAsync(globalAdmin, globalAdminRole.Name);
            _logger.LogInformation("Successfully created global admin user.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the global admin user.");
            throw;
        }
    }
} 