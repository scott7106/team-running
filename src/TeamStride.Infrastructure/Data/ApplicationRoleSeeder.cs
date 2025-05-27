using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TeamStride.Domain.Identity;

namespace TeamStride.Infrastructure.Data;

public class ApplicationRoleSeeder
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<ApplicationRoleSeeder> _logger;

    private static readonly Dictionary<string, string> ApplicationRoles = new()
    {
        { "Admin", "Full administrative access to manage teams, users, and system settings" },
        { "Coach", "Access to manage team activities, athletes, and training plans" },
        { "Parent", "Access to view and manage their children's activities and profiles" },
        { "Athlete", "Access to view their training plans, events, and personal profile" }
    };

    public ApplicationRoleSeeder(
        RoleManager<ApplicationRole> roleManager,
        ILogger<ApplicationRoleSeeder> logger)
    {
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            foreach (var role in ApplicationRoles)
            {
                if (!await _roleManager.RoleExistsAsync(role.Key))
                {
                    var applicationRole = new ApplicationRole(role.Key)
                    {
                        Description = role.Value
                    };

                    var result = await _roleManager.CreateAsync(applicationRole);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Created role {RoleName}", role.Key);
                    }
                    else
                    {
                        var errors = string.Join(", ", result.Errors);
                        _logger.LogError("Failed to create role {RoleName}. Errors: {Errors}", role.Key, errors);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding application roles");
            throw;
        }
    }
} 