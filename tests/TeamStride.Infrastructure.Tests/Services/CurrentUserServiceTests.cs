using Microsoft.AspNetCore.Http;
using Moq;
using Shouldly;
using System.Security.Claims;
using TeamStride.Infrastructure.Services;

namespace TeamStride.Infrastructure.Tests.Services;

public class CurrentUserServiceTests
{
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<ClaimsPrincipal> _mockUser;
    private readonly CurrentUserService _currentUserService;

    public CurrentUserServiceTests()
    {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockHttpContext = new Mock<HttpContext>();
        _mockUser = new Mock<ClaimsPrincipal>();
        _currentUserService = new CurrentUserService(_mockHttpContextAccessor.Object);
    }

    #region UserId Tests

    [Fact]
    public void UserId_WhenHttpContextIsNull_ReturnsNull()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _currentUserService.UserId;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void UserId_WhenUserIsNull_ReturnsNull()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns((ClaimsPrincipal?)null!);

        // Act
        var result = _currentUserService.UserId;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void UserId_WhenNameIdentifierClaimIsNull_ReturnsNull()
    {
        // Arrange
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.UserId;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void UserId_WhenNameIdentifierClaimExists_ReturnsCorrectGuid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.UserId;

        // Assert
        result.ShouldBe(userId);
    }

    [Fact]
    public void UserId_WhenNameIdentifierClaimIsEmpty_ThrowsFormatException()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, string.Empty)
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act & Assert
        Should.Throw<FormatException>(() => _currentUserService.UserId);
    }

    [Fact]
    public void UserId_WhenNameIdentifierClaimIsInvalidGuid_ThrowsFormatException()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "invalid-guid")
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act & Assert
        Should.Throw<FormatException>(() => _currentUserService.UserId);
    }

    [Fact]
    public void UserId_WhenMultipleNameIdentifierClaims_ReturnsFirstOne()
    {
        // Arrange
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, firstUserId.ToString()),
            new(ClaimTypes.NameIdentifier, secondUserId.ToString())
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.UserId;

        // Assert
        result.ShouldBe(firstUserId);
    }

    #endregion

    #region UserEmail Tests

    [Fact]
    public void UserEmail_WhenHttpContextIsNull_ReturnsNull()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _currentUserService.UserEmail;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void UserEmail_WhenUserIsNull_ReturnsNull()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns((ClaimsPrincipal?)null!);

        // Act
        var result = _currentUserService.UserEmail;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void UserEmail_WhenEmailClaimIsNull_ReturnsNull()
    {
        // Arrange
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.UserEmail;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void UserEmail_WhenEmailClaimExists_ReturnsCorrectEmail()
    {
        // Arrange
        var email = "test@example.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.UserEmail;

        // Assert
        result.ShouldBe(email);
    }

    [Fact]
    public void UserEmail_WhenEmailClaimIsEmpty_ReturnsEmptyString()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, string.Empty)
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.UserEmail;

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void UserEmail_WhenMultipleEmailClaims_ReturnsFirstOne()
    {
        // Arrange
        var firstEmail = "first@example.com";
        var secondEmail = "second@example.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, firstEmail),
            new(ClaimTypes.Email, secondEmail)
        };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.UserEmail;

        // Assert
        result.ShouldBe(firstEmail);
    }

    #endregion

    #region IsAuthenticated Tests

    [Fact]
    public void IsAuthenticated_WhenHttpContextIsNull_ReturnsFalse()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _currentUserService.IsAuthenticated;

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsAuthenticated_WhenUserIsNull_ReturnsFalse()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns((ClaimsPrincipal?)null!);

        // Act
        var result = _currentUserService.IsAuthenticated;

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsAuthenticated_WhenIdentityIsNull_ReturnsFalse()
    {
        // Arrange
        var user = new ClaimsPrincipal();
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.IsAuthenticated;

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsAuthenticated_WhenUserIsAuthenticated_ReturnsTrue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.IsAuthenticated;

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsAuthenticated_WhenUserIsNotAuthenticated_ReturnsFalse()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };
        var identity = new ClaimsIdentity(claims); // No authentication type
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act
        var result = _currentUserService.IsAuthenticated;

        // Assert
        result.ShouldBeFalse();
    }

    #endregion



    #region Integration Tests

    [Fact]
    public void CurrentUserService_WithCompleteUserClaims_ReturnsAllPropertiesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act & Assert
        _currentUserService.UserId.ShouldBe(userId);
        _currentUserService.UserEmail.ShouldBe(email);
        _currentUserService.IsAuthenticated.ShouldBeTrue();
    }

    [Fact]
    public void CurrentUserService_WithPartialUserClaims_ReturnsCorrectValues()
    {
        // Arrange - Only NameIdentifier claim, no email
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act & Assert
        _currentUserService.UserId.ShouldBe(userId);
        _currentUserService.UserEmail.ShouldBeNull();
        _currentUserService.IsAuthenticated.ShouldBeTrue();
    }

    [Fact]
    public void CurrentUserService_WithAnonymousUser_ReturnsCorrectValues()
    {
        // Arrange
        var identity = new ClaimsIdentity(); // Anonymous identity
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act & Assert
        _currentUserService.UserId.ShouldBeNull();
        _currentUserService.UserEmail.ShouldBeNull();
        _currentUserService.IsAuthenticated.ShouldBeFalse();
    }

    [Fact]
    public void CurrentUserService_PropertiesAreConsistent_AcrossMultipleCalls()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
        _mockHttpContext.Setup(x => x.User).Returns(user);

        // Act - Call properties multiple times
        var userId1 = _currentUserService.UserId;
        var userId2 = _currentUserService.UserId;
        var email1 = _currentUserService.UserEmail;
        var email2 = _currentUserService.UserEmail;
        var auth1 = _currentUserService.IsAuthenticated;
        var auth2 = _currentUserService.IsAuthenticated;

        // Assert - All calls should return the same values
        userId1.ShouldBe(userId2);
        email1.ShouldBe(email2);
        auth1.ShouldBe(auth2);
        
        userId1.ShouldBe(userId);
        email1.ShouldBe(email);
        auth1.ShouldBeTrue();
    }

    #endregion
} 