using Moq;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Interfaces;

namespace TeamStride.Infrastructure.Tests.Helpers;

/// <summary>
/// Helper for building test contexts with simplified security configurations
/// </summary>
public class TestContextBuilder
{
    private Guid? _userId;
    private Guid? _teamId;
    private bool _isGlobalAdmin;
    private bool _isAuthenticated = true;
    private TeamRole? _teamRole;
    private MemberType? _memberType;

    public static TestContextBuilder Create() => new();

    public TestContextBuilder AsAnonymous()
    {
        _isAuthenticated = false;
        _userId = null;
        _teamId = null;
        _isGlobalAdmin = false;
        _teamRole = null;
        _memberType = null;
        return this;
    }

    public TestContextBuilder AsGlobalAdmin(Guid? userId = null)
    {
        _isAuthenticated = true;
        _isGlobalAdmin = true;
        _userId = userId ?? Guid.NewGuid();
        _teamId = null; // Global admins don't have a specific team
        _teamRole = null;
        _memberType = null;
        return this;
    }

    public TestContextBuilder AsStandardUser(Guid teamId, TeamRole role = TeamRole.TeamMember, Guid? userId = null)
    {
        _isAuthenticated = true;
        _isGlobalAdmin = false;
        _userId = userId ?? Guid.NewGuid();
        _teamId = teamId;
        _teamRole = role;
        _memberType = MemberType.Coach; // Default to coach
        return this;
    }

    public TestContextBuilder AsAthlete(Guid teamId, Guid? userId = null)
    {
        _isAuthenticated = true;
        _isGlobalAdmin = false;
        _userId = userId ?? Guid.NewGuid();
        _teamId = teamId;
        _teamRole = TeamRole.TeamMember;
        _memberType = MemberType.Athlete;
        return this;
    }

    public TestContextBuilder WithUser(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public TestContextBuilder WithTeam(Guid teamId)
    {
        if (_isGlobalAdmin)
        {
            throw new InvalidOperationException("Global admins cannot be assigned to a specific team");
        }
        _teamId = teamId;
        return this;
    }

    public TestContextBuilder WithRole(TeamRole role)
    {
        if (_isGlobalAdmin)
        {
            throw new InvalidOperationException("Global admins don't have team roles");
        }
        _teamRole = role;
        return this;
    }

    public TestContextBuilder WithMemberType(MemberType memberType)
    {
        if (_isGlobalAdmin)
        {
            throw new InvalidOperationException("Global admins don't have member types");
        }
        _memberType = memberType;
        return this;
    }

    public (Mock<ICurrentUserService> UserService, Mock<ICurrentTeamService> TeamService) Build()
    {
        var mockUserService = new Mock<ICurrentUserService>();
        var mockTeamService = new Mock<ICurrentTeamService>();

        // Setup user service
        mockUserService.Setup(x => x.UserId).Returns(_userId);
        mockUserService.Setup(x => x.IsAuthenticated).Returns(_isAuthenticated);
        mockUserService.Setup(x => x.IsGlobalAdmin).Returns(_isGlobalAdmin);
        
        // Delegate team operations to team service
        mockUserService.Setup(x => x.CanAccessTeam(It.IsAny<Guid>()))
            .Returns<Guid>(teamId => mockTeamService.Object.CanAccessTeam(teamId));
        mockUserService.Setup(x => x.HasMinimumTeamRole(It.IsAny<TeamRole>()))
            .Returns<TeamRole>(role => mockTeamService.Object.HasMinimumTeamRole(role));

        // Setup team service
        if (_teamId.HasValue)
        {
            mockUserService.Setup(x => x.CurrentTeamId).Returns(_teamId.Value);
            mockTeamService.Setup(x => x.TeamId).Returns(_teamId.Value);
            mockTeamService.Setup(x => x.IsTeamSet).Returns(true);
        }
        else
        {
            mockTeamService.Setup(x => x.TeamId).Throws(new InvalidOperationException("Current team is not set"));
            mockTeamService.Setup(x => x.IsTeamSet).Returns(false);
        }

        mockTeamService.Setup(x => x.TeamRole).Returns(_teamRole);
        mockTeamService.Setup(x => x.MemberType).Returns(_memberType);
        mockTeamService.Setup(x => x.IsTeamOwner).Returns(_teamRole == TeamRole.TeamOwner);
        mockTeamService.Setup(x => x.IsTeamAdmin).Returns(_teamRole == TeamRole.TeamAdmin);
        mockTeamService.Setup(x => x.IsTeamMember).Returns(_teamRole == TeamRole.TeamMember);

        // Setup authorization methods
        mockTeamService.Setup(x => x.CanAccessTeam(It.IsAny<Guid>()))
            .Returns<Guid>(teamId => _isGlobalAdmin || (_teamId.HasValue && _teamId.Value == teamId));
        
        mockTeamService.Setup(x => x.CanAccessCurrentTeam())
            .Returns(() => _teamId.HasValue && mockTeamService.Object.CanAccessTeam(_teamId.Value));

        mockTeamService.Setup(x => x.HasMinimumTeamRole(It.IsAny<TeamRole>()))
            .Returns<TeamRole>(minimumRole => _isGlobalAdmin || 
                (_teamRole.HasValue && IsRoleSufficient(_teamRole.Value, minimumRole)));

        return (mockUserService, mockTeamService);
    }

    private static bool IsRoleSufficient(TeamRole userRole, TeamRole minimumRole)
    {
        var hierarchy = new Dictionary<TeamRole, int>
        {
            { TeamRole.TeamOwner, 1 },
            { TeamRole.TeamAdmin, 2 },
            { TeamRole.TeamMember, 3 }
        };

        return hierarchy[userRole] <= hierarchy[minimumRole];
    }
} 