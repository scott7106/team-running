using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;
using TeamStride.Infrastructure.Identity;
using TeamStride.Application.Authentication.Dtos;
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

    #region Global Admin Tests

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
        var teams = new List<TeamMembershipDto>
        {
            new()
            {
                TeamId = teamId,
                TeamRole = TeamRole.TeamOwner,
                MemberType = MemberType.Coach
            }
        };

        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "GlobalAdmin" });

        // Act
        var token = await _jwtTokenService.GenerateJwtTokenAsync(user, teams);

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
        var teams = new List<TeamMembershipDto>
        {
            new()
            {
                TeamId = teamId,
                TeamRole = TeamRole.TeamMember,
                MemberType = MemberType.Athlete
            }
        };

        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "StandardUser" });

        // Act
        var token = await _jwtTokenService.GenerateJwtTokenAsync(user, teams);

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
        var teams = new List<TeamMembershipDto>
        {
            new()
            {
                TeamId = teamId,
                TeamRole = TeamRole.TeamMember,
                MemberType = MemberType.Parent
            }
        };

        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act
        var token = await _jwtTokenService.GenerateJwtTokenAsync(user, teams);

        // Assert
        token.ShouldNotBeNullOrEmpty();

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        
        var isGlobalAdminClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "is_global_admin");
        isGlobalAdminClaim.ShouldNotBeNull();
        isGlobalAdminClaim.Value.ShouldBe("false");
    }

    #endregion

    #region Standard Claims Tests

    [Fact]
    public async Task GenerateJwtTokenAsync_ShouldIncludeAllRequiredStandardClaims()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };

        var teams = new List<TeamMembershipDto>();

        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act
        var token = await _jwtTokenService.GenerateJwtTokenAsync(user, teams);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        // Verify standard JWT claims
        var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        subClaim.ShouldNotBeNull();
        subClaim.Value.ShouldBe(user.Id.ToString());

        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        emailClaim.ShouldNotBeNull();
        emailClaim.Value.ShouldBe(user.Email);

        var jtiClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
        jtiClaim.ShouldNotBeNull();
        Guid.TryParse(jtiClaim.Value, out _).ShouldBeTrue();

        var firstNameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "first_name");
        firstNameClaim.ShouldNotBeNull();
        firstNameClaim.Value.ShouldBe(user.FirstName);

        var lastNameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "last_name");
        lastNameClaim.ShouldNotBeNull();
        lastNameClaim.Value.ShouldBe(user.LastName);
    }

    [Fact]
    public async Task GenerateJwtTokenAsync_WithNullUserEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = null!, // Null email
            FirstName = "John",
            LastName = "Doe"
        };

        var teams = new List<TeamMembershipDto>();

        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _jwtTokenService.GenerateJwtTokenAsync(user, teams));
    }

    #endregion

    #region Teams Claim Tests

    [Fact]
    public async Task GenerateJwtTokenAsync_WithSingleTeam_ShouldIncludeTeamsClaimAsJson()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "user@test.com",
            FirstName = "Test",
            LastName = "User"
        };

        var teamId = Guid.NewGuid();
        var teams = new List<TeamMembershipDto>
        {
            new()
            {
                TeamId = teamId,
                TeamRole = TeamRole.TeamAdmin,
                MemberType = MemberType.Coach
            }
        };

        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act
        var token = await _jwtTokenService.GenerateJwtTokenAsync(user, teams);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        
        var teamsClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "teams");
        teamsClaim.ShouldNotBeNull();

        // Verify JSON structure
        var teamsJson = teamsClaim.Value;
        var deserializedTeams = JsonSerializer.Deserialize<List<TeamMembershipDto>>(teamsJson, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });

        deserializedTeams.ShouldNotBeNull();
        deserializedTeams.Count.ShouldBe(1);
        deserializedTeams[0].TeamId.ShouldBe(teamId);
        deserializedTeams[0].TeamRole.ShouldBe(TeamRole.TeamAdmin);
        deserializedTeams[0].MemberType.ShouldBe(MemberType.Coach);
    }

    [Fact]
    public async Task GenerateJwtTokenAsync_WithMultipleTeams_ShouldIncludeAllTeamsInClaim()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "multiuser@test.com",
            FirstName = "Multi",
            LastName = "User"
        };

        var team1Id = Guid.NewGuid();
        var team2Id = Guid.NewGuid();
        var teams = new List<TeamMembershipDto>
        {
            new()
            {
                TeamId = team1Id,
                TeamRole = TeamRole.TeamOwner,
                MemberType = MemberType.Coach
            },
            new()
            {
                TeamId = team2Id,
                TeamRole = TeamRole.TeamMember,
                MemberType = MemberType.Parent
            }
        };

        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act
        var token = await _jwtTokenService.GenerateJwtTokenAsync(user, teams);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        
        var teamsClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "teams");
        teamsClaim.ShouldNotBeNull();

        var deserializedTeams = JsonSerializer.Deserialize<List<TeamMembershipDto>>(teamsClaim.Value, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });

        deserializedTeams.ShouldNotBeNull();
        deserializedTeams.Count.ShouldBe(2);
        
        // Verify first team
        var firstTeam = deserializedTeams.FirstOrDefault(t => t.TeamId == team1Id);
        firstTeam.ShouldNotBeNull();
        firstTeam.TeamRole.ShouldBe(TeamRole.TeamOwner);
        firstTeam.MemberType.ShouldBe(MemberType.Coach);
        
        // Verify second team
        var secondTeam = deserializedTeams.FirstOrDefault(t => t.TeamId == team2Id);
        secondTeam.ShouldNotBeNull();
        secondTeam.TeamRole.ShouldBe(TeamRole.TeamMember);
        secondTeam.MemberType.ShouldBe(MemberType.Parent);
    }

    [Fact]
    public async Task GenerateJwtTokenAsync_WithEmptyTeams_ShouldIncludeEmptyArrayInClaim()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "globaladmin@test.com",
            FirstName = "Global",
            LastName = "Admin"
        };

        var teams = new List<TeamMembershipDto>(); // Empty teams list

        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "GlobalAdmin" });

        // Act
        var token = await _jwtTokenService.GenerateJwtTokenAsync(user, teams);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        
        var teamsClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "teams");
        teamsClaim.ShouldNotBeNull();

        var deserializedTeams = JsonSerializer.Deserialize<List<TeamMembershipDto>>(teamsClaim.Value, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });

        deserializedTeams.ShouldNotBeNull();
        deserializedTeams.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GenerateJwtTokenAsync_TeamsClaimShouldUseCamelCaseNaming()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "user@test.com",
            FirstName = "Test",
            LastName = "User"
        };

        var teams = new List<TeamMembershipDto>
        {
            new()
            {
                TeamId = Guid.NewGuid(),
                TeamRole = TeamRole.TeamMember,
                MemberType = MemberType.Athlete
            }
        };

        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act
        var token = await _jwtTokenService.GenerateJwtTokenAsync(user, teams);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        
        var teamsClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "teams");
        teamsClaim.ShouldNotBeNull();

        // Verify camelCase naming in JSON
        var teamsJson = teamsClaim.Value;
        teamsJson.ShouldContain("teamId");
        teamsJson.ShouldContain("teamRole");
        teamsJson.ShouldContain("memberType");
        
        // Verify it contains camelCase JSON structure correctly
        // The JSON should look like: [{"teamId":"...","teamRole":2,"memberType":1}]
        teamsJson.ShouldStartWith("[{\"teamId\":");
        teamsJson.ShouldContain("\"teamRole\":");
        teamsJson.ShouldContain("\"memberType\":");
    }

    #endregion

    #region Argument Validation Tests

    [Fact]
    public async Task GenerateJwtTokenAsync_WithNullUser_ShouldThrowArgumentNullException()
    {
        // Arrange
        var teams = new List<TeamMembershipDto>();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => _jwtTokenService.GenerateJwtTokenAsync(null!, teams));
    }

    [Fact]
    public async Task GenerateJwtTokenAsync_WithNullTeams_ShouldThrowArgumentNullException()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "user@test.com",
            FirstName = "Test",
            LastName = "User"
        };

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => _jwtTokenService.GenerateJwtTokenAsync(user, null!));
    }

    #endregion

    #region Token Structure Tests

    [Fact]
    public async Task GenerateJwtTokenAsync_ShouldGenerateValidJwtStructure()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "user@test.com",
            FirstName = "Test",
            LastName = "User"
        };

        var teams = new List<TeamMembershipDto>();

        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act
        var token = await _jwtTokenService.GenerateJwtTokenAsync(user, teams);

        // Assert
        token.ShouldNotBeNullOrEmpty();

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        // Verify token structure
        jwtToken.Header.ShouldNotBeNull();
        jwtToken.Payload.ShouldNotBeNull();
        jwtToken.Header.Alg.ShouldBe("HS256");
        jwtToken.Issuer.ShouldBe(_authConfig.JwtIssuer);
        jwtToken.Audiences.ShouldContain(_authConfig.JwtAudience);
        
        // Verify expiration is set correctly
        var expiration = jwtToken.ValidTo;
        var expectedExpiration = DateTime.UtcNow.AddMinutes(_authConfig.JwtExpirationMinutes);
        var timeDifference = Math.Abs((expiration - expectedExpiration).TotalSeconds);
        timeDifference.ShouldBeLessThan(5); // Allow 5 seconds tolerance
    }

    #endregion
} 