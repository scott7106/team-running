using Microsoft.AspNetCore.Http;
using Moq;
using Shouldly;
using System.Security.Claims;
using TeamStride.Infrastructure.Services;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Interfaces;

namespace TeamStride.Infrastructure.Tests.Services;

public class CurrentUserServiceTests
{
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<ICurrentTeamService> _mockCurrentTeamService;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<ClaimsPrincipal> _mockUser;
    private readonly CurrentUserService _currentUserService;

    public CurrentUserServiceTests()
    {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockCurrentTeamService = new Mock<ICurrentTeamService>();
        _mockHttpContext = new Mock<HttpContext>();
        _mockUser = new Mock<ClaimsPrincipal>();
        _currentUserService = new CurrentUserService(_mockHttpContextAccessor.Object, _mockCurrentTeamService.Object);
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
    public void CurrentTeamId_WhenCurrentTeamServiceReturnsNull_ReturnsNull()
    {
        // Arrange
        _mockCurrentTeamService.Setup(x => x.IsTeamSet).Returns(false);

        // Act
        var result = _currentUserService.CurrentTeamId;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void TeamId_WhenCurrentTeamServiceReturnsTeamId_ReturnsCorrectGuid()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _mockCurrentTeamService.Setup(x => x.IsTeamSet).Returns(true);
        _mockCurrentTeamService.Setup(x => x.TeamId).Returns(teamId);

        // Act
        var result = _currentUserService.CurrentTeamId;

        // Assert
        result.ShouldBe(teamId);
    }

    #endregion

    #region TeamRole Tests

    [Theory]
    [InlineData(TeamRole.TeamOwner)]
    [InlineData(TeamRole.TeamAdmin)]
    [InlineData(TeamRole.TeamMember)]
    public void TeamRole_WhenCurrentTeamServiceReturnsRole_ReturnsCorrectRole(TeamRole expectedRole)
    {
        // Arrange
        _mockCurrentTeamService.Setup(x => x.CurrentTeamRole).Returns(expectedRole);

        // Act
        var result = _currentUserService.CurrentTeamRole;

        // Assert
        result.ShouldBe(expectedRole);
    }

    #endregion

    #region MemberType Tests

    [Theory]
    [InlineData(MemberType.Coach)]
    [InlineData(MemberType.Athlete)]
    [InlineData(MemberType.Parent)]
    public void MemberType_WhenCurrentTeamServiceReturnsMemberType_ReturnsCorrectType(MemberType expectedType)
    {
        // Arrange
        _mockCurrentTeamService.Setup(x => x.CurrentMemberType).Returns(expectedType);

        // Act
        var result = _currentUserService.CurrentMemberType;

        // Assert
        result.ShouldBe(expectedType);
    }

    #endregion

    #region Helper Methods Tests

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsTeamOwner_ReturnsCorrectValue(bool expected)
    {
        // Arrange
        _mockCurrentTeamService.Setup(x => x.IsTeamOwner).Returns(expected);

        // Act
        var result = _currentUserService.IsTeamOwner;

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsTeamAdmin_ReturnsCorrectValue(bool expected)
    {
        // Arrange
        _mockCurrentTeamService.Setup(x => x.IsTeamAdmin).Returns(expected);

        // Act
        var result = _currentUserService.IsTeamAdmin;

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsTeamMember_ReturnsCorrectValue(bool expected)
    {
        // Arrange
        _mockCurrentTeamService.Setup(x => x.IsTeamMember).Returns(expected);

        // Act
        var result = _currentUserService.IsTeamMember;

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void CanAccessTeam_DelegatesToCurrentTeamService()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var expectedResult = true;
        _mockCurrentTeamService.Setup(x => x.CanAccessTeam(teamId)).Returns(expectedResult);

        // Act
        var result = _currentUserService.CanAccessTeam(teamId);

        // Assert
        result.ShouldBe(expectedResult);
        _mockCurrentTeamService.Verify(x => x.CanAccessTeam(teamId), Times.Once);
    }

    [Fact]
    public void HasMinimumTeamRole_DelegatesToCurrentTeamService()
    {
        // Arrange
        var minimumRole = TeamRole.TeamAdmin;
        var expectedResult = true;
        _mockCurrentTeamService.Setup(x => x.HasMinimumTeamRole(minimumRole)).Returns(expectedResult);

        // Act
        var result = _currentUserService.HasMinimumTeamRole(minimumRole);

        // Assert
        result.ShouldBe(expectedResult);
        _mockCurrentTeamService.Verify(x => x.HasMinimumTeamRole(minimumRole), Times.Once);
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
        
        // Setup ICurrentTeamService mocks for delegation
        _mockCurrentTeamService.Setup(x => x.IsTeamSet).Returns(true);
        _mockCurrentTeamService.Setup(x => x.TeamId).Returns(teamId);
        _mockCurrentTeamService.Setup(x => x.CurrentTeamRole).Returns(TeamRole.TeamOwner);
        _mockCurrentTeamService.Setup(x => x.CurrentMemberType).Returns(MemberType.Coach);
        _mockCurrentTeamService.Setup(x => x.IsTeamOwner).Returns(true);
        _mockCurrentTeamService.Setup(x => x.IsTeamAdmin).Returns(false);
        _mockCurrentTeamService.Setup(x => x.IsTeamMember).Returns(false);
        _mockCurrentTeamService.Setup(x => x.CanAccessTeam(teamId)).Returns(true);
        _mockCurrentTeamService.Setup(x => x.HasMinimumTeamRole(TeamRole.TeamMember)).Returns(true);

        // Act & Assert
        _currentUserService.UserId.ShouldBe(userId);
        _currentUserService.UserEmail.ShouldBe(email);
        _currentUserService.IsAuthenticated.ShouldBeTrue();
        _currentUserService.IsGlobalAdmin.ShouldBeFalse();
        _currentUserService.CurrentTeamId.ShouldBe(teamId);
        _currentUserService.CurrentTeamRole.ShouldBe(TeamRole.TeamOwner);
        _currentUserService.CurrentMemberType.ShouldBe(MemberType.Coach);
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
        var testTeamId = Guid.NewGuid();
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
        
        // Setup ICurrentTeamService mocks for global admin (should return default values since no team context)
        _mockCurrentTeamService.Setup(x => x.IsTeamSet).Returns(false);
        _mockCurrentTeamService.Setup(x => x.CurrentTeamRole).Returns((TeamRole?)null);
        _mockCurrentTeamService.Setup(x => x.CurrentMemberType).Returns((MemberType?)null);
        _mockCurrentTeamService.Setup(x => x.IsTeamOwner).Returns(false);
        _mockCurrentTeamService.Setup(x => x.IsTeamAdmin).Returns(false);
        _mockCurrentTeamService.Setup(x => x.IsTeamMember).Returns(false);
        _mockCurrentTeamService.Setup(x => x.CanAccessTeam(testTeamId)).Returns(true);
        _mockCurrentTeamService.Setup(x => x.HasMinimumTeamRole(TeamRole.TeamOwner)).Returns(true);

        // Act & Assert
        _currentUserService.UserId.ShouldBe(userId);
        _currentUserService.UserEmail.ShouldBe(email);
        _currentUserService.IsAuthenticated.ShouldBeTrue();
        _currentUserService.IsGlobalAdmin.ShouldBeTrue();
        _currentUserService.CurrentTeamId.ShouldBeNull();
        _currentUserService.CurrentTeamRole.ShouldBeNull();
        _currentUserService.CurrentMemberType.ShouldBeNull();
        _currentUserService.IsTeamOwner.ShouldBeFalse();
        _currentUserService.IsTeamAdmin.ShouldBeFalse();
        _currentUserService.IsTeamMember.ShouldBeFalse();
        _currentUserService.CanAccessTeam(testTeamId).ShouldBeTrue(); // Global admin can access any team
        _currentUserService.HasMinimumTeamRole(TeamRole.TeamOwner).ShouldBeTrue(); // Global admin bypasses role checks
    }

    [Fact]
    public void CurrentUserService_WithAnonymousUser_ReturnsCorrectValues()
    {
        // Arrange
        var testTeamId = Guid.NewGuid();
        var identity = new ClaimsIdentity(); // No authentication type = not authenticated
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);
        
        // Setup ICurrentTeamService mocks for anonymous user (should return false/null)
        _mockCurrentTeamService.Setup(x => x.IsTeamSet).Returns(false);
        _mockCurrentTeamService.Setup(x => x.CurrentTeamRole).Returns((TeamRole?)null);
        _mockCurrentTeamService.Setup(x => x.CurrentMemberType).Returns((MemberType?)null);
        _mockCurrentTeamService.Setup(x => x.IsTeamOwner).Returns(false);
        _mockCurrentTeamService.Setup(x => x.IsTeamAdmin).Returns(false);
        _mockCurrentTeamService.Setup(x => x.IsTeamMember).Returns(false);
        _mockCurrentTeamService.Setup(x => x.CanAccessTeam(testTeamId)).Returns(false);
        _mockCurrentTeamService.Setup(x => x.HasMinimumTeamRole(TeamRole.TeamMember)).Returns(false);

        // Act & Assert
        _currentUserService.UserId.ShouldBeNull();
        _currentUserService.UserEmail.ShouldBeNull();
        _currentUserService.IsAuthenticated.ShouldBeFalse();
        _currentUserService.IsGlobalAdmin.ShouldBeFalse();
        _currentUserService.CurrentTeamId.ShouldBeNull();
        _currentUserService.CurrentTeamRole.ShouldBeNull();
        _currentUserService.CurrentMemberType.ShouldBeNull();
        _currentUserService.IsTeamOwner.ShouldBeFalse();
        _currentUserService.IsTeamAdmin.ShouldBeFalse();
        _currentUserService.IsTeamMember.ShouldBeFalse();
        _currentUserService.CanAccessTeam(testTeamId).ShouldBeFalse();
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
        
        // Setup ICurrentTeamService mocks for team admin
        _mockCurrentTeamService.Setup(x => x.IsTeamSet).Returns(true);
        _mockCurrentTeamService.Setup(x => x.TeamId).Returns(teamId);
        _mockCurrentTeamService.Setup(x => x.CurrentTeamRole).Returns(TeamRole.TeamAdmin);
        _mockCurrentTeamService.Setup(x => x.CurrentMemberType).Returns(MemberType.Coach);
        _mockCurrentTeamService.Setup(x => x.IsTeamOwner).Returns(false);
        _mockCurrentTeamService.Setup(x => x.IsTeamAdmin).Returns(true);
        _mockCurrentTeamService.Setup(x => x.IsTeamMember).Returns(false);
        _mockCurrentTeamService.Setup(x => x.CanAccessTeam(teamId)).Returns(true);
        _mockCurrentTeamService.Setup(x => x.HasMinimumTeamRole(TeamRole.TeamMember)).Returns(true);

        // Act & Assert - Multiple calls should return consistent values
        for (int i = 0; i < 3; i++)
        {
            _currentUserService.UserId.ShouldBe(userId);
            _currentUserService.UserEmail.ShouldBe(email);
            _currentUserService.IsAuthenticated.ShouldBeTrue();
            _currentUserService.IsGlobalAdmin.ShouldBeFalse();
            _currentUserService.CurrentTeamId.ShouldBe(teamId);
            _currentUserService.CurrentTeamRole.ShouldBe(TeamRole.TeamAdmin);
            _currentUserService.CurrentMemberType.ShouldBe(MemberType.Coach);
            _currentUserService.IsTeamOwner.ShouldBeFalse();
            _currentUserService.IsTeamAdmin.ShouldBeTrue();
            _currentUserService.IsTeamMember.ShouldBeFalse();
            _currentUserService.CanAccessTeam(teamId).ShouldBeTrue();
            _currentUserService.HasMinimumTeamRole(TeamRole.TeamMember).ShouldBeTrue();
        }
    }

    #endregion
} 