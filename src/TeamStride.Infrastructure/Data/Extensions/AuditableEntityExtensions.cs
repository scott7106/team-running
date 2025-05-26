using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TeamStride.Domain.Common;
using TeamStride.Domain.Interfaces;

namespace TeamStride.Infrastructure.Data.Extensions;

public static class AuditableEntityExtensions
{
    public static void HandleAuditableEntities(this ChangeTracker changeTracker, ICurrentUserService currentUserService)
    {
        var userId = currentUserService.UserId;
        var utcNow = DateTime.UtcNow;

        foreach (var entry in changeTracker.Entries<AuditedEntity<Guid>>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedOn = utcNow;
                    entry.Entity.CreatedBy = userId;
                    break;

                case EntityState.Modified:
                    entry.Entity.ModifiedOn = utcNow;
                    entry.Entity.ModifiedBy = userId;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.DeletedOn = utcNow;
                    entry.Entity.DeletedBy = userId;
                    entry.Entity.IsDeleted = true;
                    break;
            }
        }
    }
} 