using Microsoft.EntityFrameworkCore;
using TeamStride.Domain.Common;

namespace TeamStride.Infrastructure.Data.Extensions;

public static class QueryableExtensions
{
    /// <summary>
    /// Explicitly filters out deleted entities. This is redundant when global query filters are active,
    /// but can be useful for clarity or when global filters are disabled.
    /// </summary>
    public static IQueryable<T> NotDeleted<T>(this IQueryable<T> query) where T : class, IAuditedEntity
    {
        return query.Where(x => !x.IsDeleted);
    }

    /// <summary>
    /// Returns only deleted entities. This ignores global query filters and explicitly filters for deleted items.
    /// </summary>
    public static IQueryable<T> OnlyDeleted<T>(this IQueryable<T> query) where T : class, IAuditedEntity
    {
        return query.IgnoreQueryFilters().Where(x => x.IsDeleted);
    }

    /// <summary>
    /// Includes both deleted and non-deleted entities by ignoring global query filters.
    /// </summary>
    public static IQueryable<T> IncludeDeleted<T>(this IQueryable<T> query) where T : class, IAuditedEntity
    {
        return query.IgnoreQueryFilters();
    }
} 