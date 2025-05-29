using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TeamStride.Domain.Common;
using TeamStride.Domain.Entities;

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
    
    /// <summary>
    /// Conditionally applies a filter to the query based on the provided condition.
    /// </summary>
    /// <param name="query">The queryable object</param>
    /// <param name="condition">a boolean expression</param>
    /// <param name="predicate">the constraint to add if the expression is true</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, Expression<Func<T, bool>> predicate)
    {
        return condition
            ? query.Where(predicate)
            : query;
    }

    /// <summary>
    /// Applies team access filter to Team queries based on the provided current team ID.
    /// If currentTeamId is provided, only teams where the user has access through UserTeam relationships are returned.
    /// </summary>
    /// <param name="query">The Team queryable object</param>
    /// <param name="currentTeamId">The current team ID to filter by (nullable)</param>
    /// <returns>Filtered queryable for teams the user has access to</returns>
    public static IQueryable<Team> ApplyTeamMembershipFilter(this IQueryable<Team> query, Guid? currentTeamId)
    {
        return query.WhereIf(currentTeamId.HasValue,
            e => e.Users.Any(x => x.TeamId == currentTeamId!.Value));
    }

    /// <summary>
    /// Applies a filter to Team.Id based on the user's current team ID.
    /// If currentTeamId is provided, only the selected team is returned.
    /// </summary>
    /// <param name="query">The Team queryable object</param>
    /// <param name="currentTeamId">The current team ID to filter by (nullable)</param>
    /// <returns>Filtered queryable for teams the user has access to</returns>
    public static IQueryable<Team> ApplyTeamFilter(this IQueryable<Team> query, Guid? currentTeamId)
    {
        return query.WhereIf(currentTeamId.HasValue,
            e => e.Id == currentTeamId!.Value);
    }
    
    /// <summary>
    /// Applies a filter to entities with TeamId based on the user's current team ID.
    /// If currentTeamId is provided, only entities belonging to the selected team are returned.
    /// </summary>
    /// <param name="query">The queryable object</param>
    /// <param name="currentTeamId">The current team ID to filter by (nullable)</param>
    /// <returns>Filtered queryable for entities belonging to the user's team</returns>
    public static IQueryable<T> ApplyTeamIdFilter<T>(this IQueryable<T> query, Guid? currentTeamId) where T : class, IHasTeam
    {
        return query.WhereIf(currentTeamId.HasValue,
            e => e.TeamId == currentTeamId!.Value);
    }
} 