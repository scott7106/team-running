using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using System.Security.Claims;
using TeamStride.Api.Authorization;
using TeamStride.Domain.Entities;
using Xunit;

namespace TeamStride.Api.Tests.Authorization;

public class RequireTeamAccessAttributeTests
{
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<ClaimsPrincipal> _mockUser;
    private readonly Mock<ClaimsIdentity> _mockIdentity;

    public RequireTeamAccessAttributeTests()
    {
        _mockHttpContext = new Mock<HttpContext>();
        _mockUser = new Mock<ClaimsPrincipal>();
        _mockIdentity = new Mock<ClaimsIdentity>();

        _mockHttpContext.Setup(x => x.User).Returns(_mockUser.Object);
        _mockUser.Setup(x => x.Identity).Returns(_mockIdentity.Object);
    }

    private AuthorizationFilterContext CreateAuthorizationContext(Dictionary<string, object>? routeValues = null)
    {
        var routeData = new RouteData();
        if (routeValues != null)
        {
            foreach (var kvp in routeValues)
            {
                routeData.Values[kvp.Key] = kvp.Value;
            }
        }

        var actionContext = new ActionContext(
            _mockHttpContext.Object,
            routeData,
            new ActionDescriptor()
        );

        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    private void SetupUserClaims(List<Claim> claims)
    {
        _mockIdentity.Setup(x => x.IsAuthenticated).Returns(true);
        foreach (var claim in claims)
        {
            _mockUser.Setup(x => x.FindFirst(claim.Type)).Returns(claim);
        }
    }

    #region Authentication Tests

    [Fact]
    public void OnAuthorization_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var attribute = new RequireTeamAccessAttribute();
        _mockIdentity.Setup(x => x.IsAuthenticated).Returns(false);
        var context = CreateAuthorizationContext();

        // Act
        attribute.OnAuthorization(context);

        // Assert
        Assert.NotNull(context.Result);
        Assert.IsType<UnauthorizedResult>(context.Result);
    }

    [Fact]
    public void OnAuthorization_WhenUserIdentityIsNull_ShouldReturnUnauthorized()
    {
        // Arrange
        var attribute = new RequireTeamAccessAttribute();
        _mockUser.Setup(x => x.Identity).Returns((ClaimsIdentity?)null);
        var context = CreateAuthorizationContext();

        // Act
        attribute.OnAuthorization(context);

        // Assert
        Assert.NotNull(context.Result);
        Assert.IsType<UnauthorizedResult>(context.Result);
    }

    #endregion

    #region Global Admin Tests

    [Fact]
    public void OnAuthorization_WhenUserIsGlobalAdmin_ShouldAllowAccess()
    {
        // Arrange
        var attribute = new RequireTeamAccessAttribute(TeamRole.TeamOwner);
        var claims = new List<Claim>
        {
            new("is_global_admin", "true")
        };
        SetupUserClaims(claims);
        var context = CreateAuthorizationContext();

        // Act
        attribute.OnAuthorization(context);

        // Assert
        Assert.Null(context.Result);
    }

    [Fact]
    public void OnAuthorization_WhenGlobalAdminClaimIsFalse_ShouldContinueWithTeamValidation()
    {
        // Arrange
        var attribute = new RequireTeamAccessAttribute();
        var claims = new List<Claim>
        {
            new("is_global_admin", "false")
        };
        SetupUserClaims(claims);
        var context = CreateAuthorizationContext();

        // Act
        attribute.OnAuthorization(context);

        // Assert
        Assert.NotNull(context.Result);
        Assert.IsType<ForbidResult>(context.Result);
    }

    #endregion

    #region Team ID Validation Tests

    [Fact]
    public void OnAuthorization_WhenTeamIdClaimIsMissing_ShouldDenyAccess()
    {
        // Arrange
        var attribute = new RequireTeamAccessAttribute();
        var claims = new List<Claim>();
        SetupUserClaims(claims);
        var context = CreateAuthorizationContext();

        // Act
        attribute.OnAuthorization(context);

        // Assert
        Assert.NotNull(context.Result);
        var forbidResult = Assert.IsType<ForbidResult>(context.Result);
        Assert.Contains("User is not associated with any team", forbidResult.AuthenticationSchemes.FirstOrDefault() ?? "");
    }

    [Fact]
    public void OnAuthorization_WhenTeamIdClaimIsInvalid_ShouldDenyAccess()
    {
        // Arrange
        var attribute = new RequireTeamAccessAttribute();
        var claims = new List<Claim>
        {
            new("team_id", "invalid-guid")
        };
        SetupUserClaims(claims);
        var context = CreateAuthorizationContext();

        // Act
        attribute.OnAuthorization(context);

        // Assert
        Assert.NotNull(context.Result);
        Assert.IsType<ForbidResult>(context.Result);
    }

    #endregion

    #region Team Role Validation Tests

