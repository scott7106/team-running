using Microsoft.EntityFrameworkCore;
using TeamStride.Domain.Common;
using TeamStride.Domain.Interfaces;

namespace TeamStride.Infrastructure.Data.Extensions;

/// <summary>
/// Security-focused query extensions for team isolation and access control
/// </summary>
public static class SecurityQueryExtensions
{
    /// <summary>
    /// Applies team security filtering based on the current security context.
    /// Global admins bypass filters, standard users see only their team's data.
    /// </summary>
    public static IQueryable<T> ApplyTeamSecurity<T>(
        this IQueryable<T> query, 
        ICurrentUserService currentUserService) 
        where T : class, IHasTeam
    {
        if (currentUserService.IsGlobalAdmin)
        {
            return query.IgnoreQueryFilters();
        }

        if (currentUserService.CurrentTeamId.HasValue)
        {
            return query.Where(e => e.TeamId == currentUserService.CurrentTeamId.Value);
        }

        // No team access - return empty result
        return query.Where(e => false);
    }

    /// <summary>
    /// Applies team security with soft delete filtering
    /// </summary>
    public static IQueryable<T> ApplySecuredAccess<T>(
        this IQueryable<T> query,
        ICurrentUserService currentUserService)
        where T : class, IHasTeam, IAuditedEntity
    {
        return query.ApplyTeamSecurity(currentUserService)
                   .NotDeleted();
    }

    /// <summary>
    /// Forces global admin access by ignoring all query filters
    /// Use this when you need to bypass both team and soft delete filters
    /// </summary>
    public static IQueryable<T> AsGlobalAdmin<T>(this IQueryable<T> query) where T : class
    {
        return query.IgnoreQueryFilters();
    }

    /// <summary>
    /// Applies standard user security (team isolation + soft delete filter)
    /// </summary>
    public static IQueryable<T> AsStandardUser<T>(
        this IQueryable<T> query,
        Guid teamId)
        where T : class, IHasTeam, IAuditedEntity
    {
        return query.NotDeleted().Where(e => e.TeamId == teamId);
    }
} 