using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TeamStride.Infrastructure.Configuration;
using TeamStride.Infrastructure.Middleware;
using Shouldly;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Routing;

namespace TeamStride.Infrastructure.Tests.Middleware;

public class RateLimitingMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly Mock<ILogger<RateLimitingMiddleware>> _mockLogger;
    private readonly TeamStride.Infrastructure.Configuration.RateLimitingOptions _options;
    private readonly RateLimitingMiddleware _middleware;
    private readonly DefaultHttpContext _context;

    public RateLimitingMiddlewareTests()
    {
        _mockNext = new Mock<RequestDelegate>();
        _mockLogger = new Mock<ILogger<RateLimitingMiddleware>>();
        _options = new TeamStride.Infrastructure.Configuration.RateLimitingOptions
        {
            WindowMinutes = 15,
            MaxRequestsPerIp = 100,
            MaxRequestsPerDevice = 50,
            MaxRequestsPerEmail = 5,
            MaxRequestsPerTeam = 200
        };
        var optionsWrapper = Options.Create(_options);
        _middleware = new RateLimitingMiddleware(_mockNext.Object, _mockLogger.Object, optionsWrapper);
        _context = new DefaultHttpContext();
        
        // Set up route data and endpoint
        var routeData = new RouteData();
        routeData.Values["controller"] = "account";
        routeData.Values["action"] = "register";
        _context.Features.Set<IRoutingFeature>(new RoutingFeature { RouteData = routeData });
        
        var endpoint = new Endpoint(
            context => Task.CompletedTask,
            new EndpointMetadataCollection(),
            "TestEndpoint");
        _context.SetEndpoint(endpoint);
    }

    [Fact]
    public async Task InvokeAsync_WhenIpLimitExceeded_Returns429()
    {
        // Arrange
        _context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        for (int i = 0; i <= _options.MaxRequestsPerIp; i++)
        {
            await _middleware.InvokeAsync(_context);
        }

        // Assert
        _context.Response.StatusCode.ShouldBe(429);
        _context.Response.Headers["Retry-After"].ToString().ShouldBe((_options.WindowMinutes * 60).ToString());
    }

    [Fact]
    public async Task InvokeAsync_WhenDeviceLimitExceeded_Returns429()
    {
        // Arrange
        _context.Request.Headers["X-Device-ID"] = "TestDevice";
        for (int i = 0; i <= _options.MaxRequestsPerDevice; i++)
        {
            await _middleware.InvokeAsync(_context);
        }

        // Assert
        _context.Response.StatusCode.ShouldBe(429);
        _context.Response.Headers["Retry-After"].ToString().ShouldBe((_options.WindowMinutes * 60).ToString());
    }

    [Fact]
    public async Task InvokeAsync_WhenEmailLimitExceeded_Returns429()
    {
        // Arrange
        _context.Request.ContentType = "application/x-www-form-urlencoded";
        _context.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            { "email", "test@example.com" }
        });
        
        for (int i = 0; i <= _options.MaxRequestsPerEmail; i++)
        {
            await _middleware.InvokeAsync(_context);
        }

        // Assert
        _context.Response.StatusCode.ShouldBe(429);
        _context.Response.Headers["Retry-After"].ToString().ShouldBe((_options.WindowMinutes * 60).ToString());
    }

    [Fact]
    public async Task InvokeAsync_WhenTeamLimitExceeded_Returns429()
    {
        // Arrange
        var routeData = new RouteData();
        routeData.Values["controller"] = "team";
        routeData.Values["teamId"] = "test-team-id";
        _context.Features.Set<IRoutingFeature>(new RoutingFeature { RouteData = routeData });
        
        for (int i = 0; i <= _options.MaxRequestsPerTeam; i++)
        {
            await _middleware.InvokeAsync(_context);
        }

        // Assert
        _context.Response.StatusCode.ShouldBe(429);
        _context.Response.Headers["Retry-After"].ToString().ShouldBe((_options.WindowMinutes * 60).ToString());
    }

    [Fact]
    public async Task InvokeAsync_WhenNoLimitsExceeded_CallsNext()
    {
        // Arrange
        var nextCalled = false;
        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .Callback(() => nextCalled = true)
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(_context);

        // Assert
        nextCalled.ShouldBeTrue();
        _context.Response.StatusCode.ShouldBe(200);
    }

    private class RoutingFeature : IRoutingFeature
    {
        public RouteData? RouteData { get; set; } = new();
    }
} 