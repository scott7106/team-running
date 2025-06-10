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
using System.Text.Json;
using System.Text.Json.Serialization;

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

    private void SetupHttpContextWithTeamMemberships(List<TeamMembershipInfo> memberships, string? currentSubdomain = null, bool isGlobalAdmin = false)
    {
        var claims = new List<Claim>();
        
        // Add authentication claim
        claims.Add(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));
        
        // Add global admin claim if needed
        if (isGlobalAdmin)
        {
            claims.Add(new Claim("is_global_admin", "true"));
        }

        // Add team memberships as JSON
        if (memberships.Any())
        {
            var membershipDtos = memberships.Select(m => new 
            {
                TeamId = m.TeamId.ToString(),
                TeamSubdomain = m.TeamSubdomain,
                TeamRole = m.TeamRole.ToString(),
                MemberType = m.MemberType.ToString()
            }).ToList();

            var membershipJson = JsonSerializer.Serialize(membershipDtos, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            
            claims.Add(new Claim("team_memberships", membershipJson));
        }

        SetupHttpContext(claims);

        // Set the current subdomain if provided
        if (!string.IsNullOrEmpty(currentSubdomain))
        {
            _currentTeamService.SetTeamSubdomain(currentSubdomain);
        }
    }

    private void SetupHttpContextWithSingleTeamMembership(Guid teamId, string teamSubdomain, TeamRole teamRole, MemberType memberType, bool isGlobalAdmin = false)
    {
        var membership = new TeamMembershipInfo(teamId, teamSubdomain, teamRole, memberType);
        SetupHttpContextWithTeamMemberships(new List<TeamMembershipInfo> { membership }, teamSubdomain, isGlobalAdmin);
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
    public void CurrentTeamRole_WhenHttpContextIsNull_ReturnsNull()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _currentTeamService.CurrentTeamRole;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void CurrentTeamRole_WhenClaimDoesNotExist_ReturnsNull()
    {
        // Arrange
        SetupHttpContext();

        // Act
        var result = _currentTeamService.CurrentTeamRole;

        // Assert
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData(TeamRole.TeamOwner)]
    [InlineData(TeamRole.TeamAdmin)]
    [InlineData(TeamRole.TeamMember)]
    public void CurrentTeamRole_WhenValidTeamMembership_ReturnsCorrectRole(TeamRole expectedRole)
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var teamSubdomain = "test-team";
        SetupHttpContextWithSingleTeamMembership(teamId, teamSubdomain, expectedRole, MemberType.Coach);

        // Act
        var result = _currentTeamService.CurrentTeamRole;

        // Assert
        result.ShouldBe(expectedRole);
    }

    [Fact]
    public void CurrentTeamRole_WhenNoMatchingSubdomain_ReturnsNull()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var teamSubdomain = "test-team";
        // Set up membership but different subdomain context
        SetupHttpContextWithSingleTeamMembership(teamId, teamSubdomain, TeamRole.TeamOwner, MemberType.Coach);
        _currentTeamService.SetTeamSubdomain("different-team");

        // Act
        var result = _currentTeamService.CurrentTeamRole;

        // Assert
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData(MemberType.Coach)]
    [InlineData(MemberType.Athlete)]
    [InlineData(MemberType.Parent)]
    public void CurrentMemberType_WhenValidTeamMembership_ReturnsCorrectType(MemberType expectedType)
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var teamSubdomain = "test-team";
        SetupHttpContextWithSingleTeamMembership(teamId, teamSubdomain, TeamRole.TeamMember, expectedType);

        // Act
        var result = _currentTeamService.CurrentMemberType;

        // Assert
        result.ShouldBe(expectedType);
    }

    [Fact]
    public void CurrentMemberType_WhenNoMatchingSubdomain_ReturnsNull()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var teamSubdomain = "test-team";
        // Set up membership but different subdomain context
        SetupHttpContextWithSingleTeamMembership(teamId, teamSubdomain, TeamRole.TeamMember, MemberType.Athlete);
        _currentTeamService.SetTeamSubdomain("different-team");

        // Act
        var result = _currentTeamService.CurrentMemberType;

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region Helper Properties Tests

    [Theory]
    [InlineData(TeamRole.TeamOwner, true)]
    [InlineData(TeamRole.TeamAdmin, false)]
    [InlineData(TeamRole.TeamMember, false)]
    public void IsTeamOwner_ReturnsCorrectValue(TeamRole userRole, bool expected)
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var teamSubdomain = "test-team";
        SetupHttpContextWithSingleTeamMembership(teamId, teamSubdomain, userRole, MemberType.Coach);

        // Act
        var result = _currentTeamService.IsTeamOwner;

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(TeamRole.TeamOwner, false)]
    [InlineData(TeamRole.TeamAdmin, true)]
    [InlineData(TeamRole.TeamMember, false)]
    public void IsTeamAdmin_ReturnsCorrectValue(TeamRole userRole, bool expected)
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var teamSubdomain = "test-team";
        SetupHttpContextWithSingleTeamMembership(teamId, teamSubdomain, userRole, MemberType.Coach);

        // Act
        var result = _currentTeamService.IsTeamAdmin;

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(TeamRole.TeamOwner, false)]
    [InlineData(TeamRole.TeamAdmin, false)]
    [InlineData(TeamRole.TeamMember, true)]
    public void IsTeamMember_ReturnsCorrectValue(TeamRole userRole, bool expected)
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var teamSubdomain = "test-team";
        SetupHttpContextWithSingleTeamMembership(teamId, teamSubdomain, userRole, MemberType.Coach);

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
        var teamSubdomain = "test-team";
        SetupHttpContextWithTeamMemberships(new List<TeamMembershipInfo>(), teamSubdomain, isGlobalAdmin: true);

        // Act
        var result = _currentTeamService.SetTeamFromJwtClaims();

        // Assert
        result.ShouldBeTrue();
        // Note: Global admins don't automatically set team from claims, team context is managed by subdomain
    }

    [Fact]
    public void SetTeamFromJwtClaims_WhenValidTeamAccess_ReturnsTrue()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var teamSubdomain = "test-team";
        SetupHttpContextWithSingleTeamMembership(teamId, teamSubdomain, TeamRole.TeamMember, MemberType.Athlete);

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
        var teamSubdomain = "test-team";
        SetupHttpContextWithTeamMemberships(new List<TeamMembershipInfo>(), teamSubdomain, isGlobalAdmin: true);
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
        var teamId = Guid.NewGuid();
        var teamSubdomain = "test-team";
        SetupHttpContextWithSingleTeamMembership(teamId, teamSubdomain, userRole, MemberType.Coach);

        // Act
        var result = _currentTeamService.HasMinimumTeamRole(minimumRole);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void HasMinimumTeamRole_WhenGlobalAdmin_ReturnsTrue()
    {
        // Arrange
        SetupHttpContextWithTeamMemberships(new List<TeamMembershipInfo>(), null, isGlobalAdmin: true);

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
        var teamSubdomain = "test-team";
        SetupHttpContextWithSingleTeamMembership(teamId, teamSubdomain, TeamRole.TeamMember, MemberType.Athlete);

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
        var teamSubdomain = "user-team";
        SetupHttpContextWithSingleTeamMembership(userTeamId, teamSubdomain, TeamRole.TeamMember, MemberType.Athlete);

        // Act
        var result = _currentTeamService.CanAccessTeam(requestedTeamId);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion
} 