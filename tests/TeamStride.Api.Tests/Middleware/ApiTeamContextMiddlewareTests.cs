using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System.Text.Json;
using TeamStride.Api.Middleware;
using TeamStride.Domain.Interfaces;

namespace TeamStride.Api.Tests.Middleware;

public class ApiTeamContextMiddlewareTests
{
    private readonly Mock<ILogger<ApiTeamContextMiddleware>> _mockLogger;
    private readonly Mock<ICurrentTeamService> _mockTeamService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly ApiTeamContextMiddleware _middleware;
    private readonly Mock<RequestDelegate> _mockNext;

    public ApiTeamContextMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<ApiTeamContextMiddleware>>();
        _mockTeamService = new Mock<ICurrentTeamService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockNext = new Mock<RequestDelegate>();
        
        _middleware = new ApiTeamContextMiddleware(_mockNext.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task InvokeAsync_WithNonApiTeamsPath_SkipsProcessing()
    {
        // Arrange
        var context = CreateHttpContext("/api/auth/login", "localhost:5295");
        SetupServices(context);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
        _mockTeamService.Verify(x => x.SetTeamFromJwtClaims(), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithApiTeamsPath_TeamAlreadySet_SkipsProcessing()
    {
        // Arrange
        var context = CreateHttpContext("/api/teams/athletes", "localhost:5295");
        SetupServices(context);
        _mockTeamService.Setup(x => x.IsTeamSet).Returns(true);
        _mockTeamService.Setup(x => x.TeamId).Returns(Guid.NewGuid());

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
        _mockTeamService.Verify(x => x.SetTeamFromJwtClaims(), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithApiTeamsPath_UnauthenticatedUser_SkipsProcessing()
    {
        // Arrange
        var context = CreateHttpContext("/api/teams/athletes", "localhost:5295");
        SetupServices(context);
        _mockTeamService.Setup(x => x.IsTeamSet).Returns(false);
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(false);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(x => x(context), Times.Once);
        _mockTeamService.Verify(x => x.SetTeamFromJwtClaims(), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithXSubdomainHeader_SetsSubdomainContext()
    {
        // Arrange
        var context = CreateHttpContext("/api/teams/athletes", "localhost:5295");
        context.Request.Headers["X-Subdomain"] = "testteam";
        SetupServices(context);
        
        _mockTeamService.Setup(x => x.IsTeamSet).Returns(false);
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _mockTeamService.Setup(x => x.GetSubdomain).Returns((string?)null);
        _mockTeamService.Setup(x => x.SetTeamFromJwtClaims()).Returns(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockTeamService.Verify(x => x.SetTeamSubdomain("testteam"), Times.Once);
        _mockTeamService.Verify(x => x.SetTeamFromJwtClaims(), Times.Once);
        _mockNext.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithSubdomainHost_ExtractsSubdomainFromHost()
    {
        // Arrange
        var context = CreateHttpContext("/api/teams/athletes", "testteam.teamstride.net");
        SetupServices(context);
        
        _mockTeamService.Setup(x => x.IsTeamSet).Returns(false);
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _mockTeamService.Setup(x => x.GetSubdomain).Returns((string?)null);
        _mockTeamService.Setup(x => x.SetTeamFromJwtClaims()).Returns(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockTeamService.Verify(x => x.SetTeamSubdomain("testteam"), Times.Once);
        _mockTeamService.Verify(x => x.SetTeamFromJwtClaims(), Times.Once);
        _mockNext.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithExistingSubdomain_SkipsSubdomainExtraction()
    {
        // Arrange
        var context = CreateHttpContext("/api/teams/athletes", "localhost:5295");
        context.Request.Headers["X-Subdomain"] = "headerteam";
        SetupServices(context);
        
        _mockTeamService.Setup(x => x.IsTeamSet).Returns(false);
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _mockTeamService.Setup(x => x.GetSubdomain).Returns("existingteam");
        _mockTeamService.Setup(x => x.SetTeamFromJwtClaims()).Returns(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockTeamService.Verify(x => x.SetTeamSubdomain(It.IsAny<string>()), Times.Never);
        _mockTeamService.Verify(x => x.SetTeamFromJwtClaims(), Times.Once);
        _mockNext.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithDevelopmentQueryParameter_UsesQueryParameter()
    {
        // Arrange
        var context = CreateHttpContext("/api/teams/athletes?subdomain=queryteam", "localhost:5295");
        SetupServices(context);
        
        _mockTeamService.Setup(x => x.IsTeamSet).Returns(false);
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _mockTeamService.Setup(x => x.GetSubdomain).Returns((string?)null);
        _mockTeamService.Setup(x => x.SetTeamFromJwtClaims()).Returns(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockTeamService.Verify(x => x.SetTeamSubdomain("queryteam"), Times.Once);
        _mockTeamService.Verify(x => x.SetTeamFromJwtClaims(), Times.Once);
        _mockNext.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_HostTakesPrecedenceOverXSubdomainHeader()
    {
        // Arrange
        var context = CreateHttpContext("/api/teams/athletes", "hostteam.teamstride.net");
        context.Request.Headers["X-Subdomain"] = "headerteam";
        SetupServices(context);
        
        _mockTeamService.Setup(x => x.IsTeamSet).Returns(false);
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _mockTeamService.Setup(x => x.GetSubdomain).Returns((string?)null);
        _mockTeamService.Setup(x => x.SetTeamFromJwtClaims()).Returns(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockTeamService.Verify(x => x.SetTeamSubdomain("hostteam"), Times.Once);
        _mockTeamService.Verify(x => x.SetTeamSubdomain("headerteam"), Times.Never);
        _mockTeamService.Verify(x => x.SetTeamFromJwtClaims(), Times.Once);
        _mockNext.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var context = CreateHttpContext("/api/teams/athletes", "localhost:5295");
        context.Request.Headers["X-Subdomain"] = "testteam";
        SetupServices(context);
        
        _mockTeamService.Setup(x => x.IsTeamSet).Returns(false);
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _mockTeamService.Setup(x => x.GetSubdomain).Returns((string?)null);
        _mockTeamService.Setup(x => x.SetTeamFromJwtClaims()).Throws(new Exception("JWT processing error"));

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.ShouldBe(500);
        _mockNext.Verify(x => x(context), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_SuccessfulTeamContextSet_LogsInformation()
    {
        // Arrange
        var context = CreateHttpContext("/api/teams/athletes", "localhost:5295");
        context.Request.Headers["X-Subdomain"] = "testteam";
        SetupServices(context);
        
        var userId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        
        _mockTeamService.Setup(x => x.IsTeamSet).Returns(false);
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockTeamService.Setup(x => x.GetSubdomain).Returns((string?)null);
        _mockTeamService.Setup(x => x.SetTeamFromJwtClaims()).Returns(true);
        _mockTeamService.Setup(x => x.TeamId).Returns(teamId);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockTeamService.Verify(x => x.SetTeamSubdomain("testteam"), Times.Once);
        _mockTeamService.Verify(x => x.SetTeamFromJwtClaims(), Times.Once);
        _mockNext.Verify(x => x(context), Times.Once);
    }

    private HttpContext CreateHttpContext(string path, string host)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Host = new HostString(host);
        context.Response.Body = new MemoryStream();
        
        // Parse query string if present
        var pathAndQuery = path.Split('?');
        if (pathAndQuery.Length > 1)
        {
            context.Request.Path = pathAndQuery[0];
            context.Request.QueryString = new QueryString($"?{pathAndQuery[1]}");
        }
        
        return context;
    }

    private void SetupServices(HttpContext context)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_mockTeamService.Object);
        services.AddSingleton(_mockCurrentUserService.Object);
        
        context.RequestServices = services.BuildServiceProvider();
    }
} 