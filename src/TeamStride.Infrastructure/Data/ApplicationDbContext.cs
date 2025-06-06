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
    public readonly ICurrentUserService _currentUserService;
    private bool _bypassAuditHandling = false;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    // Application entities
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<UserTeam> UserTeams => Set<UserTeam>();
    public DbSet<Athlete> Athletes => Set<Athlete>();
    public DbSet<AthleteProfile> AthleteProfiles => Set<AthleteProfile>();
    public DbSet<OwnershipTransfer> OwnershipTransfers => Set<OwnershipTransfer>();

    // Identity entities
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();

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
            .WithMany(u => u.UserTeams)
            .HasForeignKey(ut => ut.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserTeam>()
            .HasOne(ut => ut.Team)
            .WithMany(t => t.Users)
            .HasForeignKey(ut => ut.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<OwnershipTransfer>()
            .HasOne(ot => ot.Team)
            .WithMany()
            .HasForeignKey(ot => ot.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<OwnershipTransfer>()
            .HasOne(ot => ot.InitiatedByUser)
            .WithMany()
            .HasForeignKey(ot => ot.InitiatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OwnershipTransfer>()
            .HasOne(ot => ot.ExistingMember)
            .WithMany()
            .HasForeignKey(ot => ot.ExistingMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<OwnershipTransfer>()
            .HasOne(ot => ot.CompletedByUser)
            .WithMany()
            .HasForeignKey(ot => ot.CompletedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UserSession>()
            .HasOne(us => us.User)
            .WithMany(u => u.Sessions)
            .HasForeignKey(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private void ConfigureGlobalQueryFilters(ModelBuilder builder)
    {
        // Configure entities with only soft delete filters
        builder.Entity<Athlete>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<AthleteProfile>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<UserTeam>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Team>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<ApplicationUser>().HasQueryFilter(e => !e.IsDeleted);

        // Configure RefreshToken to filter out tokens for soft-deleted users
        builder.Entity<RefreshToken>().HasQueryFilter(rt =>
            rt.User != null && !rt.User.IsDeleted);

        // Configure OwnershipTransfer with soft delete filter
        builder.Entity<OwnershipTransfer>().HasQueryFilter(e => !e.IsDeleted);

        // Configure UserSession to filter out sessions for soft-deleted users
        builder.Entity<UserSession>().HasQueryFilter(us =>
            us.User != null && !us.User.IsDeleted);
    }

    /// <summary>
    /// Saves changes without applying audit handling (allows permanent deletion).
    /// Use this method when you need to bypass soft delete behavior.
    /// </summary>
    public async Task<int> SaveChangesWithoutAuditAsync(CancellationToken cancellationToken = default)
    {
        _bypassAuditHandling = true;
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _bypassAuditHandling = false;
        }
    }

    /// <summary>
    /// Saves changes without applying audit handling (allows permanent deletion).
    /// Use this method when you need to bypass soft delete behavior.
    /// </summary>
    public int SaveChangesWithoutAudit()
    {
        _bypassAuditHandling = true;
        try
        {
            return base.SaveChanges();
        }
        finally
        {
            _bypassAuditHandling = false;
        }
    }

    public override int SaveChanges()
    {
        if (!_bypassAuditHandling)
        {
            HandleAuditFields();
        }

        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        if (!_bypassAuditHandling)
        {
            HandleAuditFields();
        }

        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (!_bypassAuditHandling)
        {
            HandleAuditFields();
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        if (!_bypassAuditHandling)
        {
            HandleAuditFields();
        }

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void HandleAuditFields()
    {
        ChangeTracker.HandleAuditableEntities(_currentUserService);
    }
}