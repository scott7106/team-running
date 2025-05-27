using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using System.Security.Claims;
using TeamStride.Api.Authorization;
using Xunit;

namespace TeamStride.Api.Tests.Authorization;

public class RequireGlobalAdminAttributeTests
{
    private readonly RequireGlobalAdminAttribute _attribute;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<ClaimsPrincipal> _mockUser;
    private readonly Mock<ClaimsIdentity> _mockIdentity;

    public RequireGlobalAdminAttributeTests()
    {
        _attribute = new RequireGlobalAdminAttribute();
        _mockHttpContext = new Mock<HttpContext>();
        _mockUser = new Mock<ClaimsPrincipal>();
        _mockIdentity = new Mock<ClaimsIdentity>();

        _mockHttpContext.Setup(x => x.User).Returns(_mockUser.Object);
        _mockUser.Setup(x => x.Identity).Returns(_mockIdentity.Object);
    }

    private AuthorizationFilterContext CreateAuthorizationContext()
    {
        var actionContext = new ActionContext(
            _mockHttpContext.Object,
            new RouteData(),
            new ActionDescriptor()
        );

        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    [Fact]
    public void OnAuthorization_WhenUserIsAuthenticatedGlobalAdmin_ShouldAllowAccess()
    {
        // Arrange
        _mockIdentity.Setup(x => x.IsAuthenticated).Returns(true);
        
        var claims = new List<Claim>
        {
            new("is_global_admin", "true")
        };
        _mockUser.Setup(x => x.FindFirst("is_global_admin")).Returns(claims[0]);

        var context = CreateAuthorizationContext();

        // Act
        _attribute.OnAuthorization(context);

        // Assert
        Assert.Null(context.Result);
    }

    [Fact]
    public void OnAuthorization_WhenUserIsAuthenticatedButNotGlobalAdmin_ShouldDenyAccess()
    {
        // Arrange
        _mockIdentity.Setup(x => x.IsAuthenticated).Returns(true);
        
        var claims = new List<Claim>
        {
            new("is_global_admin", "false")
        };
        _mockUser.Setup(x => x.FindFirst("is_global_admin")).Returns(claims[0]);

        var context = CreateAuthorizationContext();

        // Act
        _attribute.OnAuthorization(context);

        // Assert
        Assert.NotNull(context.Result);
        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public void OnAuthorization_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        _mockIdentity.Setup(x => x.IsAuthenticated).Returns(false);

        var context = CreateAuthorizationContext();

        // Act
        _attribute.OnAuthorization(context);

        // Assert
        Assert.NotNull(context.Result);
        Assert.IsType<UnauthorizedResult>(context.Result);
    }

    [Fact]
    public void OnAuthorization_WhenUserIdentityIsNull_ShouldReturnUnauthorized()
    {
        // Arrange
        _mockUser.Setup(x => x.Identity).Returns((ClaimsIdentity?)null);

        var context = CreateAuthorizationContext();

        // Act
        _attribute.OnAuthorization(context);

        // Assert
        Assert.NotNull(context.Result);
        Assert.IsType<UnauthorizedResult>(context.Result);
    }

    [Fact]
    public void OnAuthorization_WhenGlobalAdminClaimIsMissing_ShouldDenyAccess()
    {
        // Arrange
        _mockIdentity.Setup(x => x.IsAuthenticated).Returns(true);
        _mockUser.Setup(x => x.FindFirst("is_global_admin")).Returns((Claim?)null);

        var context = CreateAuthorizationContext();

        // Act
        _attribute.OnAuthorization(context);

        // Assert
        Assert.NotNull(context.Result);
        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public void OnAuthorization_WhenGlobalAdminClaimHasInvalidValue_ShouldDenyAccess()
    {
        // Arrange
        _mockIdentity.Setup(x => x.IsAuthenticated).Returns(true);
        
        var claims = new List<Claim>
        {
            new("is_global_admin", "invalid_value")
        };
        _mockUser.Setup(x => x.FindFirst("is_global_admin")).Returns(claims[0]);

        var context = CreateAuthorizationContext();

        // Act
        _attribute.OnAuthorization(context);

        // Assert
        Assert.NotNull(context.Result);
        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public void OnAuthorization_WhenGlobalAdminClaimIsEmptyString_ShouldDenyAccess()
    {
        // Arrange
        _mockIdentity.Setup(x => x.IsAuthenticated).Returns(true);
        
        var claims = new List<Claim>
        {
            new("is_global_admin", "")
        };
        _mockUser.Setup(x => x.FindFirst("is_global_admin")).Returns(claims[0]);

        var context = CreateAuthorizationContext();

        // Act
        _attribute.OnAuthorization(context);

        // Assert
        Assert.NotNull(context.Result);
        Assert.IsType<ForbidResult>(context.Result);
    }

    [Theory]
    [InlineData("True")]
    [InlineData("TRUE")]
    [InlineData("true")]
    public void OnAuthorization_WhenGlobalAdminClaimIsTrueInDifferentCases_ShouldAllowAccess(string claimValue)
    {
        // Arrange
        _mockIdentity.Setup(x => x.IsAuthenticated).Returns(true);
        
        var claims = new List<Claim>
        {
            new("is_global_admin", claimValue)
        };
        _mockUser.Setup(x => x.FindFirst("is_global_admin")).Returns(claims[0]);

        var context = CreateAuthorizationContext();

        // Act
        _attribute.OnAuthorization(context);

        // Assert
        Assert.Null(context.Result);
    }

    [Theory]
    [InlineData("False")]
    [InlineData("FALSE")]
    [InlineData("false")]
    [InlineData("0")]
    [InlineData("no")]
    [InlineData("off")]
    public void OnAuthorization_WhenGlobalAdminClaimIsFalseOrOtherValues_ShouldDenyAccess(string claimValue)
    {
        // Arrange
        _mockIdentity.Setup(x => x.IsAuthenticated).Returns(true);
        
        var claims = new List<Claim>
        {
            new("is_global_admin", claimValue)
        };
        _mockUser.Setup(x => x.FindFirst("is_global_admin")).Returns(claims[0]);

        var context = CreateAuthorizationContext();

        // Act
        _attribute.OnAuthorization(context);

        // Assert
        Assert.NotNull(context.Result);
        Assert.IsType<ForbidResult>(context.Result);
    }
} 