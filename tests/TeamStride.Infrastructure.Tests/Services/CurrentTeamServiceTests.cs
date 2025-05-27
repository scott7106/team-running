using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using TeamStride.Infrastructure.Services;

namespace TeamStride.Infrastructure.Tests.Services;

public class CurrentTeamServiceTests
{
    private readonly Mock<ILogger<CurrentTeamService>> _mockLogger;
    private readonly CurrentTeamService _currentTeamService;

    public CurrentTeamServiceTests()
    {
        _mockLogger = new Mock<ILogger<CurrentTeamService>>();
        _currentTeamService = new CurrentTeamService(_mockLogger.Object);
    }

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
} 