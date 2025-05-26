using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using TeamStride.Infrastructure.Services;

namespace TeamStride.Infrastructure.Tests.Services;

public class TeamServiceTests
{
    private readonly Mock<ILogger<TeamService>> _mockLogger;
    private readonly TeamService _teamService;

    public TeamServiceTests()
    {
        _mockLogger = new Mock<ILogger<TeamService>>();
        _teamService = new TeamService(_mockLogger.Object);
    }

    #region CurrentTeamId Tests

    [Fact]
    public void CurrentTeamId_WhenNotSet_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => _teamService.CurrentTeamId);
        exception.Message.ShouldBe("Current team is not set");
    }

    [Fact]
    public void CurrentTeamId_WhenSetWithGuid_ReturnsCorrectValue()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _teamService.SetCurrentTeam(teamId);

        // Act
        var result = _teamService.CurrentTeamId;

        // Assert
        result.ShouldBe(teamId);
    }

    [Fact]
    public void CurrentTeamId_AfterClear_ThrowsInvalidOperationException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _teamService.SetCurrentTeam(teamId);
        _teamService.ClearCurrentTeam();

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => _teamService.CurrentTeamId);
        exception.Message.ShouldBe("Current team is not set");
    }

    #endregion

    #region CurrentTeamSubdomain Tests

    [Fact]
    public void CurrentTeamSubdomain_WhenNotSet_ReturnsNull()
    {
        // Act
        var result = _teamService.CurrentTeamSubdomain;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void CurrentTeamSubdomain_WhenSetWithSubdomain_ReturnsCorrectValue()
    {
        // Arrange
        var subdomain = "test-team";
        _teamService.SetCurrentTeam(subdomain);

        // Act
        var result = _teamService.CurrentTeamSubdomain;

        // Assert
        result.ShouldBe(subdomain);
    }

    [Fact]
    public void CurrentTeamSubdomain_AfterClear_ReturnsNull()
    {
        // Arrange
        var subdomain = "test-team";
        _teamService.SetCurrentTeam(subdomain);
        _teamService.ClearCurrentTeam();

        // Act
        var result = _teamService.CurrentTeamSubdomain;

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region SetCurrentTeam(Guid) Tests

    [Fact]
    public void SetCurrentTeam_WithValidGuid_SetsTeamIdAndLogsInformation()
    {
        // Arrange
        var teamId = Guid.NewGuid();

        // Act
        _teamService.SetCurrentTeam(teamId);

        // Assert
        _teamService.CurrentTeamId.ShouldBe(teamId);
        
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
    public void SetCurrentTeam_WithEmptyGuid_SetsTeamIdToEmpty()
    {
        // Arrange
        var teamId = Guid.Empty;

        // Act
        _teamService.SetCurrentTeam(teamId);

        // Assert
        _teamService.CurrentTeamId.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void SetCurrentTeam_WithGuid_OverwritesPreviousValue()
    {
        // Arrange
        var firstTeamId = Guid.NewGuid();
        var secondTeamId = Guid.NewGuid();
        _teamService.SetCurrentTeam(firstTeamId);

        // Act
        _teamService.SetCurrentTeam(secondTeamId);

        // Assert
        _teamService.CurrentTeamId.ShouldBe(secondTeamId);
    }

    #endregion

    #region SetCurrentTeam(string) Tests

    [Fact]
    public void SetCurrentTeam_WithValidSubdomain_SetsSubdomainAndLogsInformation()
    {
        // Arrange
        var subdomain = "test-team";

        // Act
        _teamService.SetCurrentTeam(subdomain);

        // Assert
        _teamService.CurrentTeamSubdomain.ShouldBe(subdomain);
        
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
    public void SetCurrentTeam_WithEmptyString_SetsSubdomainToEmpty()
    {
        // Arrange
        var subdomain = string.Empty;

        // Act
        _teamService.SetCurrentTeam(subdomain);

        // Assert
        _teamService.CurrentTeamSubdomain.ShouldBe(string.Empty);
    }

    [Fact]
    public void SetCurrentTeam_WithNullString_SetsSubdomainToNull()
    {
        // Arrange
        string? subdomain = null;

        // Act
        _teamService.SetCurrentTeam(subdomain!);

        // Assert
        _teamService.CurrentTeamSubdomain.ShouldBeNull();
    }

    [Fact]
    public void SetCurrentTeam_WithSubdomain_OverwritesPreviousValue()
    {
        // Arrange
        var firstSubdomain = "first-team";
        var secondSubdomain = "second-team";
        _teamService.SetCurrentTeam(firstSubdomain);

        // Act
        _teamService.SetCurrentTeam(secondSubdomain);

        // Assert
        _teamService.CurrentTeamSubdomain.ShouldBe(secondSubdomain);
    }

    [Fact]
    public void SetCurrentTeam_WithSubdomain_DoesNotAffectTeamId()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var subdomain = "test-team";
        _teamService.SetCurrentTeam(teamId);

        // Act
        _teamService.SetCurrentTeam(subdomain);

        // Assert
        _teamService.CurrentTeamId.ShouldBe(teamId);
        _teamService.CurrentTeamSubdomain.ShouldBe(subdomain);
    }

    #endregion

    #region ClearCurrentTeam Tests

    [Fact]
    public void ClearCurrentTeam_WhenTeamIsSet_ClearsBothTeamIdAndSubdomainAndLogsInformation()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var subdomain = "test-team";
        _teamService.SetCurrentTeam(teamId);
        _teamService.SetCurrentTeam(subdomain);

        // Act
        _teamService.ClearCurrentTeam();

        // Assert
        Should.Throw<InvalidOperationException>(() => _teamService.CurrentTeamId);
        _teamService.CurrentTeamSubdomain.ShouldBeNull();
        
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

    [Fact]
    public void ClearCurrentTeam_WhenTeamNotSet_DoesNotThrowAndLogsInformation()
    {
        // Act
        _teamService.ClearCurrentTeam();

        // Assert
        Should.Throw<InvalidOperationException>(() => _teamService.CurrentTeamId);
        _teamService.CurrentTeamSubdomain.ShouldBeNull();
        
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

    [Fact]
    public void ClearCurrentTeam_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _teamService.SetCurrentTeam(teamId);

        // Act & Assert
        Should.NotThrow(() => _teamService.ClearCurrentTeam());
        Should.NotThrow(() => _teamService.ClearCurrentTeam());
        Should.NotThrow(() => _teamService.ClearCurrentTeam());
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void TeamService_CompleteWorkflow_WorksCorrectly()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var subdomain = "test-team";

        // Act & Assert - Initial state
        Should.Throw<InvalidOperationException>(() => _teamService.CurrentTeamId);
        _teamService.CurrentTeamSubdomain.ShouldBeNull();

        // Set team ID
        _teamService.SetCurrentTeam(teamId);
        _teamService.CurrentTeamId.ShouldBe(teamId);
        _teamService.CurrentTeamSubdomain.ShouldBeNull();

        // Set subdomain
        _teamService.SetCurrentTeam(subdomain);
        _teamService.CurrentTeamId.ShouldBe(teamId);
        _teamService.CurrentTeamSubdomain.ShouldBe(subdomain);

        // Clear team
        _teamService.ClearCurrentTeam();
        Should.Throw<InvalidOperationException>(() => _teamService.CurrentTeamId);
        _teamService.CurrentTeamSubdomain.ShouldBeNull();
    }

    [Fact]
    public void TeamService_SetTeamIdAfterSubdomain_BothValuesArePreserved()
    {
        // Arrange
        var subdomain = "test-team";
        var teamId = Guid.NewGuid();

        // Act
        _teamService.SetCurrentTeam(subdomain);
        _teamService.SetCurrentTeam(teamId);

        // Assert
        _teamService.CurrentTeamId.ShouldBe(teamId);
        _teamService.CurrentTeamSubdomain.ShouldBe(subdomain);
    }

    #endregion
} 