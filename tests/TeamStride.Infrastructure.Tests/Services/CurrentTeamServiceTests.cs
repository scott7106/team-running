using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System.Security.Claims;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Application.Teams.Services;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Interfaces;
using TeamStride.Infrastructure.Services;

namespace TeamStride.Infrastructure.Tests.Services;

public class CurrentTeamServiceTests
{
    private readonly Mock<ILogger<CurrentTeamService>> _mockLogger;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<ClaimsPrincipal> _mockUser;
    private readonly Mock<IStandardTeamService> _mockStandardTeamService;
    private readonly CurrentTeamService _currentTeamService;

    public CurrentTeamServiceTests()
    {
        _mockLogger = new Mock<ILogger<CurrentTeamService>>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockHttpContext = new Mock<HttpContext>();
        _mockUser = new Mock<ClaimsPrincipal>();
        _mockStandardTeamService = new Mock<IStandardTeamService>();
        
        _currentTeamService = new CurrentTeamService(
            _mockLogger.Object,
            _mockHttpContextAccessor.Object,
            _mockServiceProvider.Object);
    }

    private void SetupHttpContext(List<Claim>? claims = null)
    {
        claims ??= new List<Claim>();
        var identity = new ClaimsIdentity(claims, "test");
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);
    }

    #region IsTeamSet Tests

    [Fact]
    public void IsTeamSet_WhenNotSet_ReturnsFalse()
    {
        // Act
        var result = _currentTeamService.IsTeamSet;

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsTeamSet_WhenSet_ReturnsTrue()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _currentTeamService.SetTeamId(teamId);

        // Act
        var result = _currentTeamService.IsTeamSet;

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsTeamSet_AfterClear_ReturnsFalse()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _currentTeamService.SetTeamId(teamId);
        _currentTeamService.ClearTeam();

        // Act
        var result = _currentTeamService.IsTeamSet;

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region TeamId Tests

    [Fact]
    public void TeamId_WhenNotSet_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => _currentTeamService.TeamId);
        exception.Message.ShouldBe("Current team is not set");
    }

    [Fact]
    public void TeamId_WhenSetWithGuid_ReturnsCorrectValue()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _currentTeamService.SetTeamId(teamId);

        // Act
        var result = _currentTeamService.TeamId;

        // Assert
        result.ShouldBe(teamId);
    }

    [Fact]
    public void TeamId_AfterClear_ThrowsInvalidOperationException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _currentTeamService.SetTeamId(teamId);
        _currentTeamService.ClearTeam();

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => _currentTeamService.TeamId);
        exception.Message.ShouldBe("Current team is not set");
    }

    #endregion

    #region GetSubdomain Tests

    [Fact]
    public void GetSubdomain_WhenNotSet_ReturnsNull()
    {
        // Act
        var result = _currentTeamService.GetSubdomain;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetSubdomain_WhenSetWithSubdomain_ReturnsCorrectValue()
    {
        // Arrange
        var subdomain = "test-team";
        _currentTeamService.SetTeamSubdomain(subdomain);

        // Act
        var result = _currentTeamService.GetSubdomain;

        // Assert
        result.ShouldBe(subdomain);
    }

    [Fact]
    public void GetSubdomain_AfterClear_ReturnsNull()
    {
        // Arrange
        var subdomain = "test-team";
        _currentTeamService.SetTeamSubdomain(subdomain);
        _currentTeamService.ClearTeam();

        // Act
        var result = _currentTeamService.GetSubdomain;

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region SetTeamId Tests

    [Fact]
    public void SetTeamId_WithValidGuid_SetsTeamIdAndLogsInformation()
    {
        // Arrange
        var teamId = Guid.NewGuid();

        // Act
        _currentTeamService.SetTeamId(teamId);

        // Assert
        _currentTeamService.TeamId.ShouldBe(teamId);
        
        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Current team set to {teamId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void SetTeamId_WithEmptyGuid_SetsTeamIdToEmpty()
    {
        // Arrange
        var teamId = Guid.Empty;

        // Act
        _currentTeamService.SetTeamId(teamId);

        // Assert
        _currentTeamService.TeamId.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void SetTeamId_WithGuid_OverwritesPreviousValue()
    {
        // Arrange
        var firstTeamId = Guid.NewGuid();
        var secondTeamId = Guid.NewGuid();
        _currentTeamService.SetTeamId(firstTeamId);

        // Act
        _currentTeamService.SetTeamId(secondTeamId);

        // Assert
        _currentTeamService.TeamId.ShouldBe(secondTeamId);
    }

    #endregion

    #region SetTeamSubdomain Tests

    [Fact]
    public void SetTeamSubdomain_WithValidSubdomain_SetsSubdomainAndLogsInformation()
    {
        // Arrange
        var subdomain = "test-team";

        // Act
        _currentTeamService.SetTeamSubdomain(subdomain);

        // Assert
        _currentTeamService.GetSubdomain.ShouldBe(subdomain);
        
        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Current team subdomain set to {subdomain}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void SetTeamSubdomain_WithEmptyString_SetsSubdomainToEmpty()
    {
        // Arrange
        var subdomain = string.Empty;

        // Act
        _currentTeamService.SetTeamSubdomain(subdomain);

        // Assert
        _currentTeamService.GetSubdomain.ShouldBe(string.Empty);
    }

    [Fact]
    public void SetTeamSubdomain_WithSubdomain_OverwritesPreviousValue()
    {
        // Arrange
        var firstSubdomain = "first-team";
        var secondSubdomain = "second-team";
        _currentTeamService.SetTeamSubdomain(firstSubdomain);

        // Act
        _currentTeamService.SetTeamSubdomain(secondSubdomain);

        // Assert
        _currentTeamService.GetSubdomain.ShouldBe(secondSubdomain);
    }

    #endregion

    #region ClearTeam Tests

    [Fact]
    public void ClearTeam_WhenCalled_ClearsAllValuesAndLogsInformation()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var subdomain = "test-team";
        _currentTeamService.SetTeamId(teamId);
        _currentTeamService.SetTeamSubdomain(subdomain);

        // Act
        _currentTeamService.ClearTeam();

        // Assert
        Should.Throw<InvalidOperationException>(() => _currentTeamService.TeamId);
        _currentTeamService.GetSubdomain.ShouldBeNull();
        
        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Current team cleared")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region TeamRole and MemberType Properties Tests

    [Fact]
    public void TeamRole_WhenHttpContextIsNull_ReturnsNull()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _currentTeamService.TeamRole;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void TeamRole_WhenClaimDoesNotExist_ReturnsNull()
    {
        // Arrange
        SetupHttpContext();

        // Act
        var result = _currentTeamService.TeamRole;

        // Assert
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData("TeamOwner", TeamRole.TeamOwner)]
    [InlineData("TeamAdmin", TeamRole.TeamAdmin)]
    [InlineData("TeamMember", TeamRole.TeamMember)]
    public void TeamRole_WhenValidClaim_ReturnsCorrectRole(string claimValue, TeamRole expectedRole)
    {
        // Arrange
        var claims = new List<Claim> { new("team_role", claimValue) };
        SetupHttpContext(claims);

        // Act
        var result = _currentTeamService.TeamRole;

        // Assert
        result.ShouldBe(expectedRole);
    }

    [Fact]
    public void TeamRole_WhenInvalidClaim_ReturnsNull()
    {
        // Arrange
        var claims = new List<Claim> { new("team_role", "InvalidRole") };
        SetupHttpContext(claims);

        // Act
        var result = _currentTeamService.TeamRole;

        // Assert
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData("Coach", MemberType.Coach)]
    [InlineData("Athlete", MemberType.Athlete)]
    [InlineData("Parent", MemberType.Parent)]
    public void MemberType_WhenValidClaim_ReturnsCorrectType(string claimValue, MemberType expectedType)
    {
        // Arrange
        var claims = new List<Claim> { new("member_type", claimValue) };
        SetupHttpContext(claims);

        // Act
        var result = _currentTeamService.MemberType;

        // Assert
        result.ShouldBe(expectedType);
    }

    [Fact]
    public void MemberType_WhenInvalidClaim_ReturnsNull()
    {
        // Arrange
        var claims = new List<Claim> { new("member_type", "InvalidType") };
        SetupHttpContext(claims);

        // Act
        var result = _currentTeamService.MemberType;

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region Helper Properties Tests

    [Theory]
    [InlineData("TeamOwner", true)]
    [InlineData("TeamAdmin", false)]
    [InlineData("TeamMember", false)]
    public void IsTeamOwner_ReturnsCorrectValue(string roleValue, bool expected)
    {
        // Arrange
        var claims = new List<Claim> { new("team_role", roleValue) };
        SetupHttpContext(claims);

        // Act
        var result = _currentTeamService.IsTeamOwner;

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
        var claims = new List<Claim> { new("team_role", roleValue) };
        SetupHttpContext(claims);

        // Act
        var result = _currentTeamService.IsTeamAdmin;

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
        var claims = new List<Claim> { new("team_role", roleValue) };
        SetupHttpContext(claims);

        // Act
        var result = _currentTeamService.IsTeamMember;

        // Assert
        result.ShouldBe(expected);
    }

    #endregion

    #region SetTeamFromSubdomainAsync Tests

    [Fact]
    public async Task SetTeamFromSubdomainAsync_WhenTeamServiceNotAvailable_ReturnsFalse()
    {
        // Arrange
        var subdomain = "test-team";
        _mockServiceProvider.Setup(x => x.GetService(typeof(IStandardTeamService))).Returns((object?)null);

        // Act
        var result = await _currentTeamService.SetTeamFromSubdomainAsync(subdomain);

        // Assert
        result.ShouldBeFalse();
        _currentTeamService.IsTeamSet.ShouldBeFalse();
    }

    [Fact]
    public async Task SetTeamFromSubdomainAsync_WhenTeamNotFound_ReturnsFalse()
    {
        // Arrange
        var subdomain = "nonexistent-team";
        _mockServiceProvider.Setup(x => x.GetService(typeof(IStandardTeamService))).Returns(_mockStandardTeamService.Object);
        _mockStandardTeamService.Setup(x => x.GetTeamBySubdomainAsync(subdomain))
            .ThrowsAsync(new InvalidOperationException($"Team with subdomain '{subdomain}' not found"));

        // Act
        var result = await _currentTeamService.SetTeamFromSubdomainAsync(subdomain);

        // Assert
        result.ShouldBeFalse();
        _currentTeamService.IsTeamSet.ShouldBeFalse();
    }

    [Fact]
    public async Task SetTeamFromSubdomainAsync_WhenTeamFound_ReturnsTrue()
    {
        // Arrange
        var subdomain = "test-team";
        var teamId = Guid.NewGuid();
        var teamDto = new TeamDto { Id = teamId, Subdomain = subdomain };
        
        _mockServiceProvider.Setup(x => x.GetService(typeof(IStandardTeamService))).Returns(_mockStandardTeamService.Object);
        _mockStandardTeamService.Setup(x => x.GetTeamBySubdomainAsync(subdomain)).ReturnsAsync(teamDto);

        // Act
        var result = await _currentTeamService.SetTeamFromSubdomainAsync(subdomain);

        // Assert
        result.ShouldBeTrue();
        _currentTeamService.IsTeamSet.ShouldBeTrue();
        _currentTeamService.TeamId.ShouldBe(teamId);
        _currentTeamService.GetSubdomain.ShouldBe(subdomain);
    }

    [Fact]
    public async Task SetTeamFromSubdomainAsync_WhenExceptionThrown_ReturnsFalse()
    {
        // Arrange
        var subdomain = "test-team";
        _mockServiceProvider.Setup(x => x.GetService(typeof(IStandardTeamService))).Returns(_mockStandardTeamService.Object);
        _mockStandardTeamService.Setup(x => x.GetTeamBySubdomainAsync(subdomain)).ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _currentTeamService.SetTeamFromSubdomainAsync(subdomain);

        // Assert
        result.ShouldBeFalse();
        _currentTeamService.IsTeamSet.ShouldBeFalse();
    }

    #endregion

    #region SetTeamFromJwtClaims Tests

    [Fact]
    public void SetTeamFromJwtClaims_WhenUserNotAuthenticated_ReturnsFalse()
    {
        // Arrange
        SetupHttpContext();

        // Act
        var result = _currentTeamService.SetTeamFromJwtClaims();

        // Assert
        result.ShouldBeFalse();
        _currentTeamService.IsTeamSet.ShouldBeFalse();
    }

    [Fact]
    public void SetTeamFromJwtClaims_WhenNoTeamIdClaim_ReturnsFalse()
    {
        // Arrange
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, "test");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentTeamService.SetTeamFromJwtClaims();

        // Assert
        result.ShouldBeFalse();
        _currentTeamService.IsTeamSet.ShouldBeFalse();
    }

    [Fact]
    public void SetTeamFromJwtClaims_WhenGlobalAdmin_ReturnsTrue()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new("team_id", teamId.ToString()),
            new("is_global_admin", "true")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentTeamService.SetTeamFromJwtClaims();

        // Assert
        result.ShouldBeTrue();
        _currentTeamService.IsTeamSet.ShouldBeTrue();
        _currentTeamService.TeamId.ShouldBe(teamId);
    }

    [Fact]
    public void SetTeamFromJwtClaims_WhenValidTeamAccess_ReturnsTrue()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new("team_id", teamId.ToString()),
            new("team_role", "TeamMember")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentTeamService.SetTeamFromJwtClaims();

        // Assert
        result.ShouldBeTrue();
        _currentTeamService.IsTeamSet.ShouldBeTrue();
        _currentTeamService.TeamId.ShouldBe(teamId);
    }

    #endregion

    #region Authorization Methods Tests

    [Fact]
    public void CanAccessCurrentTeam_WhenTeamNotSet_ReturnsFalse()
    {
        // Act
        var result = _currentTeamService.CanAccessCurrentTeam();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void CanAccessCurrentTeam_WhenGlobalAdmin_ReturnsTrue()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new("team_id", teamId.ToString()),
            new("is_global_admin", "true")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);
        _currentTeamService.SetTeamId(teamId);

        // Act
        var result = _currentTeamService.CanAccessCurrentTeam();

        // Assert
        result.ShouldBeTrue();
    }

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
    public void HasMinimumTeamRole_WithRoleHierarchy_ReturnsCorrectValue(TeamRole userRole, TeamRole minimumRole, bool expected)
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new("team_role", userRole.ToString())
        };
        var identity = new ClaimsIdentity(claims, "test");
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentTeamService.HasMinimumTeamRole(minimumRole);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void HasMinimumTeamRole_WhenGlobalAdmin_ReturnsTrue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new("is_global_admin", "true")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentTeamService.HasMinimumTeamRole(TeamRole.TeamOwner);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanAccessTeam_WhenTeamMatches_ReturnsTrue()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new("team_id", teamId.ToString()),
            new("team_role", "TeamMember")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentTeamService.CanAccessTeam(teamId);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanAccessTeam_WhenTeamDoesNotMatch_ReturnsFalse()
    {
        // Arrange
        var userTeamId = Guid.NewGuid();
        var requestedTeamId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new("team_id", userTeamId.ToString()),
            new("team_role", "TeamMember")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentTeamService.CanAccessTeam(requestedTeamId);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion
} 