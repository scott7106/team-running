using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;

namespace TeamStride.Infrastructure.Data;

public class DevelopmentTestDataSeeder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DevelopmentTestDataConfiguration _config;
    private readonly ILogger<DevelopmentTestDataSeeder> _logger;
    private readonly IHostEnvironment _environment;

    public DevelopmentTestDataSeeder(
        IServiceProvider serviceProvider,
        IOptions<DevelopmentTestDataConfiguration> config,
        ILogger<DevelopmentTestDataSeeder> logger,
        IHostEnvironment environment)
    {
        _serviceProvider = serviceProvider;
        _config = config.Value;
        _logger = logger;
        _environment = environment;
    }

    public async Task SeedAsync()
    {
        try
        {
            // Only run in development environment
            if (!_environment.IsDevelopment())
            {
                _logger.LogInformation("Not in development environment. Skipping test data seeding.");
                return;
            }

            if (!_config.SeedTestData)
            {
                _logger.LogInformation("Test data seeding is disabled. Skipping test data seeding.");
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            _logger.LogInformation("Starting development test data seeding...");

            // Ensure GlobalAdmin role exists
            await EnsureGlobalAdminRoleExists(roleManager);

            // Seed teams first
            var createdTeams = await SeedTeams(context, userManager);

            // Seed additional users
            await SeedUsers(context, userManager, createdTeams);

            _logger.LogInformation("Development test data seeding completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding test data.");
            throw;
        }
    }

    private async Task EnsureGlobalAdminRoleExists(RoleManager<ApplicationRole> roleManager)
    {
        if (!await roleManager.RoleExistsAsync("GlobalAdmin"))
        {
            var globalAdminRole = new ApplicationRole
            {
                Name = "GlobalAdmin",
                Description = "Global admin role"
            };
            await roleManager.CreateAsync(globalAdminRole);
            _logger.LogInformation("Created GlobalAdmin role.");
        }
    }

    private async Task<Dictionary<string, Team>> SeedTeams(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        var createdTeams = new Dictionary<string, Team>();

        foreach (var teamConfig in _config.Teams)
        {
            // Check if team already exists
            var existingTeam = await context.Teams
                .FirstOrDefaultAsync(t => t.Subdomain == teamConfig.Subdomain);

            if (existingTeam != null)
            {
                _logger.LogInformation("Team with subdomain '{Subdomain}' already exists. Skipping creation.", teamConfig.Subdomain);
                createdTeams[teamConfig.Subdomain] = existingTeam;
                continue;
            }

            // Create or get owner user
            var owner = await GetOrCreateUser(userManager, teamConfig.OwnerEmail, "TempPassword123!", "Team", "Owner");

            // Create team
            var team = new Team
            {
                Id = Guid.NewGuid(),
                Name = teamConfig.Name,
                Subdomain = teamConfig.Subdomain,
                OwnerId = owner.Id,
                Status = TeamStatus.Active,
                Tier = TeamTier.Standard,
                PrimaryColor = teamConfig.PrimaryColor ?? "#1E40AF",
                SecondaryColor = teamConfig.SecondaryColor ?? "#FFFFFF",
                CreatedOn = DateTime.UtcNow
            };

            context.Teams.Add(team);
            await context.SaveChangesAsync();

            // Create owner relationship in UserTeam
            var userTeam = new UserTeam
            {
                Id = Guid.NewGuid(),
                UserId = owner.Id,
                TeamId = team.Id,
                Role = TeamRole.TeamOwner,
                MemberType = MemberType.Coach,
                IsActive = true,
                IsDefault = owner.DefaultTeamId == null,
                JoinedOn = DateTime.UtcNow,
                CreatedOn = DateTime.UtcNow
            };

            context.UserTeams.Add(userTeam);

            // Set as default team if user doesn't have one
            if (owner.DefaultTeamId == null)
            {
                owner.DefaultTeamId = team.Id;
                await userManager.UpdateAsync(owner);
            }

            await context.SaveChangesAsync();

            createdTeams[teamConfig.Subdomain] = team;
            _logger.LogInformation("Created team '{TeamName}' with subdomain '{Subdomain}'", team.Name, team.Subdomain);
        }

        return createdTeams;
    }

    private async Task SeedUsers(ApplicationDbContext context, UserManager<ApplicationUser> userManager, Dictionary<string, Team> teams)
    {
        foreach (var userConfig in _config.Users)
        {
            // Check if user already exists
            var existingUser = await userManager.FindByEmailAsync(userConfig.Email);
            if (existingUser != null)
            {
                _logger.LogInformation("User with email '{Email}' already exists. Skipping creation.", userConfig.Email);
                continue;
            }

            // Create user
            var user = await GetOrCreateUser(userManager, userConfig.Email, userConfig.Password, userConfig.FirstName, userConfig.LastName);

            // Add to GlobalAdmin role if specified
            if (userConfig.IsGlobalAdmin)
            {
                var roles = await userManager.GetRolesAsync(user);
                if (!roles.Contains("GlobalAdmin"))
                {
                    await userManager.AddToRoleAsync(user, "GlobalAdmin");
                    _logger.LogInformation("Added user '{Email}' to GlobalAdmin role.", user.Email);
                }
            }

            // Create team memberships
            foreach (var membership in userConfig.TeamMemberships)
            {
                if (!teams.TryGetValue(membership.TeamSubdomain, out var team))
                {
                    _logger.LogWarning("Team with subdomain '{Subdomain}' not found for user '{Email}'. Skipping membership.", 
                        membership.TeamSubdomain, userConfig.Email);
                    continue;
                }

                // Check if membership already exists
                var existingMembership = await context.UserTeams
                    .FirstOrDefaultAsync(ut => ut.UserId == user.Id && ut.TeamId == team.Id);

                if (existingMembership != null)
                {
                    _logger.LogInformation("User '{Email}' is already a member of team '{TeamName}'. Skipping membership creation.", 
                        user.Email, team.Name);
                    continue;
                }

                // Parse role and member type
                if (!Enum.TryParse<TeamRole>(membership.Role, out var role))
                {
                    _logger.LogWarning("Invalid role '{Role}' for user '{Email}' in team '{TeamName}'. Using TeamMember.", 
                        membership.Role, user.Email, team.Name);
                    role = TeamRole.TeamMember;
                }

                if (!Enum.TryParse<MemberType>(membership.MemberType, out var memberType))
                {
                    _logger.LogWarning("Invalid member type '{MemberType}' for user '{Email}' in team '{TeamName}'. Using Coach.", 
                        membership.MemberType, user.Email, team.Name);
                    memberType = MemberType.Coach;
                }

                var userTeam = new UserTeam
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    TeamId = team.Id,
                    Role = role,
                    MemberType = memberType,
                    IsActive = true,
                    IsDefault = membership.IsDefault && user.DefaultTeamId == null,
                    JoinedOn = DateTime.UtcNow,
                    CreatedOn = DateTime.UtcNow
                };

                context.UserTeams.Add(userTeam);

                // Set as default team if specified and user doesn't have one
                if (membership.IsDefault && user.DefaultTeamId == null)
                {
                    user.DefaultTeamId = team.Id;
                    await userManager.UpdateAsync(user);
                }

                _logger.LogInformation("Added user '{Email}' to team '{TeamName}' with role '{Role}' and member type '{MemberType}'", 
                    user.Email, team.Name, role, memberType);
            }

            await context.SaveChangesAsync();
        }
    }

    private async Task<ApplicationUser> GetOrCreateUser(UserManager<ApplicationUser> userManager, string email, string password, string firstName, string lastName)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user != null)
        {
            return user;
        }

        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true,
            IsActive = true,
            Status = UserStatus.Active,
            CreatedOn = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception($"Failed to create user '{email}': {errors}");
        }

        _logger.LogInformation("Created user '{Email}' ({FirstName} {LastName})", email, firstName, lastName);
        return user;
    }
} 