using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using TeamStride.Application.Common.Services;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Interfaces;
using TeamStride.Infrastructure.Services;

namespace TeamStride.Infrastructure.Tests.Services;

public class AuthorizationServiceTests
{
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<ILogger<AuthorizationService>> _mockLogger;
    private readonly AuthorizationService _authorizationService;

    public AuthorizationServiceTests()
    {
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockLogger = new Mock<ILogger<AuthorizationService>>();
        _authorizationService = new AuthorizationService(_mockCurrentUserService.Object, _mockLogger.Object);
    }

    #region RequireGlobalAdminAsync Tests

    [Fact]
    public async Task RequireGlobalAdminAsync_WhenUserIsNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(false);

        // Act & Assert
        var exception = await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _authorizationService.RequireGlobalAdminAsync());
        
        exception.Message.ShouldBe("User is not authenticated");
    }

    [Fact]
    public async Task RequireGlobalAdminAsync_WhenUserIsNotGlobalAdmin_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(false);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());

        // Act & Assert
        var exception = await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _authorizationService.RequireGlobalAdminAsync());
        
        exception.Message.ShouldBe("Global admin privileges required");
    }

    [Fact]
    public async Task RequireGlobalAdminAsync_WhenUserIsGlobalAdmin_ShouldNotThrow()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(true);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());

        // Act & Assert
        await Should.NotThrowAsync(() => _authorizationService.RequireGlobalAdminAsync());
    }

    #endregion

    #region RequireTeamAccessAsync Tests

    [Fact]
    public async Task RequireTeamAccessAsync_WhenUserIsNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(false);

        // Act & Assert
        var exception = await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _authorizationService.RequireTeamAccessAsync(teamId));
        
        exception.Message.ShouldBe("User is not authenticated");
    }

    [Fact]
    public async Task RequireTeamAccessAsync_WhenUserIsGlobalAdmin_ShouldNotThrow()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(true);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());

        // Act & Assert
        await Should.NotThrowAsync(() => _authorizationService.RequireTeamAccessAsync(teamId, TeamRole.TeamOwner));
    }

    [Fact]
    public async Task RequireTeamAccessAsync_WhenUserCannotAccessTeam_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(false);
        _mockCurrentUserService.Setup(x => x.CanAccessTeam(teamId)).Returns(false);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);

        // Act & Assert
        var exception = await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _authorizationService.RequireTeamAccessAsync(teamId));
        
        exception.Message.ShouldBe($"Access denied to team {teamId}");
    }

    [Fact]
    public async Task RequireTeamAccessAsync_WhenUserHasInsufficientRole_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(false);
        _mockCurrentUserService.Setup(x => x.CanAccessTeam(teamId)).Returns(true);
        _mockCurrentUserService.Setup(x => x.HasMinimumTeamRole(TeamRole.TeamAdmin)).Returns(false);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);

        // Act & Assert
        var exception = await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _authorizationService.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin));
        
        exception.Message.ShouldBe("Minimum role TeamAdmin required");
    }

    [Fact]
    public async Task RequireTeamAccessAsync_WhenUserHasSufficientAccess_ShouldNotThrow()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(false);
        _mockCurrentUserService.Setup(x => x.CanAccessTeam(teamId)).Returns(true);
        _mockCurrentUserService.Setup(x => x.HasMinimumTeamRole(TeamRole.TeamMember)).Returns(true);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);

        // Act & Assert
        await Should.NotThrowAsync(() => _authorizationService.RequireTeamAccessAsync(teamId));
    }

    #endregion

    #region RequireTeamOwnershipAsync Tests

    [Fact]
    public async Task RequireTeamOwnershipAsync_WhenUserIsNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(false);

        // Act & Assert
        var exception = await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _authorizationService.RequireTeamOwnershipAsync(teamId));
        
        exception.Message.ShouldBe("User is not authenticated");
    }

    [Fact]
    public async Task RequireTeamOwnershipAsync_WhenUserIsGlobalAdmin_ShouldNotThrow()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(true);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());

        // Act & Assert
        await Should.NotThrowAsync(() => _authorizationService.RequireTeamOwnershipAsync(teamId));
    }

    [Fact]
    public async Task RequireTeamOwnershipAsync_WhenUserIsNotTeamOwner_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(false);
        _mockCurrentUserService.Setup(x => x.CanAccessTeam(teamId)).Returns(true);
        _mockCurrentUserService.Setup(x => x.HasMinimumTeamRole(TeamRole.TeamOwner)).Returns(false);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);

        // Act & Assert
        var exception = await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _authorizationService.RequireTeamOwnershipAsync(teamId));
        
        exception.Message.ShouldBe($"Team ownership required for team {teamId}");
    }

    [Fact]
    public async Task RequireTeamOwnershipAsync_WhenUserIsTeamOwner_ShouldNotThrow()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(false);
        _mockCurrentUserService.Setup(x => x.CanAccessTeam(teamId)).Returns(true);
        _mockCurrentUserService.Setup(x => x.HasMinimumTeamRole(TeamRole.TeamOwner)).Returns(true);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);

        // Act & Assert
        await Should.NotThrowAsync(() => _authorizationService.RequireTeamOwnershipAsync(teamId));
    }

    #endregion

    #region RequireTeamAdminAsync Tests

    [Fact]
    public async Task RequireTeamAdminAsync_ShouldCallRequireTeamAccessWithAdminRole()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(false);
        _mockCurrentUserService.Setup(x => x.CanAccessTeam(teamId)).Returns(true);
        _mockCurrentUserService.Setup(x => x.HasMinimumTeamRole(TeamRole.TeamAdmin)).Returns(true);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);

        // Act & Assert
        await Should.NotThrowAsync(() => _authorizationService.RequireTeamAdminAsync(teamId));
    }

    #endregion

    #region CanAccessTeamAsync Tests

    [Fact]
    public async Task CanAccessTeamAsync_WhenUserIsNotAuthenticated_ShouldReturnFalse()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(false);

        // Act
        var result = await _authorizationService.CanAccessTeamAsync(teamId);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task CanAccessTeamAsync_WhenUserIsGlobalAdmin_ShouldReturnTrue()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(true);

        // Act
        var result = await _authorizationService.CanAccessTeamAsync(teamId);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task CanAccessTeamAsync_WhenUserHasAccess_ShouldReturnTrue()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(false);
        _mockCurrentUserService.Setup(x => x.CanAccessTeam(teamId)).Returns(true);
        _mockCurrentUserService.Setup(x => x.HasMinimumTeamRole(TeamRole.TeamMember)).Returns(true);

        // Act
        var result = await _authorizationService.CanAccessTeamAsync(teamId);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task CanAccessTeamAsync_WhenUserLacksAccess_ShouldReturnFalse()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(false);
        _mockCurrentUserService.Setup(x => x.CanAccessTeam(teamId)).Returns(false);

        // Act
        var result = await _authorizationService.CanAccessTeamAsync(teamId);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsTeamOwnerAsync Tests

    [Fact]
    public async Task IsTeamOwnerAsync_WhenUserIsNotAuthenticated_ShouldReturnFalse()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(false);

        // Act
        var result = await _authorizationService.IsTeamOwnerAsync(teamId);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task IsTeamOwnerAsync_WhenUserIsGlobalAdmin_ShouldReturnTrue()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(true);

        // Act
        var result = await _authorizationService.IsTeamOwnerAsync(teamId);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsTeamOwnerAsync_WhenUserIsTeamOwner_ShouldReturnTrue()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(false);
        _mockCurrentUserService.Setup(x => x.CanAccessTeam(teamId)).Returns(true);
        _mockCurrentUserService.Setup(x => x.HasMinimumTeamRole(TeamRole.TeamOwner)).Returns(true);

        // Act
        var result = await _authorizationService.IsTeamOwnerAsync(teamId);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsTeamOwnerAsync_WhenUserIsNotTeamOwner_ShouldReturnFalse()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(false);
        _mockCurrentUserService.Setup(x => x.CanAccessTeam(teamId)).Returns(true);
        _mockCurrentUserService.Setup(x => x.HasMinimumTeamRole(TeamRole.TeamOwner)).Returns(false);

        // Act
        var result = await _authorizationService.IsTeamOwnerAsync(teamId);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsTeamAdminAsync Tests

    [Fact]
    public async Task IsTeamAdminAsync_ShouldCallCanAccessTeamWithAdminRole()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(false);
        _mockCurrentUserService.Setup(x => x.CanAccessTeam(teamId)).Returns(true);
        _mockCurrentUserService.Setup(x => x.HasMinimumTeamRole(TeamRole.TeamAdmin)).Returns(true);

        // Act
        var result = await _authorizationService.IsTeamAdminAsync(teamId);

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region Resource Access Tests

    private class TestTeamResource : ITeamResource
    {
        public Guid TeamId { get; set; }
    }

    [Fact]
    public async Task RequireResourceAccessAsync_WhenResourceIsNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => _authorizationService.RequireResourceAccessAsync<TestTeamResource>(null!));
    }

    [Fact]
    public async Task RequireResourceAccessAsync_WhenUserHasAccess_ShouldNotThrow()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var resource = new TestTeamResource { TeamId = teamId };
        var userId = Guid.NewGuid();
        
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(false);
        _mockCurrentUserService.Setup(x => x.CanAccessTeam(teamId)).Returns(true);
        _mockCurrentUserService.Setup(x => x.HasMinimumTeamRole(TeamRole.TeamMember)).Returns(true);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);

        // Act & Assert
        await Should.NotThrowAsync(() => _authorizationService.RequireResourceAccessAsync(resource));
    }

    [Fact]
    public async Task CanAccessResourceAsync_WhenResourceIsNull_ShouldReturnFalse()
    {
        // Act
        var result = await _authorizationService.CanAccessResourceAsync<TestTeamResource>(null!);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task CanAccessResourceAsync_WhenUserHasAccess_ShouldReturnTrue()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var resource = new TestTeamResource { TeamId = teamId };
        
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(false);
        _mockCurrentUserService.Setup(x => x.CanAccessTeam(teamId)).Returns(true);
        _mockCurrentUserService.Setup(x => x.HasMinimumTeamRole(TeamRole.TeamMember)).Returns(true);

        // Act
        var result = await _authorizationService.CanAccessResourceAsync(resource);

        // Assert
        result.ShouldBeTrue();
    }

    #endregion
} 