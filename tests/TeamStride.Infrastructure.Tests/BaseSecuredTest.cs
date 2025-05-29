using TeamStride.Domain.Entities;
using TeamStride.Infrastructure.Tests.Helpers;

namespace TeamStride.Infrastructure.Tests;

/// <summary>
/// Base class for tests that need simplified security context setup
/// </summary>
public abstract class BaseSecuredTest : BaseIntegrationTest
{
    /// <summary>
    /// Sets up the test context for a global administrator
    /// </summary>
    protected void SetupGlobalAdminContext(Guid? userId = null)
    {
        var (userService, teamService) = TestContextBuilder.Create()
            .AsGlobalAdmin(userId)
            .Build();

        MockCurrentUserService = userService;
        MockCurrentTeamService = teamService;
    }

    /// <summary>
    /// Sets up the test context for a standard user with team access
    /// </summary>
    protected void SetupStandardUserContext(Guid teamId, TeamRole role = TeamRole.TeamMember, Guid? userId = null)
    {
        var (userService, teamService) = TestContextBuilder.Create()
            .AsStandardUser(teamId, role, userId)
            .Build();

        MockCurrentUserService = userService;
        MockCurrentTeamService = teamService;
    }

    /// <summary>
    /// Sets up the test context for an athlete user
    /// </summary>
    protected void SetupAthleteContext(Guid teamId, Guid? userId = null)
    {
        var (userService, teamService) = TestContextBuilder.Create()
            .AsAthlete(teamId, userId)
            .Build();

        MockCurrentUserService = userService;
        MockCurrentTeamService = teamService;
    }

    /// <summary>
    /// Sets up the test context for an anonymous user
    /// </summary>
    protected void SetupAnonymousContext()
    {
        var (userService, teamService) = TestContextBuilder.Create()
            .AsAnonymous()
            .Build();

        MockCurrentUserService = userService;
        MockCurrentTeamService = teamService;
    }
} 