using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;
using TeamStride.Infrastructure.Identity;
using Xunit;

namespace TeamStride.Infrastructure.Tests.Identity;

public class JwtTokenServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly AuthenticationConfiguration _authConfig;
    private readonly JwtTokenService _jwtTokenService;

    public JwtTokenServiceTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        
        _authConfig = new AuthenticationConfiguration
        {
            JwtSecret = "this-is-a-very-long-secret-key-for-testing-purposes-that-meets-minimum-requirements",
            JwtIssuer = "test-issuer",
            JwtAudience = "test-audience",
            JwtExpirationMinutes = 60,
            Microsoft = new ExternalProviderConfiguration { ClientId = "test", ClientSecret = "test" },
            Google = new ExternalProviderConfiguration { ClientId = "test", ClientSecret = "test" },
            Facebook = new ExternalProviderConfiguration { ClientId = "test", ClientSecret = "test" },
            Twitter = new ExternalProviderConfiguration { ClientId = "test", ClientSecret = "test" }
        };

        _jwtTokenService = new JwtTokenService(_authConfig, _mockUserManager.Object);
    }

    [Fact]
    public async Task GenerateJwtTokenAsync_WhenUserIsGlobalAdmin_ShouldIncludeIsGlobalAdminClaimAsTrue()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "admin@test.com",
            FirstName = "Global",
            LastName = "Admin"
        };

        var teamId = Guid.NewGuid();
        var role = TeamRole.TeamOwner;

        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "GlobalAdmin" });

        // Act
        var token = await _jwtTokenService.GenerateJwtTokenAsync(user, teamId, role, MemberType.Coach);

        // Assert
        token.ShouldNotBeNullOrEmpty();

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        
        var isGlobalAdminClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "is_global_admin");
        isGlobalAdminClaim.ShouldNotBeNull();
        isGlobalAdminClaim.Value.ShouldBe("true");
    }

    [Fact]
    public async Task GenerateJwtTokenAsync_WhenUserIsStandardUser_ShouldIncludeIsGlobalAdminClaimAsFalse()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "user@test.com",
            FirstName = "Standard",
            LastName = "User"
        };

        var teamId = Guid.NewGuid();
        var role = TeamRole.TeamMember;

        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "StandardUser" });

        // Act
        var token = await _jwtTokenService.GenerateJwtTokenAsync(user, teamId, role, MemberType.Athlete);

        // Assert
        token.ShouldNotBeNullOrEmpty();

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        
        var isGlobalAdminClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "is_global_admin");
        isGlobalAdminClaim.ShouldNotBeNull();
        isGlobalAdminClaim.Value.ShouldBe("false");
    }

    [Fact]
    public async Task GenerateJwtTokenAsync_WhenUserHasNoRoles_ShouldIncludeIsGlobalAdminClaimAsFalse()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "noroles@test.com",
            FirstName = "No",
            LastName = "Roles"
        };

        var teamId = Guid.NewGuid();
        var role = TeamRole.TeamMember;

        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act
        var token = await _jwtTokenService.GenerateJwtTokenAsync(user, teamId, role, MemberType.Parent);

        // Assert
        token.ShouldNotBeNullOrEmpty();

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        
        var isGlobalAdminClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "is_global_admin");
        isGlobalAdminClaim.ShouldNotBeNull();
        isGlobalAdminClaim.Value.ShouldBe("false");
    }
} 