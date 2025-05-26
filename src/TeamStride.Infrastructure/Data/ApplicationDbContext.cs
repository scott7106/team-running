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
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantService tenantService,
        ICurrentUserService currentUserService) : base(options)
    {
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    // Application entities
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<UserTenant> UserTenants => Set<UserTenant>();
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
    }

    private void ConfigureGlobalQueryFilters(ModelBuilder builder)
    {
        // Configure multi-tenant entities with soft delete filters
        builder.Entity<Athlete>().HasQueryFilter(e => 
            !e.IsDeleted && e.TenantId == _tenantService.CurrentTenantId);
        
        builder.Entity<AthleteProfile>().HasQueryFilter(e => 
            !e.IsDeleted && e.TenantId == _tenantService.CurrentTenantId);
        
        builder.Entity<UserTenant>().HasQueryFilter(e => 
            !e.IsDeleted && (e.TenantId == null || e.TenantId == _tenantService.CurrentTenantId));

        // Configure entities with only soft delete filters
        builder.Entity<Tenant>().HasQueryFilter(e => !e.IsDeleted);
        
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