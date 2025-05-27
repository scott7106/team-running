using Microsoft.AspNetCore.Http;
using Moq;
using Shouldly;
using System.Security.Claims;
using TeamStride.Infrastructure.Services;
using TeamStride.Domain.Entities;

namespace TeamStride.Infrastructure.Tests.Services;

public class CurrentUserServiceTests
{
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<ClaimsPrincipal> _mockUser;
    private readonly CurrentUserService _currentUserService;

    public CurrentUserServiceTests()
    {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockHttpContext = new Mock<HttpContext>();
        _mockUser = new Mock<ClaimsPrincipal>();
        _currentUserService = new CurrentUserService(_mockHttpContextAccessor.Object);
    }

    #region UserId Tests

    [Fact]
    public void UserId_WhenHttpContextIsNull_ReturnsNull()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _currentUserService.UserId;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void UserId_WhenUserIsNull_ReturnsNull()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns((ClaimsPrincipal?)null!);

        // Act
        var result = _currentUserService.UserId;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void UserId_WhenNameIdentifierClaimIsNull_ReturnsNull()
    {
        // Arrange
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.UserId;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void UserId_WhenNameIdentifierClaimExists_ReturnsCorrectGuid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.UserId;

        // Assert
        result.ShouldBe(userId);
    }

