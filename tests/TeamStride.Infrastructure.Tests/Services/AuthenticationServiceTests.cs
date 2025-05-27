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
        MockTeamService.Setup(x => x.TeamId).Returns(_testTeamId);
        MockCurrentUserService.Setup(x => x.UserId).Returns(_testUserId);

        // Create mocks for dependencies
        _mockUserManager = CreateMockUserManager();
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<AuthenticationService>>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockExternalAuthService = new Mock<IExternalAuthService>();

        // Setup default mock behaviors
        _mockJwtTokenService.Setup(x => x.GenerateJwtTokenAsync(It.IsAny<ApplicationUser>(), It.IsAny<Guid?>(), It.IsAny<TeamRole?>(), It.IsAny<MemberType?>()))
            .ReturnsAsync("mock-jwt-token");

        _mockEmailService.Setup(x => x.SendEmailConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockEmailService.Setup(x => x.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

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
        var request = new LoginRequestDto { Email = null!, Password = "password", TeamId = _testTeamId };

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => _authenticationService.LoginAsync(request));
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var request = new LoginRequestDto { Email = "test@example.com", Password = null!, TeamId = _testTeamId };

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => _authenticationService.LoginAsync(request));
    }

    [Fact]
    public async Task LoginAsync_WhenTeamIdIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var request = new LoginRequestDto { Email = "test@example.com", Password = "password", TeamId = null! };

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => _authenticationService.LoginAsync(request));
    }

    [Fact]
    public async Task LoginAsync_WhenUserNotFound_ThrowsAuthenticationException()
    {
        // Arrange
        var request = new LoginRequestDto { Email = "nonexistent@example.com", Password = "password", TeamId = _testTeamId };
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
        var request = new LoginRequestDto { Email = user.Email!, Password = "password", TeamId = _testTeamId };
        
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
        var request = new LoginRequestDto { Email = user.Email!, Password = "wrongpassword", TeamId = _testTeamId };
        
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
        var request = new LoginRequestDto { Email = user.Email!, Password = "password", TeamId = _testTeamId };
        
        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync(user);
        _mockUserManager.Setup(x => x.CheckPasswordAsync(user, request.Password)).ReturnsAsync(true);

        // Act & Assert
        var exception = await Should.ThrowAsync<AuthenticationException>(() => _authenticationService.LoginAsync(request));
        exception.ErrorCode.ShouldBe(AuthenticationException.ErrorCodes.EmailNotConfirmed);
        exception.Message.ShouldBe("Email not confirmed");
    }

    [Fact]
    public async Task LoginAsync_WhenUserTeamNotFound_ThrowsAuthenticationException()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var differentTeamId = Guid.NewGuid();
        var request = new LoginRequestDto { Email = user.Email!, Password = "password", TeamId = differentTeamId };
        
        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync(user);
        _mockUserManager.Setup(x => x.CheckPasswordAsync(user, request.Password)).ReturnsAsync(true);

        // Act & Assert
        var exception = await Should.ThrowAsync<AuthenticationException>(() => _authenticationService.LoginAsync(request));
        exception.ErrorCode.ShouldBe(AuthenticationException.ErrorCodes.TenantNotFound);
        exception.Message.ShouldBe("Invalid team");
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var userTeam = await CreateTestUserTeamAsync(user.Id, _testTeamId);
        var request = new LoginRequestDto { Email = user.Email!, Password = "password", TeamId = _testTeamId };
        
        _mockUserManager.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync(user);
        _mockUserManager.Setup(x => x.CheckPasswordAsync(user, request.Password)).ReturnsAsync(true);
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
        result.TeamId.ShouldBe(_testTeamId);
        result.Role.ShouldBe(userTeam.Role);
        result.RequiresEmailConfirmation.ShouldBeFalse();
        result.RefreshToken.ShouldNotBeNullOrEmpty();

        // Verify refresh token was created in database
        var refreshToken = DbContext.RefreshTokens.FirstOrDefault(rt => rt.UserId == user.Id);
        refreshToken.ShouldNotBeNull();
        refreshToken.TeamId.ShouldBe(_testTeamId);
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
        result.TeamId.ShouldBe(team.Id);
        result.Role.ShouldBe(request.Role);
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
        result.TeamId.ShouldBe(_testTeamId);
        result.Role.ShouldBe(userTeam.Role);

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
        var token = "valid-reset-token";
        var newPassword = "NewPassword123!";

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
        var activeToken1 = await CreateTestRefreshTokenAsync(user.Id, _testTeamId);
        var activeToken2 = await CreateTestRefreshTokenAsync(user.Id, _testTeamId);
        var expiredToken = await CreateTestRefreshTokenAsync(user.Id, _testTeamId, isExpired: true);

        // Act
        var result = await _authenticationService.LogoutAsync(user.Id);

        // Assert
        result.ShouldBeTrue();

        // Verify active tokens were revoked
        var revokedToken1 = DbContext.RefreshTokens.First(rt => rt.Id == activeToken1.Id);
        revokedToken1.RevokedOn.ShouldNotBeNull();
        revokedToken1.ReasonRevoked.ShouldBe("Logged out");

        var revokedToken2 = DbContext.RefreshTokens.First(rt => rt.Id == activeToken2.Id);
        revokedToken2.RevokedOn.ShouldNotBeNull();
        revokedToken2.ReasonRevoked.ShouldBe("Logged out");

        // Verify expired token was not modified (already inactive)
        var unchangedToken = DbContext.RefreshTokens.First(rt => rt.Id == expiredToken.Id);
        unchangedToken.ReasonRevoked.ShouldBe("Expired"); // This token was already revoked when created
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
        result.TeamId.ShouldBe(team.Id);
        result.Role.ShouldBe(TeamRole.TeamMember); // Default role for external users

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
        result.TeamId.ShouldBe(team.Id);
        result.Role.ShouldBe(TeamRole.TeamMember); // Existing user's role
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
            Id = _testTeamId,
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