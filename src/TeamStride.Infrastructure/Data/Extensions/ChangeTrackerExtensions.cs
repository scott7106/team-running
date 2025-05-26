using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using TeamStride.Domain.Common;
using TeamStride.Domain.Interfaces;

namespace TeamStride.Infrastructure.Data.Extensions;

public static class ChangeTrackerExtensions
{
    public static void HandleAuditableEntities(this ChangeTracker changeTracker, ICurrentUserService currentUserService)
    {
        var entries = changeTracker.Entries<IAuditedEntity>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedOn = DateTime.UtcNow;
                    entry.Entity.CreatedBy = currentUserService.UserId;
                    break;

                case EntityState.Modified:
                    entry.Entity.ModifiedOn = DateTime.UtcNow;
                    entry.Entity.ModifiedBy = currentUserService.UserId;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedOn = DateTime.UtcNow;
                    entry.Entity.DeletedBy = currentUserService.UserId;
                    break;
            }
        }
    }
} 