    [Fact]
    public void OnAuthorization_WhenTeamRoleClaimIsMissing_ShouldDenyAccess()
    {
        // Arrange
        var attribute = new RequireTeamAccessAttribute();
        var teamId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new("team_id", teamId.ToString())
        };
        SetupUserClaims(claims);
        var context = CreateAuthorizationContext();

        // Act
        attribute.OnAuthorization(context);

        // Assert
        Assert.NotNull(context.Result);
        var forbidResult = Assert.IsType<ForbidResult>(context.Result);
        Assert.Contains("User team role is not specified or invalid", forbidResult.AuthenticationSchemes.FirstOrDefault() ?? "");
    }

    [Fact]
    public void OnAuthorization_WhenTeamRoleClaimIsInvalid_ShouldDenyAccess()
    {
        // Arrange
        var attribute = new RequireTeamAccessAttribute();
        var teamId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new("team_id", teamId.ToString()),
            new("team_role", "InvalidRole")
        };
        SetupUserClaims(claims);
        var context = CreateAuthorizationContext();

        // Act
        attribute.OnAuthorization(context);

        // Assert
        Assert.NotNull(context.Result);
        Assert.IsType<ForbidResult>(context.Result);
    }

    #endregion

    #region Route Team ID Validation Tests

    [Fact]
    public void OnAuthorization_WhenRouteTeamIdMatchesUserTeam_ShouldAllowAccess()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var attribute = new RequireTeamAccessAttribute(TeamRole.TeamMember, requireTeamIdFromRoute: true);
        var claims = new List<Claim>
        {
            new("team_id", teamId.ToString()),
            new("team_role", TeamRole.TeamMember.ToString())
        };
        SetupUserClaims(claims);
        var routeValues = new Dictionary<string, object> { { "teamId", teamId } };
        var context = CreateAuthorizationContext(routeValues);

        // Act
        attribute.OnAuthorization(context);

        // Assert
        Assert.Null(context.Result);
    }

    [Fact]
    public void OnAuthorization_WhenRouteTeamIdDoesNotMatchUserTeam_ShouldDenyAccess()
    {
        // Arrange
        var userTeamId = Guid.NewGuid();
        var routeTeamId = Guid.NewGuid();
        var attribute = new RequireTeamAccessAttribute(TeamRole.TeamMember, requireTeamIdFromRoute: true);
        var claims = new List<Claim>
        {
            new("team_id", userTeamId.ToString()),
            new("team_role", TeamRole.TeamMember.ToString())
        };
        SetupUserClaims(claims);
        var routeValues = new Dictionary<string, object> { { "teamId", routeTeamId } };
        var context = CreateAuthorizationContext(routeValues);

        // Act
        attribute.OnAuthorization(context);

        // Assert
        Assert.NotNull(context.Result);
        var forbidResult = Assert.IsType<ForbidResult>(context.Result);
        Assert.Contains("Access denied: User does not have access to the specified team", forbidResult.AuthenticationSchemes.FirstOrDefault() ?? "");
    }

    [Fact]
    public void OnAuthorization_WhenRequireTeamIdFromRouteIsFalse_ShouldSkipRouteValidation()
    {
        // Arrange
        var userTeamId = Guid.NewGuid();
        var routeTeamId = Guid.NewGuid();
        var attribute = new RequireTeamAccessAttribute(TeamRole.TeamMember, requireTeamIdFromRoute: false);
        var claims = new List<Claim>
        {
            new("team_id", userTeamId.ToString()),
            new("team_role", TeamRole.TeamMember.ToString())
        };
        SetupUserClaims(claims);
        var routeValues = new Dictionary<string, object> { { "teamId", routeTeamId } };
        var context = CreateAuthorizationContext(routeValues);

        // Act
        attribute.OnAuthorization(context);

        // Assert
        Assert.Null(context.Result);
    }

    [Fact]
    public void OnAuthorization_WhenNoTeamIdInRoute_ShouldAllowAccess()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var attribute = new RequireTeamAccessAttribute(TeamRole.TeamMember, requireTeamIdFromRoute: true);
        var claims = new List<Claim>
        {
            new("team_id", teamId.ToString()),
            new("team_role", TeamRole.TeamMember.ToString())
        };
        SetupUserClaims(claims);
        var context = CreateAuthorizationContext();

        // Act
        attribute.OnAuthorization(context);

        // Assert
        Assert.Null(context.Result);
    }

    #endregion

    #region Role Hierarchy Tests

    [Theory]
    [InlineData(TeamRole.TeamOwner, TeamRole.TeamOwner, true)]
    [InlineData(TeamRole.TeamOwner, TeamRole.TeamAdmin, true)]
    [InlineData(TeamRole.TeamOwner, TeamRole.TeamMember, true)]
    [InlineData(TeamRole.TeamAdmin, TeamRole.TeamOwner, false)]
    [InlineData(TeamRole.TeamAdmin, TeamRole.TeamAdmin, true)]
    [InlineData(TeamRole.TeamAdmin, TeamRole.TeamMember, true)]
    [InlineData(TeamRole.TeamMember, TeamRole.TeamOwner, false)]
    [InlineData(TeamRole.TeamMember, TeamRole.TeamAdmin, false)]
    [InlineData(TeamRole.TeamMember, TeamRole.TeamMember, true)]
    public void OnAuthorization_RoleHierarchy_ShouldRespectPermissions(TeamRole userRole, TeamRole requiredRole, bool shouldAllow)
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var attribute = new RequireTeamAccessAttribute(requiredRole, requireTeamIdFromRoute: false);
        var claims = new List<Claim>
        {
            new("team_id", teamId.ToString()),
            new("team_role", userRole.ToString())
        };
        SetupUserClaims(claims);
        var context = CreateAuthorizationContext();

        // Act
        attribute.OnAuthorization(context);

        // Assert
        if (shouldAllow)
        {
            Assert.Null(context.Result);
        }
        else
        {
            Assert.NotNull(context.Result);
            var forbidResult = Assert.IsType<ForbidResult>(context.Result);
            Assert.Contains($"Minimum required role is {requiredRole}, but user has {userRole}", forbidResult.AuthenticationSchemes.FirstOrDefault() ?? "");
        }
    }

    #endregion

    #region Route Parameter Tests

    [Theory]
    [InlineData("teamId")]
    [InlineData("id")]
    public void OnAuthorization_ShouldRecognizeCommonRouteParameterNames(string parameterName)
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var attribute = new RequireTeamAccessAttribute(TeamRole.TeamMember, requireTeamIdFromRoute: true);
        var claims = new List<Claim>
        {
            new("team_id", teamId.ToString()),
            new("team_role", TeamRole.TeamMember.ToString())
        };
        SetupUserClaims(claims);
        var routeValues = new Dictionary<string, object> { { parameterName, teamId } };
        var context = CreateAuthorizationContext(routeValues);

        // Act
        attribute.OnAuthorization(context);

        // Assert
        Assert.Null(context.Result);
    }

    #endregion

    #region Default Parameter Tests

    [Fact]
    public void Constructor_WithDefaultParameters_ShouldSetCorrectDefaults()
    {
        // Arrange & Act
        var attribute = new RequireTeamAccessAttribute();
        var teamId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new("team_id", teamId.ToString()),
            new("team_role", TeamRole.TeamMember.ToString())
        };
        SetupUserClaims(claims);
        var context = CreateAuthorizationContext();

        // Act
        attribute.OnAuthorization(context);

        // Assert - Should allow access with default TeamMember role requirement
        Assert.Null(context.Result);
    }

    [Fact]
    public void Constructor_WithCustomParameters_ShouldUseProvidedValues()
    {
        // Arrange & Act
        var attribute = new RequireTeamAccessAttribute(TeamRole.TeamAdmin, requireTeamIdFromRoute: false);
        var teamId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new("team_id", teamId.ToString()),
            new("team_role", TeamRole.TeamMember.ToString()) // Lower role than required
        };
        SetupUserClaims(claims);
        var context = CreateAuthorizationContext();

        // Act
        attribute.OnAuthorization(context);

        // Assert - Should deny access due to insufficient role
        Assert.NotNull(context.Result);
        Assert.IsType<ForbidResult>(context.Result);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void OnAuthorization_CompleteValidScenario_ShouldAllowAccess()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var attribute = new RequireTeamAccessAttribute(TeamRole.TeamAdmin, requireTeamIdFromRoute: true);
        var claims = new List<Claim>
        {
            new("team_id", teamId.ToString()),
            new("team_role", TeamRole.TeamOwner.ToString()),
            new("member_type", MemberType.Coach.ToString())
        };
        SetupUserClaims(claims);
        var routeValues = new Dictionary<string, object> { { "teamId", teamId } };
        var context = CreateAuthorizationContext(routeValues);

        // Act
        attribute.OnAuthorization(context);

        // Assert
        Assert.Null(context.Result);
    }

    [Fact]
    public void OnAuthorization_GlobalAdminWithoutTeamClaims_ShouldAllowAccess()
    {
        // Arrange
        var attribute = new RequireTeamAccessAttribute(TeamRole.TeamOwner, requireTeamIdFromRoute: true);
        var claims = new List<Claim>
        {
            new("is_global_admin", "true")
            // No team_id or team_role claims
        };
        SetupUserClaims(claims);
        var routeValues = new Dictionary<string, object> { { "teamId", Guid.NewGuid() } };
        var context = CreateAuthorizationContext(routeValues);

        // Act
        attribute.OnAuthorization(context);

        // Assert
        Assert.Null(context.Result);
    }

    #endregion
} 