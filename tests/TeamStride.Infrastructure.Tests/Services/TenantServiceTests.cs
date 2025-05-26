using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using TeamStride.Infrastructure.Services;

namespace TeamStride.Infrastructure.Tests.Services;

public class TenantServiceTests
{
    private readonly Mock<ILogger<TenantService>> _mockLogger;
    private readonly TenantService _tenantService;

    public TenantServiceTests()
    {
        _mockLogger = new Mock<ILogger<TenantService>>();
        _tenantService = new TenantService(_mockLogger.Object);
    }

    #region CurrentTenantId Tests

    [Fact]
    public void CurrentTenantId_WhenNotSet_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => _tenantService.CurrentTenantId);
        exception.Message.ShouldBe("Current tenant is not set");
    }

    [Fact]
    public void CurrentTenantId_WhenSetWithGuid_ReturnsCorrectValue()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _tenantService.SetCurrentTenant(tenantId);

        // Act
        var result = _tenantService.CurrentTenantId;

        // Assert
        result.ShouldBe(tenantId);
    }

    [Fact]
    public void CurrentTenantId_AfterClear_ThrowsInvalidOperationException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _tenantService.SetCurrentTenant(tenantId);
        _tenantService.ClearCurrentTenant();

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => _tenantService.CurrentTenantId);
        exception.Message.ShouldBe("Current tenant is not set");
    }

    #endregion

    #region CurrentTenantSubdomain Tests

    [Fact]
    public void CurrentTenantSubdomain_WhenNotSet_ReturnsNull()
    {
        // Act
        var result = _tenantService.CurrentTenantSubdomain;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void CurrentTenantSubdomain_WhenSetWithSubdomain_ReturnsCorrectValue()
    {
        // Arrange
        var subdomain = "test-tenant";
        _tenantService.SetCurrentTenant(subdomain);

        // Act
        var result = _tenantService.CurrentTenantSubdomain;

        // Assert
        result.ShouldBe(subdomain);
    }

    [Fact]
    public void CurrentTenantSubdomain_AfterClear_ReturnsNull()
    {
        // Arrange
        var subdomain = "test-tenant";
        _tenantService.SetCurrentTenant(subdomain);
        _tenantService.ClearCurrentTenant();

        // Act
        var result = _tenantService.CurrentTenantSubdomain;

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region SetCurrentTenant(Guid) Tests

    [Fact]
    public void SetCurrentTenant_WithValidGuid_SetsTenantIdAndLogsInformation()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        _tenantService.SetCurrentTenant(tenantId);

        // Assert
        _tenantService.CurrentTenantId.ShouldBe(tenantId);
        
        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Current tenant set to {tenantId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void SetCurrentTenant_WithEmptyGuid_SetsTenantIdToEmpty()
    {
        // Arrange
        var tenantId = Guid.Empty;

        // Act
        _tenantService.SetCurrentTenant(tenantId);

        // Assert
        _tenantService.CurrentTenantId.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void SetCurrentTenant_WithGuid_OverwritesPreviousValue()
    {
        // Arrange
        var firstTenantId = Guid.NewGuid();
        var secondTenantId = Guid.NewGuid();
        _tenantService.SetCurrentTenant(firstTenantId);

        // Act
        _tenantService.SetCurrentTenant(secondTenantId);

        // Assert
        _tenantService.CurrentTenantId.ShouldBe(secondTenantId);
    }

    #endregion

    #region SetCurrentTenant(string) Tests

    [Fact]
    public void SetCurrentTenant_WithValidSubdomain_SetsSubdomainAndLogsInformation()
    {
        // Arrange
        var subdomain = "test-tenant";

        // Act
        _tenantService.SetCurrentTenant(subdomain);

        // Assert
        _tenantService.CurrentTenantSubdomain.ShouldBe(subdomain);
        
        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Current tenant subdomain set to {subdomain}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void SetCurrentTenant_WithEmptyString_SetsSubdomainToEmpty()
    {
        // Arrange
        var subdomain = string.Empty;

        // Act
        _tenantService.SetCurrentTenant(subdomain);

        // Assert
        _tenantService.CurrentTenantSubdomain.ShouldBe(string.Empty);
    }

    [Fact]
    public void SetCurrentTenant_WithNullString_SetsSubdomainToNull()
    {
        // Arrange
        string? subdomain = null;

        // Act
        _tenantService.SetCurrentTenant(subdomain!);

        // Assert
        _tenantService.CurrentTenantSubdomain.ShouldBeNull();
    }

    [Fact]
    public void SetCurrentTenant_WithSubdomain_OverwritesPreviousValue()
    {
        // Arrange
        var firstSubdomain = "first-tenant";
        var secondSubdomain = "second-tenant";
        _tenantService.SetCurrentTenant(firstSubdomain);

        // Act
        _tenantService.SetCurrentTenant(secondSubdomain);

        // Assert
        _tenantService.CurrentTenantSubdomain.ShouldBe(secondSubdomain);
    }

    [Fact]
    public void SetCurrentTenant_WithSubdomain_DoesNotAffectTenantId()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var subdomain = "test-tenant";
        _tenantService.SetCurrentTenant(tenantId);

        // Act
        _tenantService.SetCurrentTenant(subdomain);

        // Assert
        _tenantService.CurrentTenantId.ShouldBe(tenantId);
        _tenantService.CurrentTenantSubdomain.ShouldBe(subdomain);
    }

    #endregion

    #region ClearCurrentTenant Tests

    [Fact]
    public void ClearCurrentTenant_WhenTenantIsSet_ClearsBothTenantIdAndSubdomainAndLogsInformation()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var subdomain = "test-tenant";
        _tenantService.SetCurrentTenant(tenantId);
        _tenantService.SetCurrentTenant(subdomain);

        // Act
        _tenantService.ClearCurrentTenant();

        // Assert
        Should.Throw<InvalidOperationException>(() => _tenantService.CurrentTenantId);
        _tenantService.CurrentTenantSubdomain.ShouldBeNull();
        
        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Current tenant cleared")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ClearCurrentTenant_WhenTenantNotSet_DoesNotThrowAndLogsInformation()
    {
        // Act
        _tenantService.ClearCurrentTenant();

        // Assert
        Should.Throw<InvalidOperationException>(() => _tenantService.CurrentTenantId);
        _tenantService.CurrentTenantSubdomain.ShouldBeNull();
        
        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Current tenant cleared")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ClearCurrentTenant_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _tenantService.SetCurrentTenant(tenantId);

        // Act & Assert
        Should.NotThrow(() => _tenantService.ClearCurrentTenant());
        Should.NotThrow(() => _tenantService.ClearCurrentTenant());
        Should.NotThrow(() => _tenantService.ClearCurrentTenant());
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void TenantService_CompleteWorkflow_WorksCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var subdomain = "test-tenant";

        // Act & Assert - Initial state
        Should.Throw<InvalidOperationException>(() => _tenantService.CurrentTenantId);
        _tenantService.CurrentTenantSubdomain.ShouldBeNull();

        // Set tenant ID
        _tenantService.SetCurrentTenant(tenantId);
        _tenantService.CurrentTenantId.ShouldBe(tenantId);
        _tenantService.CurrentTenantSubdomain.ShouldBeNull();

        // Set subdomain
        _tenantService.SetCurrentTenant(subdomain);
        _tenantService.CurrentTenantId.ShouldBe(tenantId);
        _tenantService.CurrentTenantSubdomain.ShouldBe(subdomain);

        // Clear tenant
        _tenantService.ClearCurrentTenant();
        Should.Throw<InvalidOperationException>(() => _tenantService.CurrentTenantId);
        _tenantService.CurrentTenantSubdomain.ShouldBeNull();
    }

    [Fact]
    public void TenantService_SetTenantIdAfterSubdomain_BothValuesArePreserved()
    {
        // Arrange
        var subdomain = "test-tenant";
        var tenantId = Guid.NewGuid();

        // Act
        _tenantService.SetCurrentTenant(subdomain);
        _tenantService.SetCurrentTenant(tenantId);

        // Assert
        _tenantService.CurrentTenantId.ShouldBe(tenantId);
        _tenantService.CurrentTenantSubdomain.ShouldBe(subdomain);
    }

    #endregion
} 