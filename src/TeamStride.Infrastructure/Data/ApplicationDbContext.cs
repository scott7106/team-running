using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TeamStride.Domain.Common;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;
using TeamStride.Domain.Interfaces;
using TeamStride.Infrastructure.Data.Extensions;
using Microsoft.AspNetCore.Identity;

namespace TeamStride.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    private readonly ICurrentTeamService _teamService;
    private readonly ICurrentUserService _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentTeamService teamService,
        ICurrentUserService currentUserService) : base(options)
    {
        _teamService = teamService;
        _currentUserService = currentUserService;
    }

    // Application entities
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<UserTeam> UserTeams => Set<UserTeam>();
    public DbSet<Athlete> Athletes => Set<Athlete>();
    public DbSet<AthleteProfile> AthleteProfiles => Set<AthleteProfile>();
    
    // Identity entities
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Remove AspNet prefix from Identity tables
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

        // Configure global query filters for soft deletes and multi-tenancy
        ConfigureGlobalQueryFilters(builder);

        // Configure relationships
        builder.Entity<Athlete>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Athlete>()
            .HasOne(a => a.Profile)
            .WithOne(p => p.Athlete)
            .HasForeignKey<AthleteProfile>(p => p.AthleteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserTeam>()
            .HasOne(ut => ut.User)
            .WithMany()
            .HasForeignKey(ut => ut.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserTeam>()
            .HasOne(ut => ut.Team)
            .WithMany()
            .HasForeignKey(ut => ut.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private void ConfigureGlobalQueryFilters(ModelBuilder builder)
    {
        // Configure multi-team entities with soft delete filters
        builder.Entity<Athlete>().HasQueryFilter(e => 
            !e.IsDeleted && e.TeamId == _teamService.TeamId);
        
        builder.Entity<AthleteProfile>().HasQueryFilter(e => 
            !e.IsDeleted && e.TeamId == _teamService.TeamId);
        
        builder.Entity<UserTeam>().HasQueryFilter(e => !e.IsDeleted);

        // Configure entities with only soft delete filters
        builder.Entity<Team>().HasQueryFilter(e => 
            !e.IsDeleted && (e.Users.Any(ut => ut.UserId == _currentUserService.UserId) || _currentUserService.IsGlobalAdmin));
        
        builder.Entity<ApplicationUser>().HasQueryFilter(e => !e.IsDeleted);
        
        // Configure RefreshToken to filter out tokens for soft-deleted users
        builder.Entity<RefreshToken>().HasQueryFilter(rt => 
            rt.User != null && !rt.User.IsDeleted);
    }

    public override int SaveChanges()
    {
        HandleAuditFields();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        HandleAuditFields();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        HandleAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        HandleAuditFields();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void HandleAuditFields()
    {
        ChangeTracker.HandleAuditableEntities(_currentUserService);
    }
} 