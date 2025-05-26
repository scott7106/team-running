using Microsoft.EntityFrameworkCore;
using TeamStride.Domain.Common;

namespace TeamStride.Infrastructure.Data.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> NotDeleted<T>(this IQueryable<T> query) where T : AuditedEntity<Guid>
    {
        return query.Where(x => !x.IsDeleted);
    }

    public static IQueryable<T> OnlyDeleted<T>(this IQueryable<T> query) where T : AuditedEntity<Guid>
    {
        return query.Where(x => x.IsDeleted);
    }

    public static IQueryable<T> IncludeDeleted<T>(this IQueryable<T> query) where T : AuditedEntity<Guid>
    {
        return query;
    }
} 