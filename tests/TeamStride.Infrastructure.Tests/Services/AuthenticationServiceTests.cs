using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using TeamStride.Application.Authentication.Dtos;
using TeamStride.Application.Authentication.Services;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;
using TeamStride.Infrastructure.Email;
using TeamStride.Infrastructure.Identity;

namespace TeamStride.Infrastructure.Tests.Services;

public class AuthenticationServiceTests : BaseIntegrationTest
{
    private readonly AuthenticationService _authenticationService;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILogger<AuthenticationService>> _mockLogger;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IExternalAuthService> _mockExternalAuthService;
    private readonly Guid _testTeamId;
    private readonly Guid _testUserId;

    public AuthenticationServiceTests()
    {
        _testTeamId = Guid.NewGuid();
        _testUserId = Guid.NewGuid();

        // Setup team and user service mocks
        MockCurrentTeamService.Setup(x => x.TeamId).Returns(_testTeamId);
        MockCurrentUserService.Setup(x => x.UserId).Returns(_testUserId);

        // Create mocks for dependencies
        _mockUserManager = CreateMockUserManager();
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<AuthenticationService>>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockExternalAuthService = new Mock<IExternalAuthService>();

        // Setup default mock behaviors - Fix the JWT token service mock to use correct signature
        _mockJwtTokenService.Setup(x => x.GenerateJwtTokenAsync(It.IsAny<ApplicationUser>(), It.IsAny<List<TeamMembershipDto>>()))
            .ReturnsAsync("mock-jwt-token");

        _mockEmailService.Setup(x => x.SendEmailConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockEmailService.Setup(x => x.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Setup default UserManager mocks to return empty roles (non-admin users)
        _mockUserManager.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string>());

        // Create the service
        _authenticationService = new AuthenticationService(
            _mockUserManager.Object,
            _mockJwtTokenService.Object,
            _mockEmailService.Object,
            DbContext,
            _mockLogger.Object,
            _mockHttpContextAccessor.Object,
            _mockConfiguration.Object,
            _mockExternalAuthService.Object);
    }

    private static Mock<UserManager<ApplicationUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => _authenticationService.LoginAsync(null!));
    }

    [Fact]
    public async Task LoginAsync_WhenEmailIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var request = new LoginRequestDto { Email = null!, Password = "password" };

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => _authenticationService.LoginAsync(request));
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var request = new LoginRequestDto { Email = "test@example.com", Password = null! };

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => _authenticationService.LoginAsync(request));
    }

    [Fact]
    public async Task LoginAsync_WhenUserNotFound_ThrowsAuthenticationException()
    {
        // Arrange
        var request = new LoginRequestDto { Email = "nonexistent@example.com", Password = "password" };
        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync((ApplicationUser?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<AuthenticationException>(() => _authenticationService.LoginAsync(request));
        exception.ErrorCode.ShouldBe(AuthenticationException.ErrorCodes.InvalidCredentials);
        exception.Message.ShouldBe("Invalid credentials");
    }

    [Fact]
    public async Task LoginAsync_WhenUserIsNotActive_ThrowsAuthenticationException()
    {
        // Arrange
        var user = await CreateTestUserAsync(isActive: false);
        var request = new LoginRequestDto { Email = user.Email!, Password = "password" };
        
        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync(user);

        // Act & Assert
        var exception = await Should.ThrowAsync<AuthenticationException>(() => _authenticationService.LoginAsync(request));
        exception.ErrorCode.ShouldBe(AuthenticationException.ErrorCodes.AccountLocked);
        exception.Message.ShouldBe("Account is locked");
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsIncorrect_ThrowsAuthenticationException()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var request = new LoginRequestDto { Email = user.Email!, Password = "wrongpassword" };
        
        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync(user);
        _mockUserManager.Setup(x => x.CheckPasswordAsync(user, request.Password)).ReturnsAsync(false);

        // Act & Assert
        var exception = await Should.ThrowAsync<AuthenticationException>(() => _authenticationService.LoginAsync(request));
        exception.ErrorCode.ShouldBe(AuthenticationException.ErrorCodes.InvalidCredentials);
        exception.Message.ShouldBe("Invalid credentials");
    }

    [Fact]
    public async Task LoginAsync_WhenEmailNotConfirmed_ThrowsAuthenticationException()
    {
        // Arrange
        var user = await CreateTestUserAsync(emailConfirmed: false);
        var request = new LoginRequestDto { Email = user.Email!, Password = "password" };
        
        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync(user);
        _mockUserManager.Setup(x => x.CheckPasswordAsync(user, request.Password)).ReturnsAsync(true);
        _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string>()); // Non-admin user

        // Act & Assert
        var exception = await Should.ThrowAsync<AuthenticationException>(() => _authenticationService.LoginAsync(request));
        exception.ErrorCode.ShouldBe(AuthenticationException.ErrorCodes.EmailNotConfirmed);
        exception.Message.ShouldBe("Email not confirmed");
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var userTeam = await CreateTestUserTeamAsync(user.Id, _testTeamId);
        var request = new LoginRequestDto { Email = user.Email!, Password = "password" };
        
        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync(user);
        _mockUserManager.Setup(x => x.CheckPasswordAsync(user, request.Password)).ReturnsAsync(true);
        _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string>()); // Non-admin user
        _mockUserManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _mockJwtTokenService.Setup(x => x.GenerateRefreshToken()).Returns("mock-refresh-token");

        // Act
        var result = await _authenticationService.LoginAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Token.ShouldBe("mock-jwt-token");
        result.Email.ShouldBe(user.Email);
        result.FirstName.ShouldBe(user.FirstName);
        result.LastName.ShouldBe(user.LastName);
        result.Teams.ShouldNotBeNull();
        result.Teams.Count.ShouldBe(1);
        result.Teams[0].TeamRole.ShouldBe(userTeam.Role);
        result.RequiresEmailConfirmation.ShouldBeFalse();
        result.RefreshToken.ShouldNotBeNullOrEmpty();

        // Verify refresh token was created in database
        var refreshToken = DbContext.RefreshTokens.FirstOrDefault(rt => rt.UserId == user.Id);
        refreshToken.ShouldNotBeNull();
    }

    #endregion

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => _authenticationService.RegisterAsync(null!));
    }

    [Fact]
    public async Task RegisterAsync_WhenTeamNotFound_ThrowsAuthenticationException()
    {
        // Arrange
        var nonExistentTeamId = Guid.NewGuid();
        var request = new RegisterRequestDto 
        { 
            Email = "test@example.com", 
            Password = "Password123!", 
            ConfirmPassword = "Password123!",
            FirstName = "Test", 
            LastName = "User",
            TeamId = nonExistentTeamId,
            Role = TeamRole.TeamMember
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<AuthenticationException>(() => _authenticationService.RegisterAsync(request));
        exception.ErrorCode.ShouldBe(AuthenticationException.ErrorCodes.TenantNotFound);
        exception.Message.ShouldBe("Invalid team");
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ThrowsAuthenticationException()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var existingUser = await CreateTestUserAsync();
        var request = new RegisterRequestDto 
        { 
            Email = existingUser.Email!, 
            Password = "Password123!", 
            ConfirmPassword = "Password123!",
            FirstName = "Test", 
            LastName = "User",
            TeamId = team.Id,
            Role = TeamRole.TeamMember
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync(existingUser);

        // Act & Assert
        var exception = await Should.ThrowAsync<AuthenticationException>(() => _authenticationService.RegisterAsync(request));
        exception.ErrorCode.ShouldBe(AuthenticationException.ErrorCodes.EmailAlreadyExists);
        exception.Message.ShouldBe("Email already registered");
    }

    [Fact]
    public async Task RegisterAsync_WhenUserCreationFails_ThrowsAuthenticationException()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var request = new RegisterRequestDto 
        { 
            Email = "test@example.com", 
            Password = "weak", 
            ConfirmPassword = "weak",
            FirstName = "Test", 
            LastName = "User",
            TeamId = team.Id,
            Role = TeamRole.TeamMember
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync((ApplicationUser?)null);
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));

        // Act & Assert
        var exception = await Should.ThrowAsync<AuthenticationException>(() => _authenticationService.RegisterAsync(request));
        exception.Message.ShouldContain("Registration failed");
        exception.Message.ShouldContain("Password too weak");
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_CreatesUserAndReturnsAuthResponse()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var request = new RegisterRequestDto 
        { 
            Email = "newuser@example.com", 
            Password = "Password123!", 
            ConfirmPassword = "Password123!",
            FirstName = "New", 
            LastName = "User",
            TeamId = team.Id,
            Role = TeamRole.TeamMember
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync((ApplicationUser?)null);
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((user, password) => 
            {
                user.Id = Guid.NewGuid();
                // Actually add the user to the database context
                DbContext.Users.Add(user);
                DbContext.SaveChanges();
            });
        _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync("confirmation-token");
        _mockJwtTokenService.Setup(x => x.GenerateRefreshToken()).Returns("mock-refresh-token");

        // Act
        var result = await _authenticationService.RegisterAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Token.ShouldBe("mock-jwt-token");
        result.Email.ShouldBe(request.Email);
        result.FirstName.ShouldBe(request.FirstName);
        result.LastName.ShouldBe(request.LastName);
        result.Teams.ShouldNotBeNull();
        result.Teams.Count.ShouldBe(1);
        result.Teams[0].TeamId.ShouldBe(team.Id);
        result.Teams[0].TeamRole.ShouldBe(request.Role);
        result.RequiresEmailConfirmation.ShouldBeTrue();

        // Verify UserTeam was created
        var userTeam = DbContext.UserTeams.FirstOrDefault(ut => ut.TeamId == team.Id);
        userTeam.ShouldNotBeNull();
        userTeam.Role.ShouldBe(request.Role);
        userTeam.IsActive.ShouldBeTrue();
        userTeam.IsDefault.ShouldBeTrue();

        // Verify email confirmation was sent
        _mockEmailService.Verify(x => x.SendEmailConfirmationAsync(request.Email, It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region RefreshTokenAsync Tests

    [Fact]
    public async Task RefreshTokenAsync_WhenTokenIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => _authenticationService.RefreshTokenAsync(null!));
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenTokenNotFound_ThrowsAuthenticationException()
    {
        // Arrange
        var invalidToken = "invalid-token";

        // Act & Assert
        var exception = await Should.ThrowAsync<AuthenticationException>(() => _authenticationService.RefreshTokenAsync(invalidToken));
        exception.ErrorCode.ShouldBe(AuthenticationException.ErrorCodes.InvalidToken);
        exception.Message.ShouldBe("Invalid refresh token");
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenTokenIsExpired_ThrowsAuthenticationException()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var expiredToken = await CreateTestRefreshTokenAsync(user.Id, _testTeamId, isExpired: true);

        // Act & Assert
        var exception = await Should.ThrowAsync<AuthenticationException>(() => _authenticationService.RefreshTokenAsync(expiredToken.Token));
        exception.ErrorCode.ShouldBe(AuthenticationException.ErrorCodes.InvalidToken);
        exception.Message.ShouldBe("Refresh token is expired or revoked");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ReturnsNewTokens()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var userTeam = await CreateTestUserTeamAsync(user.Id, _testTeamId);
        var refreshToken = await CreateTestRefreshTokenAsync(user.Id, _testTeamId);

        var mockHttpContext = new Mock<HttpContext>();
        var mockConnection = new Mock<ConnectionInfo>();
        mockConnection.Setup(x => x.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("127.0.0.1"));
        mockHttpContext.Setup(x => x.Connection).Returns(mockConnection.Object);
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
        _mockJwtTokenService.Setup(x => x.GenerateRefreshToken()).Returns("mock-refresh-token");

        // Act
        var result = await _authenticationService.RefreshTokenAsync(refreshToken.Token);

        // Assert
        result.ShouldNotBeNull();
        result.Token.ShouldBe("mock-jwt-token");
        result.Email.ShouldBe(user.Email);
        result.Teams.ShouldNotBeNull();
        result.Teams.Count.ShouldBe(1);
        result.Teams[0].TeamId.ShouldBe(_testTeamId);
        result.Teams[0].TeamRole.ShouldBe(userTeam.Role);

        // Verify old token was revoked
        var revokedToken = DbContext.RefreshTokens.First(rt => rt.Id == refreshToken.Id);
        revokedToken.RevokedOn.ShouldNotBeNull();
        revokedToken.ReplacedByToken.ShouldNotBeNullOrEmpty();
    }

    #endregion

    #region ConfirmEmailAsync Tests

    [Fact]
    public async Task ConfirmEmailAsync_WhenUserNotFound_ThrowsAuthenticationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = "confirmation-token";

        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync((ApplicationUser?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<AuthenticationException>(() => _authenticationService.ConfirmEmailAsync(userId, token));
        exception.ErrorCode.ShouldBe(AuthenticationException.ErrorCodes.UserNotFound);
        exception.Message.ShouldBe("User not found");
    }

    [Fact]
    public async Task ConfirmEmailAsync_WhenConfirmationFails_ThrowsAuthenticationException()
    {
        // Arrange
        var user = await CreateTestUserAsync(emailConfirmed: false);
        var token = "invalid-token";

        _mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _mockUserManager.Setup(x => x.ConfirmEmailAsync(user, token))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token" }));

        // Act & Assert
        var exception = await Should.ThrowAsync<AuthenticationException>(() => _authenticationService.ConfirmEmailAsync(user.Id, token));
        exception.Message.ShouldContain("Email confirmation failed");
        exception.Message.ShouldContain("Invalid token");
    }

    [Fact]
    public async Task ConfirmEmailAsync_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var user = await CreateTestUserAsync(emailConfirmed: false);
        var token = "valid-token";

        _mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _mockUserManager.Setup(x => x.ConfirmEmailAsync(user, token)).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _authenticationService.ConfirmEmailAsync(user.Id, token);

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region SendPasswordResetEmailAsync Tests

    [Fact]
    public async Task SendPasswordResetEmailAsync_WhenUserNotFound_ThrowsAuthenticationException()
    {
        // Arrange
        var email = "nonexistent@example.com";

        _mockUserManager.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync((ApplicationUser?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<AuthenticationException>(() => _authenticationService.SendPasswordResetEmailAsync(email));
        exception.ErrorCode.ShouldBe(AuthenticationException.ErrorCodes.UserNotFound);
        exception.Message.ShouldBe("User not found");
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithValidEmail_SendsEmailAndReturnsTrue()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var resetToken = "reset-token";

        _mockUserManager.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _mockUserManager.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync(resetToken);

        // Act
        var result = await _authenticationService.SendPasswordResetEmailAsync(user.Email!);

        // Assert
        result.ShouldBeTrue();
        _mockEmailService.Verify(x => x.SendPasswordResetAsync(user.Email!, It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region ResetPasswordAsync Tests

    [Fact]
    public async Task ResetPasswordAsync_WhenUserNotFound_ThrowsAuthenticationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = "reset-token";
        var newPassword = "NewPassword123!";

        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync((ApplicationUser?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<AuthenticationException>(() => _authenticationService.ResetPasswordAsync(userId, token, newPassword));
        exception.ErrorCode.ShouldBe(AuthenticationException.ErrorCodes.UserNotFound);
        exception.Message.ShouldBe("User not found");
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenResetFails_ThrowsAuthenticationException()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var token = "invalid-token";
        var newPassword = "NewPassword123!";

        _mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _mockUserManager.Setup(x => x.ResetPasswordAsync(user, token, newPassword))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token" }));

        // Act & Assert
        var exception = await Should.ThrowAsync<AuthenticationException>(() => _authenticationService.ResetPasswordAsync(user.Id, token, newPassword));
        exception.Message.ShouldContain("Password reset failed");
        exception.Message.ShouldContain("Invalid token");
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidData_ResetsPasswordAndRevokesTokens()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var refreshToken = await CreateTestRefreshTokenAsync(user.Id, _testTeamId);
        var token = "valid-token";
        var newPassword = "NewPassword123!";

        var mockHttpContext = new Mock<HttpContext>();
        var mockConnection = new Mock<ConnectionInfo>();
        mockConnection.Setup(x => x.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("127.0.0.1"));
        mockHttpContext.Setup(x => x.Connection).Returns(mockConnection.Object);
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        _mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _mockUserManager.Setup(x => x.ResetPasswordAsync(user, token, newPassword)).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _authenticationService.ResetPasswordAsync(user.Id, token, newPassword);

        // Assert
        result.ShouldBeTrue();

        // Verify refresh tokens were revoked
        var revokedToken = DbContext.RefreshTokens.First(rt => rt.Id == refreshToken.Id);
        revokedToken.RevokedOn.ShouldNotBeNull();
        revokedToken.ReasonRevoked.ShouldBe("Password reset");
    }

    #endregion

    #region ChangePasswordAsync Tests

    [Fact]
    public async Task ChangePasswordAsync_WhenUserNotFound_ThrowsAuthenticationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentPassword = "CurrentPassword123!";
        var newPassword = "NewPassword123!";

        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync((ApplicationUser?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<AuthenticationException>(() => _authenticationService.ChangePasswordAsync(userId, currentPassword, newPassword));
        exception.ErrorCode.ShouldBe(AuthenticationException.ErrorCodes.UserNotFound);
        exception.Message.ShouldBe("User not found");
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenChangeFails_ThrowsAuthenticationException()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var currentPassword = "WrongPassword123!";
        var newPassword = "NewPassword123!";

        _mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _mockUserManager.Setup(x => x.ChangePasswordAsync(user, currentPassword, newPassword))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Incorrect password" }));

        // Act & Assert
        var exception = await Should.ThrowAsync<AuthenticationException>(() => _authenticationService.ChangePasswordAsync(user.Id, currentPassword, newPassword));
        exception.Message.ShouldContain("Password change failed");
        exception.Message.ShouldContain("Incorrect password");
    }

    [Fact]
    public async Task ChangePasswordAsync_WithValidData_ChangesPasswordAndRevokesTokens()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var refreshToken = await CreateTestRefreshTokenAsync(user.Id, _testTeamId);
        var currentPassword = "CurrentPassword123!";
        var newPassword = "NewPassword123!";

        var mockHttpContext = new Mock<HttpContext>();
        var mockConnection = new Mock<ConnectionInfo>();
        mockConnection.Setup(x => x.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("127.0.0.1"));
        mockHttpContext.Setup(x => x.Connection).Returns(mockConnection.Object);
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        _mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        _mockUserManager.Setup(x => x.ChangePasswordAsync(user, currentPassword, newPassword)).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _authenticationService.ChangePasswordAsync(user.Id, currentPassword, newPassword);

        // Assert
        result.ShouldBeTrue();

        // Verify refresh tokens were revoked
        var revokedToken = DbContext.RefreshTokens.First(rt => rt.Id == refreshToken.Id);
        revokedToken.RevokedOn.ShouldNotBeNull();
        revokedToken.ReasonRevoked.ShouldBe("Password changed");
    }

    #endregion

    #region LogoutAsync Tests

    [Fact]
    public async Task LogoutAsync_RevokesAllActiveTokensForUser()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var refreshToken1 = await CreateTestRefreshTokenAsync(user.Id, _testTeamId);
        var refreshToken2 = await CreateTestRefreshTokenAsync(user.Id, _testTeamId);

        var mockHttpContext = new Mock<HttpContext>();
        var mockConnection = new Mock<ConnectionInfo>();
        mockConnection.Setup(x => x.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("127.0.0.1"));
        mockHttpContext.Setup(x => x.Connection).Returns(mockConnection.Object);
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        // Act
        var result = await _authenticationService.LogoutAsync(user.Id);

        // Assert
        result.ShouldBeTrue();

        // Verify all refresh tokens were revoked
        var revokedTokens = DbContext.RefreshTokens.Where(rt => rt.UserId == user.Id).ToList();
        revokedTokens.ShouldAllBe(rt => rt.RevokedOn != null);
        revokedTokens.ShouldAllBe(rt => rt.ReasonRevoked == "Logged out");
    }

    #endregion

    #region ExternalLoginAsync Tests

    [Fact]
    public async Task ExternalLoginAsync_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => _authenticationService.ExternalLoginAsync(null!));
    }

    [Fact]
    public async Task ExternalLoginAsync_WhenProviderIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var request = new ExternalAuthRequestDto { Provider = null!, AccessToken = "token", TeamId = _testTeamId };

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => _authenticationService.ExternalLoginAsync(request));
    }

    [Fact]
    public async Task ExternalLoginAsync_WhenExternalUserInfoIsNull_ThrowsAuthenticationException()
    {
        // Arrange
        var request = new ExternalAuthRequestDto { Provider = "Google", AccessToken = "invalid-token", TeamId = _testTeamId };
        
        _mockExternalAuthService.Setup(x => x.GetUserInfoAsync(request.Provider, request.AccessToken))
            .ReturnsAsync((ExternalUserInfo?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<AuthenticationException>(() => _authenticationService.ExternalLoginAsync(request));
        exception.ErrorCode.ShouldBe(AuthenticationException.ErrorCodes.ExternalAuthError);
        exception.Message.ShouldBe("Failed to get external user info");
    }

    [Fact]
    public async Task ExternalLoginAsync_WithNewUser_CreatesUserAndReturnsAuthResponse()
    {
        // Arrange
        var team = await CreateTestTeamAsync(); // Ensure team exists
        var request = new ExternalAuthRequestDto { Provider = "Google", AccessToken = "valid-token", TeamId = team.Id };
        var externalUserInfo = new ExternalUserInfo
        {
            Email = "external@example.com",
            FirstName = "External",
            LastName = "User",
            ProviderId = "google-123"
        };

        _mockExternalAuthService.Setup(x => x.GetUserInfoAsync(request.Provider, request.AccessToken))
            .ReturnsAsync(externalUserInfo);
        _mockUserManager.Setup(x => x.FindByEmailAsync(externalUserInfo.Email)).ReturnsAsync((ApplicationUser?)null);
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser>(user => 
            {
                user.Id = Guid.NewGuid();
                // Actually add the user to the database context
                DbContext.Users.Add(user);
                DbContext.SaveChanges();
            });
        _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        _mockJwtTokenService.Setup(x => x.GenerateRefreshToken()).Returns("mock-refresh-token");

        // Act
        var result = await _authenticationService.ExternalLoginAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Token.ShouldBe("mock-jwt-token");
        result.Email.ShouldBe(externalUserInfo.Email);
        result.FirstName.ShouldBe(externalUserInfo.FirstName);
        result.LastName.ShouldBe(externalUserInfo.LastName);
        result.Teams.ShouldNotBeNull();
        result.Teams.Count.ShouldBe(1);
        result.Teams[0].TeamId.ShouldBe(team.Id);
        result.Teams[0].TeamRole.ShouldBe(TeamRole.TeamMember); // Default role for external users

        // Verify UserTeam was created
        var userTeam = DbContext.UserTeams.FirstOrDefault(ut => ut.TeamId == team.Id);
        userTeam.ShouldNotBeNull();
        userTeam.Role.ShouldBe(TeamRole.TeamMember);
    }

    [Fact]
    public async Task ExternalLoginAsync_WithExistingUser_ReturnsAuthResponse()
    {
        // Arrange
        var team = await CreateTestTeamAsync(); // Ensure team exists
        var existingUser = await CreateTestUserAsync(email: "existing@example.com");
        var userTeam = await CreateTestUserTeamAsync(existingUser.Id, team.Id, TeamRole.TeamMember);
        var request = new ExternalAuthRequestDto { Provider = "Google", AccessToken = "valid-token", TeamId = team.Id };
        var externalUserInfo = new ExternalUserInfo
        {
            Email = existingUser.Email!,
            FirstName = "Updated",
            LastName = "Name",
            ProviderId = "google-456"
        };

        _mockExternalAuthService.Setup(x => x.GetUserInfoAsync(request.Provider, request.AccessToken))
            .ReturnsAsync(externalUserInfo);
        _mockUserManager.Setup(x => x.FindByEmailAsync(externalUserInfo.Email)).ReturnsAsync(existingUser);
        _mockUserManager.Setup(x => x.UpdateAsync(existingUser)).ReturnsAsync(IdentityResult.Success);
        _mockJwtTokenService.Setup(x => x.GenerateRefreshToken()).Returns("mock-refresh-token");

        // Act
        var result = await _authenticationService.ExternalLoginAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Email.ShouldBe(existingUser.Email);
        result.Teams.ShouldNotBeNull();
        result.Teams.Count.ShouldBe(1);
        result.Teams[0].TeamId.ShouldBe(team.Id);
        result.Teams[0].TeamRole.ShouldBe(TeamRole.TeamMember); // Existing user's role
    }

    #endregion

    #region Multi-Team Authentication Tests

    [Fact]
    public async Task LoginAsync_WithMultipleTeams_ReturnsAllTeamsInResponse()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var team1 = await CreateTestTeamAsync("Team 1");
        var team2 = await CreateTestTeamAsync("Team 2");
        var team3 = await CreateTestTeamAsync("Team 3");
        
        var userTeam1 = await CreateTestUserTeamAsync(user.Id, team1.Id, TeamRole.TeamOwner);
        var userTeam2 = await CreateTestUserTeamAsync(user.Id, team2.Id, TeamRole.TeamAdmin);
        var userTeam3 = await CreateTestUserTeamAsync(user.Id, team3.Id, TeamRole.TeamMember);
        
        // Override the default team ID with the first team
        userTeam1.IsDefault = true;
        userTeam2.IsDefault = false;
        userTeam3.IsDefault = false;
        await DbContext.SaveChangesAsync();
        
        var request = new LoginRequestDto { Email = user.Email!, Password = "password" };
        
        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync(user);
        _mockUserManager.Setup(x => x.CheckPasswordAsync(user, request.Password)).ReturnsAsync(true);
        _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string>()); // Non-admin user
        _mockUserManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _mockJwtTokenService.Setup(x => x.GenerateRefreshToken()).Returns("mock-refresh-token");

        // Act
        var result = await _authenticationService.LoginAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Teams.ShouldNotBeNull();
        result.Teams.Count.ShouldBe(3);
        
        // Verify all teams are included with correct roles
        var ownerTeam = result.Teams.FirstOrDefault(t => t.TeamId == team1.Id);
        ownerTeam.ShouldNotBeNull();
        ownerTeam.TeamRole.ShouldBe(TeamRole.TeamOwner);
        
        var adminTeam = result.Teams.FirstOrDefault(t => t.TeamId == team2.Id);
        adminTeam.ShouldNotBeNull();
        adminTeam.TeamRole.ShouldBe(TeamRole.TeamAdmin);
        
        var memberTeam = result.Teams.FirstOrDefault(t => t.TeamId == team3.Id);
        memberTeam.ShouldNotBeNull();
        memberTeam.TeamRole.ShouldBe(TeamRole.TeamMember);
    }

    [Fact]
    public async Task RegisterAsync_WithMultipleTeamsInDatabase_OnlyIncludesRegisteredTeam()
    {
        // Arrange
        var existingTeam = await CreateTestTeamAsync("Existing Team");
        var registrationTeam = await CreateTestTeamAsync("Registration Team");
        
        var request = new RegisterRequestDto 
        { 
            Email = "newuser@example.com", 
            Password = "Password123!", 
            ConfirmPassword = "Password123!",
            FirstName = "New", 
            LastName = "User",
            TeamId = registrationTeam.Id,
            Role = TeamRole.TeamMember
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync((ApplicationUser?)null);
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((user, password) => 
            {
                user.Id = Guid.NewGuid();
                DbContext.Users.Add(user);
                DbContext.SaveChanges();
            });
        _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync("confirmation-token");
        _mockJwtTokenService.Setup(x => x.GenerateRefreshToken()).Returns("mock-refresh-token");

        // Act
        var result = await _authenticationService.RegisterAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Teams.ShouldNotBeNull();
        result.Teams.Count.ShouldBe(1);
        result.Teams[0].TeamId.ShouldBe(registrationTeam.Id);
        result.Teams[0].TeamRole.ShouldBe(TeamRole.TeamMember);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithMultipleTeams_ReturnsAllTeamsInResponse()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var team1 = await CreateTestTeamAsync("Team 1");
        var team2 = await CreateTestTeamAsync("Team 2");
        
        await CreateTestUserTeamAsync(user.Id, team1.Id, TeamRole.TeamOwner);
        await CreateTestUserTeamAsync(user.Id, team2.Id, TeamRole.TeamMember);
        
        var refreshToken = await CreateTestRefreshTokenAsync(user.Id, team1.Id);

        var mockHttpContext = new Mock<HttpContext>();
        var mockConnection = new Mock<ConnectionInfo>();
        mockConnection.Setup(x => x.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("127.0.0.1"));
        mockHttpContext.Setup(x => x.Connection).Returns(mockConnection.Object);
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
        _mockJwtTokenService.Setup(x => x.GenerateRefreshToken()).Returns("mock-refresh-token");

        // Act
        var result = await _authenticationService.RefreshTokenAsync(refreshToken.Token);

        // Assert
        result.ShouldNotBeNull();
        result.Teams.ShouldNotBeNull();
        result.Teams.Count.ShouldBe(2);
        
        // Verify both teams are included
        result.Teams.Any(t => t.TeamId == team1.Id && t.TeamRole == TeamRole.TeamOwner).ShouldBeTrue();
        result.Teams.Any(t => t.TeamId == team2.Id && t.TeamRole == TeamRole.TeamMember).ShouldBeTrue();
    }

    [Fact]
    public async Task ExternalLoginAsync_WithMultipleTeamsForExistingUser_ReturnsAllTeamsInResponse()
    {
        // Arrange
        var team1 = await CreateTestTeamAsync("Team 1");
        var team2 = await CreateTestTeamAsync("Team 2");
        var existingUser = await CreateTestUserAsync(email: "existing@example.com");
        
        await CreateTestUserTeamAsync(existingUser.Id, team1.Id, TeamRole.TeamAdmin);
        await CreateTestUserTeamAsync(existingUser.Id, team2.Id, TeamRole.TeamMember);
        
        var request = new ExternalAuthRequestDto { Provider = "Google", AccessToken = "valid-token", TeamId = team1.Id };
        var externalUserInfo = new ExternalUserInfo
        {
            Email = existingUser.Email!,
            FirstName = "Updated",
            LastName = "Name",
            ProviderId = "google-456"
        };

        _mockExternalAuthService.Setup(x => x.GetUserInfoAsync(request.Provider, request.AccessToken))
            .ReturnsAsync(externalUserInfo);
        _mockUserManager.Setup(x => x.FindByEmailAsync(externalUserInfo.Email)).ReturnsAsync(existingUser);
        _mockUserManager.Setup(x => x.UpdateAsync(existingUser)).ReturnsAsync(IdentityResult.Success);
        _mockJwtTokenService.Setup(x => x.GenerateRefreshToken()).Returns("mock-refresh-token");

        // Act
        var result = await _authenticationService.ExternalLoginAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Teams.ShouldNotBeNull();
        result.Teams.Count.ShouldBe(2);
        
        // Verify both teams are included with correct roles
        result.Teams.Any(t => t.TeamId == team1.Id && t.TeamRole == TeamRole.TeamAdmin).ShouldBeTrue();
        result.Teams.Any(t => t.TeamId == team2.Id && t.TeamRole == TeamRole.TeamMember).ShouldBeTrue();
    }

    #endregion

    #region AuthResponseDto Structure Tests

    [Fact]
    public async Task LoginAsync_AuthResponseDto_HasCorrectStructure()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var userTeam = await CreateTestUserTeamAsync(user.Id, _testTeamId);
        var request = new LoginRequestDto { Email = user.Email!, Password = "password" };
        
        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync(user);
        _mockUserManager.Setup(x => x.CheckPasswordAsync(user, request.Password)).ReturnsAsync(true);
        _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string>()); // Non-admin user
        _mockUserManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _mockJwtTokenService.Setup(x => x.GenerateRefreshToken()).Returns("mock-refresh-token");

        // Act
        var result = await _authenticationService.LoginAsync(request);

        // Assert - Verify all required properties exist
        result.ShouldNotBeNull();
        result.Token.ShouldNotBeNullOrEmpty();
        result.RefreshToken.ShouldNotBeNullOrEmpty();
        result.Email.ShouldBe(user.Email);
        result.FirstName.ShouldBe(user.FirstName);
        result.LastName.ShouldBe(user.LastName);
        result.RequiresEmailConfirmation.ShouldBeFalse();
        result.Teams.ShouldNotBeNull();
        result.Teams.Count.ShouldBe(1);
        
        // Verify team structure
        var team = result.Teams[0];
        team.TeamId.ShouldBe(_testTeamId);
        team.TeamRole.ShouldBe(userTeam.Role);
        team.MemberType.ShouldBe(MemberType.Coach); // Default member type for tests
        
        // Verify deprecated properties are not accessed (they don't exist)
        // This test ensures we're not trying to access Role or TeamId properties that don't exist
        var type = result.GetType();
        type.GetProperty("Role").ShouldBeNull();
        type.GetProperty("TeamId").ShouldBeNull();
    }

    [Fact]
    public async Task RegisterAsync_AuthResponseDto_HasCorrectStructure()
    {
        // Arrange
        var team = await CreateTestTeamAsync();
        var request = new RegisterRequestDto 
        { 
            Email = "newuser@example.com", 
            Password = "Password123!", 
            ConfirmPassword = "Password123!",
            FirstName = "New", 
            LastName = "User",
            TeamId = team.Id,
            Role = TeamRole.TeamMember
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync((ApplicationUser?)null);
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((user, password) => 
            {
                user.Id = Guid.NewGuid();
                DbContext.Users.Add(user);
                DbContext.SaveChanges();
            });
        _mockUserManager.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync("confirmation-token");
        _mockJwtTokenService.Setup(x => x.GenerateRefreshToken()).Returns("mock-refresh-token");

        // Act
        var result = await _authenticationService.RegisterAsync(request);

        // Assert - Verify all required properties exist
        result.ShouldNotBeNull();
        result.Token.ShouldNotBeNullOrEmpty();
        result.RefreshToken.ShouldNotBeNullOrEmpty();
        result.Email.ShouldBe(request.Email);
        result.FirstName.ShouldBe(request.FirstName);
        result.LastName.ShouldBe(request.LastName);
        result.RequiresEmailConfirmation.ShouldBeTrue();
        result.Teams.ShouldNotBeNull();
        result.Teams.Count.ShouldBe(1);
        
        // Verify team structure
        var teamResult = result.Teams[0];
        teamResult.TeamId.ShouldBe(team.Id);
        teamResult.TeamRole.ShouldBe(request.Role);
        teamResult.MemberType.ShouldBe(MemberType.Coach); // Default member type for registration
    }

    #endregion

    #region Team Context Validation Tests

    [Fact]
    public async Task LoginAsync_WithSuspendedTeam_IncludesAllTeamsRegardlessOfStatus()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var activeTeam = await CreateTestTeamAsync("Active Team");
        var suspendedTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Suspended Team",
            CreatedOn = DateTime.UtcNow,
            Status = TeamStatus.Suspended, // Suspended team
            Tier = TeamTier.Free
        };
        DbContext.Teams.Add(suspendedTeam);
        await DbContext.SaveChangesAsync();
        
        await CreateTestUserTeamAsync(user.Id, activeTeam.Id, TeamRole.TeamMember);
        await CreateTestUserTeamAsync(user.Id, suspendedTeam.Id, TeamRole.TeamOwner); // Owner should access suspended team
        
        var request = new LoginRequestDto { Email = user.Email!, Password = "password" };
        
        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync(user);
        _mockUserManager.Setup(x => x.CheckPasswordAsync(user, request.Password)).ReturnsAsync(true);
        _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string>());
        _mockUserManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _mockJwtTokenService.Setup(x => x.GenerateRefreshToken()).Returns("mock-refresh-token");

        // Act
        var result = await _authenticationService.LoginAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Teams.ShouldNotBeNull();
        // Authentication should include all teams where user has active membership,
        // regardless of team status. Team owners need access to suspended teams to reactivate them.
        result.Teams.Count.ShouldBe(2);
        result.Teams.Any(t => t.TeamId == activeTeam.Id).ShouldBeTrue();
        result.Teams.Any(t => t.TeamId == suspendedTeam.Id).ShouldBeTrue();
        
        // Verify the suspended team owner has correct role
        var suspendedTeamMembership = result.Teams.First(t => t.TeamId == suspendedTeam.Id);
        suspendedTeamMembership.TeamRole.ShouldBe(TeamRole.TeamOwner);
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUserTeam_ShouldExcludeInactiveUserTeam()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var team1 = await CreateTestTeamAsync("Team 1");
        var team2 = await CreateTestTeamAsync("Team 2");
        
        var activeUserTeam = await CreateTestUserTeamAsync(user.Id, team1.Id, TeamRole.TeamMember);
        var inactiveUserTeam = await CreateTestUserTeamAsync(user.Id, team2.Id, TeamRole.TeamMember);
        
        // Make the second user team inactive
        inactiveUserTeam.IsActive = false;
        await DbContext.SaveChangesAsync();
        
        var request = new LoginRequestDto { Email = user.Email!, Password = "password" };
        
        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync(user);
        _mockUserManager.Setup(x => x.CheckPasswordAsync(user, request.Password)).ReturnsAsync(true);
        _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string>());
        _mockUserManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _mockJwtTokenService.Setup(x => x.GenerateRefreshToken()).Returns("mock-refresh-token");

        // Act
        var result = await _authenticationService.LoginAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Teams.ShouldNotBeNull();
        result.Teams.Count.ShouldBe(1);
        result.Teams[0].TeamId.ShouldBe(team1.Id);
        
        // Verify inactive user team is not included
        result.Teams.Any(t => t.TeamId == team2.Id).ShouldBeFalse();
    }

    #endregion

    #region Helper Methods

    private async Task<ApplicationUser> CreateTestUserAsync(
        string email = "test@example.com",
        string firstName = "Test",
        string lastName = "User",
        bool isActive = true,
        bool emailConfirmed = true)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            IsActive = isActive,
            EmailConfirmed = emailConfirmed,
            DefaultTeamId = _testTeamId,
            CreatedOn = DateTime.UtcNow
        };

        // Add the user to the context and save to ensure it exists in the database
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();
        return user;
    }

    private async Task<Team> CreateTestTeamAsync(string name = "Test Team")
    {
        var team = new Team
        {
            Id = name == "Test Team" ? _testTeamId : Guid.NewGuid(), // Use _testTeamId for default, unique IDs for others
            Name = name,
            CreatedOn = DateTime.UtcNow,
            Status = TeamStatus.Active,
            Tier = TeamTier.Free
        };

        DbContext.Teams.Add(team);
        await DbContext.SaveChangesAsync();
        return team;
    }

    private async Task<UserTeam> CreateTestUserTeamAsync(
        Guid userId, 
        Guid teamId, 
        TeamRole role = TeamRole.TeamMember)
    {
        // Ensure the team exists
        var team = await DbContext.Teams.FindAsync(teamId);
        if (team == null)
        {
            await CreateTestTeamAsync();
        }

        var userTeam = new UserTeam
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TeamId = teamId,
            Role = role,
            IsActive = true,
            IsDefault = true,
            JoinedOn = DateTime.UtcNow,
            CreatedOn = DateTime.UtcNow
        };

        DbContext.UserTeams.Add(userTeam);
        await DbContext.SaveChangesAsync();
        return userTeam;
    }

    private async Task<RefreshToken> CreateTestRefreshTokenAsync(
        Guid userId, 
        Guid? teamId, 
        bool isExpired = false)
    {
        // Ensure the user exists in the database
        var existingUser = await DbContext.Users.FindAsync(userId);
        if (existingUser == null)
        {
            existingUser = await CreateTestUserAsync();
            userId = existingUser.Id; // Use the created user's ID
        }

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = Guid.NewGuid().ToString(),
            UserId = userId,
            TeamId = teamId,
            ExpiresOn = isExpired ? DateTime.UtcNow.AddDays(-1) : DateTime.UtcNow.AddDays(7),
            CreatedOn = DateTime.UtcNow,
            CreatedByIp = "127.0.0.1"
        };

        if (isExpired)
        {
            refreshToken.RevokedOn = DateTime.UtcNow.AddDays(-1);
            refreshToken.ReasonRevoked = "Expired";
        }

        DbContext.RefreshTokens.Add(refreshToken);
        await DbContext.SaveChangesAsync();
        return refreshToken;
    }

    #endregion
} 