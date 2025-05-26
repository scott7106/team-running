using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TeamStride.Domain.Common;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Interfaces;
using TeamStride.Infrastructure.Data.Extensions;

namespace TeamStride.Infrastructure.Data;

public class ApplicationDbContext : DbContext
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

    public DbSet<User> Users => Set<User>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();
    public DbSet<Athlete> Athletes => Set<Athlete>();
    public DbSet<AthleteProfile> AthleteProfiles => Set<AthleteProfile>();

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure global query filters
        modelBuilder.Entity<TenantUser>()
            .HasQueryFilter(tu => tu.TenantId == _tenantService.CurrentTenantId);

        modelBuilder.Entity<Athlete>()
            .HasQueryFilter(a => a.TenantId == _tenantService.CurrentTenantId);

        // Add global query filter for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(AuditedEntity<Guid>).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(AuditedEntity<Guid>.IsDeleted));
                var falseConstant = Expression.Constant(false);
                var lambda = Expression.Lambda(Expression.Equal(property, falseConstant), parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }

        // Configure relationships
        modelBuilder.Entity<TenantUser>()
            .HasOne(tu => tu.User)
            .WithMany(u => u.Tenants)
            .HasForeignKey(tu => tu.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TenantUser>()
            .HasOne(tu => tu.Tenant)
            .WithMany(t => t.Users)
            .HasForeignKey(tu => tu.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Athlete>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Athlete>()
            .HasOne(a => a.Profile)
            .WithOne(p => p.Athlete)
            .HasForeignKey<AthleteProfile>(p => p.AthleteId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes
        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.Subdomain)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Athlete>()
            .HasIndex(a => new { a.TenantId, a.UserId })
            .IsUnique();
    }
} 