    [Fact]
    public void UserId_WhenNameIdentifierClaimIsEmpty_ThrowsFormatException()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, string.Empty)
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act & Assert
        Should.Throw<FormatException>(() => _currentUserService.UserId);
    }

    [Fact]
    public void UserId_WhenNameIdentifierClaimIsInvalidGuid_ThrowsFormatException()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "invalid-guid")
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act & Assert
        Should.Throw<FormatException>(() => _currentUserService.UserId);
    }

    [Fact]
    public void UserId_WhenMultipleNameIdentifierClaims_ReturnsFirstOne()
    {
        // Arrange
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, firstUserId.ToString()),
            new(ClaimTypes.NameIdentifier, secondUserId.ToString())
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.UserId;

        // Assert
        result.ShouldBe(firstUserId);
    }

    #endregion

    #region UserEmail Tests

    [Fact]
    public void UserEmail_WhenHttpContextIsNull_ReturnsNull()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _currentUserService.UserEmail;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void UserEmail_WhenUserIsNull_ReturnsNull()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns((ClaimsPrincipal?)null!);

        // Act
        var result = _currentUserService.UserEmail;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void UserEmail_WhenEmailClaimIsNull_ReturnsNull()
    {
        // Arrange
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.UserEmail;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void UserEmail_WhenEmailClaimExists_ReturnsCorrectEmail()
    {
        // Arrange
        var email = "test@example.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.UserEmail;

        // Assert
        result.ShouldBe(email);
    }

    [Fact]
    public void UserEmail_WhenEmailClaimIsEmpty_ReturnsEmptyString()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, string.Empty)
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.UserEmail;

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void UserEmail_WhenMultipleEmailClaims_ReturnsFirstOne()
    {
        // Arrange
        var firstEmail = "first@example.com";
        var secondEmail = "second@example.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, firstEmail),
            new(ClaimTypes.Email, secondEmail)
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.UserEmail;

        // Assert
        result.ShouldBe(firstEmail);
    }

    #endregion

    #region IsAuthenticated Tests

    [Fact]
    public void IsAuthenticated_WhenHttpContextIsNull_ReturnsFalse()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _currentUserService.IsAuthenticated;

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsAuthenticated_WhenUserIsNull_ReturnsFalse()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns((ClaimsPrincipal?)null!);

        // Act
        var result = _currentUserService.IsAuthenticated;

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsAuthenticated_WhenIdentityIsNull_ReturnsFalse()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(_mockUser.Object);
        _mockUser.Setup(x => x.Identity).Returns((ClaimsIdentity?)null!);

        // Act
        var result = _currentUserService.IsAuthenticated;

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsAuthenticated_WhenUserIsAuthenticated_ReturnsTrue()
    {
        // Arrange
        var identity = new ClaimsIdentity("test");
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.IsAuthenticated;

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsAuthenticated_WhenUserIsNotAuthenticated_ReturnsFalse()
    {
        // Arrange
        var identity = new ClaimsIdentity(); // No authentication type = not authenticated
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.IsAuthenticated;

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsGlobalAdmin Tests

    [Fact]
    public void IsGlobalAdmin_WhenHttpContextIsNull_ReturnsFalse()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _currentUserService.IsGlobalAdmin;

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsGlobalAdmin_WhenClaimIsNull_ReturnsFalse()
    {
        // Arrange
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.IsGlobalAdmin;

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsGlobalAdmin_WhenClaimIsTrue_ReturnsTrue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("is_global_admin", "true")
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.IsGlobalAdmin;

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsGlobalAdmin_WhenClaimIsFalse_ReturnsFalse()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("is_global_admin", "false")
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.IsGlobalAdmin;

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsGlobalAdmin_WhenClaimIsInvalid_ReturnsFalse()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("is_global_admin", "invalid")
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.IsGlobalAdmin;

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region TeamId Tests

    [Fact]
    public void TeamId_WhenHttpContextIsNull_ReturnsNull()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _currentUserService.TeamId;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void TeamId_WhenClaimIsNull_ReturnsNull()
    {
        // Arrange
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.TeamId;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void TeamId_WhenClaimIsValidGuid_ReturnsCorrectGuid()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new("team_id", teamId.ToString())
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.TeamId;

        // Assert
        result.ShouldBe(teamId);
    }

    [Fact]
    public void TeamId_WhenClaimIsInvalidGuid_ReturnsNull()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("team_id", "invalid-guid")
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.TeamId;

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region TeamRole Tests

    [Fact]
    public void TeamRole_WhenHttpContextIsNull_ReturnsNull()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _currentUserService.TeamRole;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void TeamRole_WhenClaimIsNull_ReturnsNull()
    {
        // Arrange
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.TeamRole;

        // Assert
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData("TeamOwner", TeamRole.TeamOwner)]
    [InlineData("TeamAdmin", TeamRole.TeamAdmin)]
    [InlineData("TeamMember", TeamRole.TeamMember)]
    public void TeamRole_WhenClaimIsValidRole_ReturnsCorrectRole(string claimValue, TeamRole expectedRole)
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("team_role", claimValue)
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.TeamRole;

        // Assert
        result.ShouldBe(expectedRole);
    }

    [Fact]
    public void TeamRole_WhenClaimIsInvalidRole_ReturnsNull()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("team_role", "InvalidRole")
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.TeamRole;

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region MemberType Tests

    [Fact]
    public void MemberType_WhenHttpContextIsNull_ReturnsNull()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _currentUserService.MemberType;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void MemberType_WhenClaimIsNull_ReturnsNull()
    {
        // Arrange
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.MemberType;

        // Assert
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData("Coach", MemberType.Coach)]
    [InlineData("Athlete", MemberType.Athlete)]
    [InlineData("Parent", MemberType.Parent)]
    public void MemberType_WhenClaimIsValidType_ReturnsCorrectType(string claimValue, MemberType expectedType)
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("member_type", claimValue)
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.MemberType;

        // Assert
        result.ShouldBe(expectedType);
    }

    [Fact]
    public void MemberType_WhenClaimIsInvalidType_ReturnsNull()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("member_type", "InvalidType")
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.MemberType;

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region Helper Methods Tests

    [Theory]
    [InlineData("TeamOwner", true)]
    [InlineData("TeamAdmin", false)]
    [InlineData("TeamMember", false)]
    public void IsTeamOwner_ReturnsCorrectValue(string roleValue, bool expected)
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("team_role", roleValue)
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.IsTeamOwner;

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("TeamOwner", false)]
    [InlineData("TeamAdmin", true)]
    [InlineData("TeamMember", false)]
    public void IsTeamAdmin_ReturnsCorrectValue(string roleValue, bool expected)
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("team_role", roleValue)
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.IsTeamAdmin;

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("TeamOwner", false)]
    [InlineData("TeamAdmin", false)]
    [InlineData("TeamMember", true)]
    public void IsTeamMember_ReturnsCorrectValue(string roleValue, bool expected)
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("team_role", roleValue)
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.IsTeamMember;

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void CanAccessTeam_WhenGlobalAdmin_ReturnsTrue()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new("is_global_admin", "true")
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.CanAccessTeam(teamId);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanAccessTeam_WhenUserTeamMatches_ReturnsTrue()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new("is_global_admin", "false"),
            new("team_id", teamId.ToString())
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.CanAccessTeam(teamId);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanAccessTeam_WhenUserTeamDoesNotMatch_ReturnsFalse()
    {
        // Arrange
        var userTeamId = Guid.NewGuid();
        var requestedTeamId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new("is_global_admin", "false"),
            new("team_id", userTeamId.ToString())
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.CanAccessTeam(requestedTeamId);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void CanAccessTeam_WhenNoTeamId_ReturnsFalse()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new("is_global_admin", "false")
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.CanAccessTeam(teamId);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void HasMinimumTeamRole_WhenGlobalAdmin_ReturnsTrue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("is_global_admin", "true")
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.HasMinimumTeamRole(TeamRole.TeamOwner);

        // Assert
        result.ShouldBeTrue();
    }

    [Theory]
    [InlineData("TeamOwner", TeamRole.TeamOwner, true)]
    [InlineData("TeamOwner", TeamRole.TeamAdmin, true)]
    [InlineData("TeamOwner", TeamRole.TeamMember, true)]
    [InlineData("TeamAdmin", TeamRole.TeamOwner, false)]
    [InlineData("TeamAdmin", TeamRole.TeamAdmin, true)]
    [InlineData("TeamAdmin", TeamRole.TeamMember, true)]
    [InlineData("TeamMember", TeamRole.TeamOwner, false)]
    [InlineData("TeamMember", TeamRole.TeamAdmin, false)]
    [InlineData("TeamMember", TeamRole.TeamMember, true)]
    public void HasMinimumTeamRole_WithRoleHierarchy_ReturnsCorrectValue(string userRole, TeamRole minimumRole, bool expected)
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("is_global_admin", "false"),
            new("team_role", userRole)
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.HasMinimumTeamRole(minimumRole);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void HasMinimumTeamRole_WhenNoTeamRole_ReturnsFalse()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new("is_global_admin", "false")
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.HasMinimumTeamRole(TeamRole.TeamMember);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void CurrentUserService_WithCompleteUserClaims_ReturnsAllPropertiesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var email = "test@example.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new("is_global_admin", "false"),
            new("team_id", teamId.ToString()),
            new("team_role", "TeamOwner"),
            new("member_type", "Coach")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act & Assert
        _currentUserService.UserId.ShouldBe(userId);
        _currentUserService.UserEmail.ShouldBe(email);
        _currentUserService.IsAuthenticated.ShouldBeTrue();
        _currentUserService.IsGlobalAdmin.ShouldBeFalse();
        _currentUserService.TeamId.ShouldBe(teamId);
        _currentUserService.TeamRole.ShouldBe(TeamRole.TeamOwner);
        _currentUserService.MemberType.ShouldBe(MemberType.Coach);
        _currentUserService.IsTeamOwner.ShouldBeTrue();
        _currentUserService.IsTeamAdmin.ShouldBeFalse();
        _currentUserService.IsTeamMember.ShouldBeFalse();
        _currentUserService.CanAccessTeam(teamId).ShouldBeTrue();
        _currentUserService.HasMinimumTeamRole(TeamRole.TeamMember).ShouldBeTrue();
    }

    [Fact]
    public void CurrentUserService_WithGlobalAdminClaims_ReturnsCorrectValues()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "admin@example.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new("is_global_admin", "true")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act & Assert
        _currentUserService.UserId.ShouldBe(userId);
        _currentUserService.UserEmail.ShouldBe(email);
        _currentUserService.IsAuthenticated.ShouldBeTrue();
        _currentUserService.IsGlobalAdmin.ShouldBeTrue();
        _currentUserService.TeamId.ShouldBeNull();
        _currentUserService.TeamRole.ShouldBeNull();
        _currentUserService.MemberType.ShouldBeNull();
        _currentUserService.IsTeamOwner.ShouldBeFalse();
        _currentUserService.IsTeamAdmin.ShouldBeFalse();
        _currentUserService.IsTeamMember.ShouldBeFalse();
        _currentUserService.CanAccessTeam(Guid.NewGuid()).ShouldBeTrue(); // Global admin can access any team
        _currentUserService.HasMinimumTeamRole(TeamRole.TeamOwner).ShouldBeTrue(); // Global admin bypasses role checks
    }

    [Fact]
    public void CurrentUserService_WithAnonymousUser_ReturnsCorrectValues()
    {
        // Arrange
        var identity = new ClaimsIdentity(); // No authentication type = not authenticated
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act & Assert
        _currentUserService.UserId.ShouldBeNull();
        _currentUserService.UserEmail.ShouldBeNull();
        _currentUserService.IsAuthenticated.ShouldBeFalse();
        _currentUserService.IsGlobalAdmin.ShouldBeFalse();
        _currentUserService.TeamId.ShouldBeNull();
        _currentUserService.TeamRole.ShouldBeNull();
        _currentUserService.MemberType.ShouldBeNull();
        _currentUserService.IsTeamOwner.ShouldBeFalse();
        _currentUserService.IsTeamAdmin.ShouldBeFalse();
        _currentUserService.IsTeamMember.ShouldBeFalse();
        _currentUserService.CanAccessTeam(Guid.NewGuid()).ShouldBeFalse();
        _currentUserService.HasMinimumTeamRole(TeamRole.TeamMember).ShouldBeFalse();
    }

    [Fact]
    public void CurrentUserService_PropertiesAreConsistent_AcrossMultipleCalls()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var email = "test@example.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new("is_global_admin", "false"),
            new("team_id", teamId.ToString()),
            new("team_role", "TeamAdmin"),
            new("member_type", "Coach")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act & Assert - Multiple calls should return consistent values
        for (int i = 0; i < 3; i++)
        {
            _currentUserService.UserId.ShouldBe(userId);
            _currentUserService.UserEmail.ShouldBe(email);
            _currentUserService.IsAuthenticated.ShouldBeTrue();
            _currentUserService.IsGlobalAdmin.ShouldBeFalse();
            _currentUserService.TeamId.ShouldBe(teamId);
            _currentUserService.TeamRole.ShouldBe(TeamRole.TeamAdmin);
            _currentUserService.MemberType.ShouldBe(MemberType.Coach);
            _currentUserService.IsTeamOwner.ShouldBeFalse();
            _currentUserService.IsTeamAdmin.ShouldBeTrue();
            _currentUserService.IsTeamMember.ShouldBeFalse();
            _currentUserService.CanAccessTeam(teamId).ShouldBeTrue();
            _currentUserService.HasMinimumTeamRole(TeamRole.TeamMember).ShouldBeTrue();
        }
    }

    #endregion
} 