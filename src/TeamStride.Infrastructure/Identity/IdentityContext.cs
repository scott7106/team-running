using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TeamStride.Domain.Identity;

namespace TeamStride.Infrastructure.Identity;

public class IdentityContext : IdentityDbContext<ApplicationUser>
{
    public IdentityContext(DbContextOptions<IdentityContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<UserTenant> UserTenants { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserTenant>()
            .HasKey(ut => new { ut.UserId, ut.TenantId });

        builder.Entity<UserTenant>()
            .HasOne(ut => ut.User)
            .WithMany(u => u.UserTenants)
            .HasForeignKey(ut => ut.UserId);

        builder.Entity<UserTenant>()
            .HasOne(ut => ut.Tenant)
            .WithMany(t => t.UserTenants)
            .HasForeignKey(ut => ut.TenantId);

        builder.Entity<Tenant>()
            .HasIndex(t => t.Subdomain)
            .IsUnique();

        builder.Entity<RefreshToken>()
            .HasKey(rt => rt.Token);

        builder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId);

        builder.Entity<RefreshToken>()
            .HasIndex(rt => new { rt.UserId, rt.TenantId });
    }
} 