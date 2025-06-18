using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System.Text.Json;
using TeamStride.Api.Middleware;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Domain.Interfaces;

namespace TeamStride.Api.Tests.Middleware;

public class TeamMiddlewareTests
{
    private readonly Mock<ILogger<TeamMiddleware>> _mockLogger;
    private readonly Mock<ICurrentTeamService> _mockTeamService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly TeamMiddleware _middleware;
    private readonly Mock<RequestDelegate> _mockNext;

    public TeamMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<TeamMiddleware>>();
        _mockTeamService = new Mock<ICurrentTeamService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockNext = new Mock<RequestDelegate>();
        
        _middleware = new TeamMiddleware(_mockNext.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task InvokeAsync_WithMainSite_SkipsTeamResolution()
    {
        // Arrange
        var context = CreateHttpContext("teamstride.net");
        SetupServices(context);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockTeamService.Verify(x => x.ClearTeam(), Times.Exactly(2)); // Once at start, once in finally
        _mockNext.Verify(x => x(context), Times.Once);
        _mockTeamService.Verify(x => x.SetTeamFromSubdomainAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithApiEndpoint_SkipsTeamResolution()
    {
        // Arrange
        var context = CreateHttpContext("api.teamstride.net");
        SetupServices(context);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockTeamService.Verify(x => x.ClearTeam(), Times.Exactly(2)); // Once at start, once in finally
        _mockNext.Verify(x => x(context), Times.Once);
        _mockTeamService.Verify(x => x.SetTeamFromSubdomainAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithWwwSubdomain_SkipsTeamResolution()
    {
        // Arrange
        var context = CreateHttpContext("www.teamstride.net");
        SetupServices(context);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockTeamService.Verify(x => x.ClearTeam(), Times.Exactly(2)); // Once at start, once in finally
        _mockNext.Verify(x => x(context), Times.Once);
        _mockTeamService.Verify(x => x.SetTeamFromSubdomainAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidSubdomain_ReturnsNotFound()
    {
        // Arrange
        var context = CreateHttpContext("ab.teamstride.net"); // Too short
        SetupServices(context);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.ShouldBe(404);
        _mockNext.Verify(x => x(context), Times.Never);
        _mockTeamService.Verify(x => x.ClearTeam(), Times.Exactly(2)); // Once at start, once in finally
    }

    [Fact]
    public async Task InvokeAsync_WithValidSubdomain_TeamNotFound_ReturnsNotFound()
    {
        // Arrange
        var context = CreateHttpContext("nonexistent.teamstride.net");
        SetupServices(context);
        _mockTeamService.Setup(x => x.SetTeamFromSubdomainAsync("nonexistent"))
            .ReturnsAsync(false);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.ShouldBe(404);
        _mockNext.Verify(x => x(context), Times.Never);
        _mockTeamService.Verify(x => x.SetTeamFromSubdomainAsync("nonexistent"), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithValidSubdomain_TeamFound_UnauthenticatedUser_Succeeds()
    {
        // Arrange
        var context = CreateHttpContext("testteam.teamstride.net");
        SetupServices(context);
        var teamId = Guid.NewGuid();
        
        _mockTeamService.Setup(x => x.SetTeamFromSubdomainAsync("testteam"))
            .ReturnsAsync(true);
        _mockTeamService.Setup(x => x.TeamId).Returns(teamId);
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(false);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.ShouldBe(200);
        _mockNext.Verify(x => x(context), Times.Once);
        _mockTeamService.Verify(x => x.SetTeamFromSubdomainAsync("testteam"), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithValidSubdomain_AuthenticatedUser_HasAccess_Succeeds()
    {
        // Arrange
        var context = CreateHttpContext("testteam.teamstride.net");
        SetupServices(context);
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        _mockTeamService.Setup(x => x.SetTeamFromSubdomainAsync("testteam"))
            .ReturnsAsync(true);
        _mockTeamService.Setup(x => x.TeamId).Returns(teamId);
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(false);
        _mockCurrentUserService.Setup(x => x.CanAccessTeam(teamId)).Returns(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.ShouldBe(200);
        _mockNext.Verify(x => x(context), Times.Once);
        _mockTeamService.Verify(x => x.SetTeamFromSubdomainAsync("testteam"), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithValidSubdomain_AuthenticatedUser_NoAccess_ReturnsForbidden()
    {
        // Arrange
        var context = CreateHttpContext("testteam.teamstride.net");
        SetupServices(context);
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        _mockTeamService.Setup(x => x.SetTeamFromSubdomainAsync("testteam"))
            .ReturnsAsync(true);
        _mockTeamService.Setup(x => x.TeamId).Returns(teamId);
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(false);
        _mockCurrentUserService.Setup(x => x.CanAccessTeam(teamId)).Returns(false);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.ShouldBe(403);
        _mockNext.Verify(x => x(context), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithValidSubdomain_GlobalAdmin_AlwaysSucceeds()
    {
        // Arrange
        var context = CreateHttpContext("testteam.teamstride.net");
        SetupServices(context);
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        _mockTeamService.Setup(x => x.SetTeamFromSubdomainAsync("testteam"))
            .ReturnsAsync(true);
        _mockTeamService.Setup(x => x.TeamId).Returns(teamId);
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.ShouldBe(200);
        _mockNext.Verify(x => x(context), Times.Once);
        _mockCurrentUserService.Verify(x => x.CanAccessTeam(It.IsAny<Guid>()), Times.Never); // Global admin bypasses this check
    }

    [Fact]
    public async Task InvokeAsync_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var context = CreateHttpContext("testteam.teamstride.net");
        SetupServices(context);
        
        _mockTeamService.Setup(x => x.SetTeamFromSubdomainAsync("testteam"))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.ShouldBe(500);
        _mockNext.Verify(x => x(context), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_AlwaysClearsTeamInFinally()
    {
        // Arrange
        var context = CreateHttpContext("testteam.teamstride.net");
        SetupServices(context);
        
        _mockTeamService.Setup(x => x.SetTeamFromSubdomainAsync("testteam"))
            .ReturnsAsync(true);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockTeamService.Verify(x => x.ClearTeam(), Times.Exactly(2)); // Once at start, once in finally
    }

    private HttpContext CreateHttpContext(string host)
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString(host);
        context.Response.Body = new MemoryStream();
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