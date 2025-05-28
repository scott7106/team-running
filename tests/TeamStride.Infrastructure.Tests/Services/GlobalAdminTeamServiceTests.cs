using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using TeamStride.Application.Common.Models;
using TeamStride.Application.Common.Services;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;
using TeamStride.Domain.Interfaces;
using TeamStride.Infrastructure.Data;
using TeamStride.Infrastructure.Mapping;
using TeamStride.Infrastructure.Services;

namespace TeamStride.Infrastructure.Tests.Services;

public class GlobalAdminTeamServiceTests : BaseIntegrationTest
{
    private readonly GlobalAdminTeamService _service;
    private readonly Mock<IAuthorizationService> _mockAuthorizationService;
    private readonly Mock<ILogger<GlobalAdminTeamService>> _mockLogger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;

    public GlobalAdminTeamServiceTests()
    {
        _mockAuthorizationService = new Mock<IAuthorizationService>();
        _mockLogger = new Mock<ILogger<GlobalAdminTeamService>>();
        _userManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();

        _service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object
            );
    }

    #region GetTeamsAsync Tests

    [Fact]
    public async Task GetTeamsAsync_WhenUserIsNotGlobalAdmin_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .ThrowsAsync(new UnauthorizedAccessException("Global admin privileges required"));

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _service.GetTeamsAsync());
    }

    [Fact]
    public async Task GetTeamsAsync_WhenNoTeamsExist_ShouldReturnEmptyPaginatedList()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.GetTeamsAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
        result.PageNumber.ShouldBe(1);
        result.TotalPages.ShouldBe(0);
    }

    [Fact]
    public async Task GetTeamsAsync_WhenTeamsExist_ShouldReturnPaginatedList()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        
        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        var team = await CreateTestTeamAsync("Test Team", "test-team", owner.Id);

        // Act
        var result = await _service.GetTeamsAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(1);
        result.TotalCount.ShouldBe(1);
        
        var teamDto = result.Items.First();
        teamDto.Name.ShouldBe("Test Team");
        teamDto.Subdomain.ShouldBe("test-team");
        teamDto.OwnerEmail.ShouldBe("owner@test.com");
    }

    [Fact]
    public async Task GetTeamsAsync_WithSearchQuery_ShouldFilterResults()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        
        var owner1 = await CreateTestUserAsync("owner1@test.com", "Owner1", "User");
        var owner2 = await CreateTestUserAsync("owner2@test.com", "Owner2", "User");
        await CreateTestTeamAsync("Alpha Team", "alpha-team", owner1.Id);
        await CreateTestTeamAsync("Beta Team", "beta-team", owner2.Id);

        // Act
        var result = await _service.GetTeamsAsync(searchQuery: "alpha");

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items.First().Name.ShouldBe("Alpha Team");
    }

    [Fact]
    public async Task GetTeamsAsync_WithStatusFilter_ShouldFilterResults()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        
        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        var activeTeam = await CreateTestTeamAsync("Active Team", "active-team", owner.Id);
        var suspendedTeam = await CreateTestTeamAsync("Suspended Team", "suspended-team", owner.Id);
        suspendedTeam.Status = TeamStatus.Suspended;
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetTeamsAsync(status: TeamStatus.Active);

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items.First().Name.ShouldBe("Active Team");
    }

    [Fact]
    public async Task GetTeamsAsync_WithTierFilter_ShouldFilterResults()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        
        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        var freeTeam = await CreateTestTeamAsync("Free Team", "free-team", owner.Id);
        var premiumTeam = await CreateTestTeamAsync("Premium Team", "premium-team", owner.Id);
        premiumTeam.Tier = TeamTier.Premium;
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetTeamsAsync(tier: TeamTier.Premium);

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items.First().Name.ShouldBe("Premium Team");
    }

    [Fact]
    public async Task GetTeamsAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        
        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        for (int i = 1; i <= 15; i++)
        {
            await CreateTestTeamAsync($"Team {i:D2}", $"team-{i:D2}", owner.Id);
        }

        // Act
        var result = await _service.GetTeamsAsync(pageNumber: 2, pageSize: 10);

        // Assert
        result.Items.Count.ShouldBe(5);
        result.TotalCount.ShouldBe(15);
        result.PageNumber.ShouldBe(2);
        result.TotalPages.ShouldBe(2);
    }

    #endregion

    #region GetDeletedTeamsAsync Tests

    [Fact]
    public async Task GetDeletedTeamsAsync_WhenUserIsNotGlobalAdmin_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .ThrowsAsync(new UnauthorizedAccessException("Global admin privileges required"));

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _service.GetDeletedTeamsAsync());
    }

    [Fact]
    public async Task GetDeletedTeamsAsync_WhenNoDeletedTeamsExist_ShouldReturnEmptyList()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        
        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        await CreateTestTeamAsync("Active Team", "active-team", owner.Id);

        // Act
        var result = await _service.GetDeletedTeamsAsync();

        // Assert
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetDeletedTeamsAsync_WhenDeletedTeamsExist_ShouldReturnDeletedTeams()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        
        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        var team = await CreateTestTeamAsync("Deleted Team", "deleted-team", owner.Id);
        team.IsDeleted = true;
        team.DeletedOn = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetDeletedTeamsAsync();

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items.First().Name.ShouldBe("Deleted Team");
        result.Items.First().DeletedOn.ShouldNotBeNull();
    }

    #endregion

    #region GetTeamByIdAsync Tests

    [Fact]
    public async Task GetTeamByIdAsync_WhenUserIsNotGlobalAdmin_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .ThrowsAsync(new UnauthorizedAccessException("Global admin privileges required"));

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _service.GetTeamByIdAsync(teamId));
    }

    [Fact]
    public async Task GetTeamByIdAsync_WhenTeamDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _service.GetTeamByIdAsync(teamId));
        exception.Message.ShouldBe($"Team with ID {teamId} not found");
    }

    [Fact]
    public async Task GetTeamByIdAsync_WhenTeamExists_ShouldReturnTeam()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        
        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        var team = await CreateTestTeamAsync("Test Team", "test-team", owner.Id);

        // Act
        var result = await _service.GetTeamByIdAsync(team.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(team.Id);
        result.Name.ShouldBe("Test Team");
        result.OwnerEmail.ShouldBe("owner@test.com");
    }

    #endregion

    #region CreateTeamWithNewOwnerAsync Tests

    [Fact]
    public async Task CreateTeamWithNewOwnerAsync_WhenUserIsNotGlobalAdmin_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var dto = new CreateTeamWithNewOwnerDto
        {
            Name = "Test Team",
            Subdomain = "test-team",
            OwnerEmail = "owner@test.com",
            OwnerFirstName = "Owner",
            OwnerLastName = "User",
            OwnerPassword = "Password123!"
        };

        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .ThrowsAsync(new UnauthorizedAccessException("Global admin privileges required"));

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _service.CreateTeamWithNewOwnerAsync(dto));
    }

    [Fact]
    public async Task CreateTeamWithNewOwnerAsync_WhenSubdomainIsTaken_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        
        var existingOwner = await CreateTestUserAsync("existing@test.com", "Existing", "User");
        await CreateTestTeamAsync("Existing Team", "test-team", existingOwner.Id);

        var dto = new CreateTeamWithNewOwnerDto
        {
            Name = "New Team",
            Subdomain = "test-team",
            OwnerEmail = "newowner@test.com",
            OwnerFirstName = "New",
            OwnerLastName = "Owner",
            OwnerPassword = "Password123!"
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _service.CreateTeamWithNewOwnerAsync(dto));
        exception.Message.ShouldBe("Subdomain 'test-team' is already taken");
    }

    [Fact]
    public async Task CreateTeamWithNewOwnerAsync_WhenUserEmailExists_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        
        await CreateTestUserAsync("existing@test.com", "Existing", "User");

        var dto = new CreateTeamWithNewOwnerDto
        {
            Name = "Test Team",
            Subdomain = "test-team",
            OwnerEmail = "existing@test.com",
            OwnerFirstName = "Owner",
            OwnerLastName = "User",
            OwnerPassword = "Password123!"
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _service.CreateTeamWithNewOwnerAsync(dto));
        exception.Message.ShouldBe("User with email 'existing@test.com' already exists");
    }

    [Fact]
    public async Task CreateTeamWithNewOwnerAsync_WhenValidData_ShouldCreateTeamAndUser()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(true);
        
        var dto = new CreateTeamWithNewOwnerDto
        {
            Name = "Test Team",
            Subdomain = "test-team",
            OwnerEmail = "owner@test.com",
            OwnerFirstName = "Owner",
            OwnerLastName = "User",
            OwnerPassword = "Password123!",
            Tier = TeamTier.Premium,
            PrimaryColor = "#FF0000",
            SecondaryColor = "#00FF00",
            ExpiresOn = DateTime.UtcNow.AddYears(1)
        };

        // Act
        var result = await _service.CreateTeamWithNewOwnerAsync(dto);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Test Team");
        result.Subdomain.ShouldBe("test-team");
        result.OwnerEmail.ShouldBe("owner@test.com");
        result.Tier.ShouldBe(TeamTier.Premium);
        result.PrimaryColor.ShouldBe("#FF0000");
        result.SecondaryColor.ShouldBe("#00FF00");

        // Verify user was created
        var user = await _userManager.FindByEmailAsync("owner@test.com");
        user.ShouldNotBeNull();
        user.FirstName.ShouldBe("Owner");
        user.LastName.ShouldBe("User");

        // Verify team was created
        var team = await DbContext.Teams.FirstOrDefaultAsync(t => t.Subdomain == "test-team");
        team.ShouldNotBeNull();
        team.OwnerId.ShouldBe(user.Id);

        // Verify UserTeam relationship was created
        var userTeam = await DbContext.UserTeams.FirstOrDefaultAsync(ut => ut.UserId == user.Id && ut.TeamId == team.Id);
        userTeam.ShouldNotBeNull();
        userTeam.Role.ShouldBe(TeamRole.TeamOwner);
        userTeam.MemberType.ShouldBe(MemberType.Coach);
        userTeam.IsActive.ShouldBeTrue();
        userTeam.IsDefault.ShouldBeTrue();
    }

    #endregion

    #region CreateTeamWithExistingOwnerAsync Tests

    [Fact]
    public async Task CreateTeamWithExistingOwnerAsync_WhenUserIsNotGlobalAdmin_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var dto = new CreateTeamWithExistingOwnerDto
        {
            Name = "Test Team",
            Subdomain = "test-team",
            OwnerId = Guid.NewGuid()
        };

        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .ThrowsAsync(new UnauthorizedAccessException("Global admin privileges required"));

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _service.CreateTeamWithExistingOwnerAsync(dto));
    }

    [Fact]
    public async Task CreateTeamWithExistingOwnerAsync_WhenUserDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        
        var nonExistentUserId = Guid.NewGuid();
        var dto = new CreateTeamWithExistingOwnerDto
        {
            Name = "Test Team",
            Subdomain = "test-team",
            OwnerId = nonExistentUserId
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _service.CreateTeamWithExistingOwnerAsync(dto));
        exception.Message.ShouldBe($"User with ID {nonExistentUserId} not found");
    }

    [Fact]
    public async Task CreateTeamWithExistingOwnerAsync_WhenValidData_ShouldCreateTeam()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(true);
        
        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");

        var dto = new CreateTeamWithExistingOwnerDto
        {
            Name = "Test Team",
            Subdomain = "test-team",
            OwnerId = owner.Id,
            Tier = TeamTier.Standard,
            PrimaryColor = "#0000FF",
            SecondaryColor = "#FFFF00",
            ExpiresOn = DateTime.UtcNow.AddMonths(6)
        };

        // Act
        var result = await _service.CreateTeamWithExistingOwnerAsync(dto);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Test Team");
        result.Subdomain.ShouldBe("test-team");
        result.OwnerId.ShouldBe(owner.Id);
        result.OwnerEmail.ShouldBe("owner@test.com");
        result.Tier.ShouldBe(TeamTier.Standard);

        // Verify team was created
        var team = await DbContext.Teams.FirstOrDefaultAsync(t => t.Subdomain == "test-team");
        team.ShouldNotBeNull();
        team.OwnerId.ShouldBe(owner.Id);

        // Verify UserTeam relationship was created
        var userTeam = await DbContext.UserTeams.FirstOrDefaultAsync(ut => ut.UserId == owner.Id && ut.TeamId == team.Id);
        userTeam.ShouldNotBeNull();
        userTeam.Role.ShouldBe(TeamRole.TeamOwner);
        userTeam.MemberType.ShouldBe(MemberType.Coach);
        userTeam.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateTeamWithExistingOwnerAsync_WhenUserAlreadyOwnsAnotherTeam_ShouldAllowMultipleOwnership()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        
        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        await CreateTestTeamAsync("First Team", "first-team", owner.Id);

        var dto = new CreateTeamWithExistingOwnerDto
        {
            Name = "Second Team",
            Subdomain = "second-team",
            OwnerId = owner.Id,
            Tier = TeamTier.Standard
        };

        // Act
        var result = await _service.CreateTeamWithExistingOwnerAsync(dto);

        // Assert - Should succeed as users can own multiple teams per requirements
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Second Team");
        result.Subdomain.ShouldBe("second-team");
        result.OwnerId.ShouldBe(owner.Id);

        // Verify both teams exist with same owner
        var firstTeam = await DbContext.Teams.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Subdomain == "first-team");
        var secondTeam = await DbContext.Teams.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Subdomain == "second-team");
        
        firstTeam.ShouldNotBeNull();
        secondTeam.ShouldNotBeNull();
        firstTeam.OwnerId.ShouldBe(owner.Id);
        secondTeam.OwnerId.ShouldBe(owner.Id);

        // Verify UserTeam relationships for both teams
        var userTeams = await DbContext.UserTeams.IgnoreQueryFilters()
            .Where(ut => ut.UserId == owner.Id && ut.Role == TeamRole.TeamOwner)
            .ToListAsync();
        userTeams.Count.ShouldBe(2);
    }

    #endregion

    #region UpdateTeamAsync Tests

    [Fact]
    public async Task UpdateTeamAsync_WhenUserIsNotGlobalAdmin_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var dto = new GlobalAdminUpdateTeamDto { Name = "Updated Team" };

        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .ThrowsAsync(new UnauthorizedAccessException("Global admin privileges required"));

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _service.UpdateTeamAsync(teamId, dto));
    }

    [Fact]
    public async Task UpdateTeamAsync_WhenTeamDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        
        var teamId = Guid.NewGuid();
        var dto = new GlobalAdminUpdateTeamDto { Name = "Updated Team" };

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _service.UpdateTeamAsync(teamId, dto));
        exception.Message.ShouldBe($"Team with ID {teamId} not found");
    }

    [Fact]
    public async Task UpdateTeamAsync_WhenSubdomainIsTaken_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        
        var owner1 = await CreateTestUserAsync("owner1@test.com", "Owner1", "User");
        var owner2 = await CreateTestUserAsync("owner2@test.com", "Owner2", "User");
        var team1 = await CreateTestTeamAsync("Team 1", "team-1", owner1.Id);
        await CreateTestTeamAsync("Team 2", "team-2", owner2.Id);

        var dto = new GlobalAdminUpdateTeamDto { Subdomain = "team-2" };

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _service.UpdateTeamAsync(team1.Id, dto));
        exception.Message.ShouldBe("Subdomain 'team-2' is already taken");
    }

    [Fact]
    public async Task UpdateTeamAsync_WhenValidData_ShouldUpdateTeam()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        
        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        var team = await CreateTestTeamAsync("Original Team", "original-team", owner.Id);

        var dto = new GlobalAdminUpdateTeamDto
        {
            Name = "Updated Team",
            Subdomain = "updated-team",
            Status = TeamStatus.Suspended,
            Tier = TeamTier.Premium,
            PrimaryColor = "#FF0000",
            SecondaryColor = "#00FF00",
            LogoUrl = "https://example.com/logo.png",
            ExpiresOn = DateTime.UtcNow.AddYears(2)
        };

        // Act
        var result = await _service.UpdateTeamAsync(team.Id, dto);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Updated Team");
        result.Subdomain.ShouldBe("updated-team");
        result.Status.ShouldBe(TeamStatus.Suspended);
        result.Tier.ShouldBe(TeamTier.Premium);
        result.PrimaryColor.ShouldBe("#FF0000");
        result.SecondaryColor.ShouldBe("#00FF00");
        result.LogoUrl.ShouldBe("https://example.com/logo.png");
        result.ModifiedOn.ShouldNotBeNull();

        // Verify database was updated
        var updatedTeam = await DbContext.Teams.FindAsync(team.Id);
        updatedTeam.ShouldNotBeNull();
        updatedTeam.Name.ShouldBe("Updated Team");
        updatedTeam.Subdomain.ShouldBe("updated-team");
    }

    [Fact]
    public async Task UpdateTeamAsync_WhenPartialUpdate_ShouldUpdateOnlyProvidedFields()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        
        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        var team = await CreateTestTeamAsync("Original Team", "original-team", owner.Id);

        var dto = new GlobalAdminUpdateTeamDto
        {
            Name = "Updated Team"
            // Only updating name, other fields should remain unchanged
        };

        // Act
        var result = await _service.UpdateTeamAsync(team.Id, dto);

        // Assert
        result.Name.ShouldBe("Updated Team");
        result.Subdomain.ShouldBe("original-team"); // Should remain unchanged
        result.Status.ShouldBe(TeamStatus.Active); // Should remain unchanged
    }

    #endregion

    #region DeleteTeamAsync Tests

    [Fact]
    public async Task DeleteTeamAsync_WhenUserIsNotGlobalAdmin_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .ThrowsAsync(new UnauthorizedAccessException("Global admin privileges required"));

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _service.DeleteTeamAsync(teamId));
    }

    [Fact]
    public async Task DeleteTeamAsync_WhenTeamDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        var teamId = Guid.NewGuid();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _service.DeleteTeamAsync(teamId));
        exception.Message.ShouldBe($"Team with ID {teamId} not found");
    }

    [Fact]
    public async Task DeleteTeamAsync_WhenTeamIsAlreadyDeleted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        
        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        var team = await CreateTestTeamAsync("Test Team", "test-team", owner.Id);
        team.IsDeleted = true;
        await DbContext.SaveChangesAsync();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _service.DeleteTeamAsync(team.Id));
        exception.Message.ShouldBe($"Team with ID {team.Id} is already deleted");
    }

    [Fact]
    public async Task DeleteTeamAsync_WhenValidTeam_ShouldSoftDeleteTeam()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        
        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        var team = await CreateTestTeamAsync("Test Team", "test-team", owner.Id);

        // Act
        await _service.DeleteTeamAsync(team.Id);

        // Assert
        var deletedTeam = await DbContext.Teams.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == team.Id);
        deletedTeam.ShouldNotBeNull();
        deletedTeam.IsDeleted.ShouldBeTrue();
        deletedTeam.DeletedOn.ShouldNotBeNull();
        deletedTeam.Status.ShouldBe(TeamStatus.Suspended);

        // Verify user-team relationships are deactivated
        var userTeams = await DbContext.UserTeams.IgnoreQueryFilters()
            .Where(ut => ut.TeamId == team.Id).ToListAsync();
        userTeams.ShouldAllBe(ut => !ut.IsActive);
    }

    #endregion

    #region PermanentlyDeleteTeamAsync Tests

    [Fact]
    public async Task PermanentlyDeleteTeamAsync_WhenUserIsNotGlobalAdmin_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .ThrowsAsync(new UnauthorizedAccessException("Global admin privileges required"));

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _service.PermanentlyDeleteTeamAsync(teamId));
    }

    [Fact]
    public async Task PermanentlyDeleteTeamAsync_WhenTeamDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        var teamId = Guid.NewGuid();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _service.PermanentlyDeleteTeamAsync(teamId));
        exception.Message.ShouldBe($"Team with ID {teamId} not found");
    }

    [Fact]
    public async Task PermanentlyDeleteTeamAsync_WhenValidTeam_ShouldPermanentlyDeleteTeam()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        
        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        var team = await CreateTestTeamAsync("Test Team", "test-team", owner.Id);

        // Create an ownership transfer to test cascade deletion
        var transfer = new OwnershipTransfer
        {
            TeamId = team.Id,
            InitiatedByUserId = owner.Id,
            NewOwnerEmail = "newowner@test.com",
            Status = OwnershipTransferStatus.Pending,
            ExpiresOn = DateTime.UtcNow.AddDays(7),
            TransferToken = Guid.NewGuid().ToString("N"),
            CreatedOn = DateTime.UtcNow
        };
        DbContext.OwnershipTransfers.Add(transfer);
        await DbContext.SaveChangesAsync();

        // Act
        await _service.PermanentlyDeleteTeamAsync(team.Id);

        // Assert
        var deletedTeam = await DbContext.Teams.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == team.Id);
        deletedTeam.ShouldBeNull();

        var userTeams = await DbContext.UserTeams.IgnoreQueryFilters()
            .Where(ut => ut.TeamId == team.Id).ToListAsync();
        userTeams.ShouldBeEmpty();

        var transfers = await DbContext.OwnershipTransfers.IgnoreQueryFilters()
            .Where(ot => ot.TeamId == team.Id).ToListAsync();
        transfers.ShouldBeEmpty();
    }

    #endregion

    #region RecoverTeamAsync Tests

    [Fact]
    public async Task RecoverTeamAsync_WhenUserIsNotGlobalAdmin_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .ThrowsAsync(new UnauthorizedAccessException("Global admin privileges required"));

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _service.RecoverTeamAsync(teamId));
    }

    [Fact]
    public async Task RecoverTeamAsync_WhenTeamDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        var teamId = Guid.NewGuid();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _service.RecoverTeamAsync(teamId));
        exception.Message.ShouldBe($"Team with ID {teamId} not found");
    }

    [Fact]
    public async Task RecoverTeamAsync_WhenTeamIsNotDeleted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        
        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        var team = await CreateTestTeamAsync("Test Team", "test-team", owner.Id);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _service.RecoverTeamAsync(team.Id));
        exception.Message.ShouldBe($"Team with ID {team.Id} is not deleted");
    }

    [Fact]
    public async Task RecoverTeamAsync_WhenSubdomainIsTaken_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        
        var owner1 = await CreateTestUserAsync("owner1@test.com", "Owner1", "User");
        var owner2 = await CreateTestUserAsync("owner2@test.com", "Owner2", "User");
        
        // Create deleted team
        var deletedTeam = await CreateTestTeamAsync("Deleted Team", "test-team", owner1.Id);
        deletedTeam.IsDeleted = true;
        deletedTeam.DeletedOn = DateTime.UtcNow;
        
        // Create active team with same subdomain
        await CreateTestTeamAsync("Active Team", "test-team", owner2.Id);
        await DbContext.SaveChangesAsync();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _service.RecoverTeamAsync(deletedTeam.Id));
        exception.Message.ShouldBe("Cannot recover team: subdomain 'test-team' is now taken");
    }

    [Fact]
    public async Task RecoverTeamAsync_WhenValidDeletedTeam_ShouldRecoverTeam()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        
        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        var team = await CreateTestTeamAsync("Test Team", "test-team", owner.Id);
        
        // Soft delete the team
        team.IsDeleted = true;
        team.DeletedOn = DateTime.UtcNow;
        team.Status = TeamStatus.Suspended;
        
        // Deactivate user-team relationships
        var userTeams = await DbContext.UserTeams.Where(ut => ut.TeamId == team.Id).ToListAsync();
        foreach (var userTeam in userTeams)
        {
            userTeam.IsActive = false;
        }
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.RecoverTeamAsync(team.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(team.Id);
        result.Name.ShouldBe("Test Team");

        // Verify team is recovered
        var recoveredTeam = await DbContext.Teams.FindAsync(team.Id);
        recoveredTeam.ShouldNotBeNull();
        recoveredTeam.IsDeleted.ShouldBeFalse();
        recoveredTeam.DeletedOn.ShouldBeNull();
        recoveredTeam.Status.ShouldBe(TeamStatus.Active);

        // Verify user-team relationships are reactivated
        var reactivatedUserTeams = await DbContext.UserTeams.Where(ut => ut.TeamId == team.Id).ToListAsync();
        reactivatedUserTeams.ShouldAllBe(ut => ut.IsActive);
    }

    #endregion

    #region TransferOwnershipAsync Tests

    [Fact]
    public async Task TransferOwnershipAsync_WhenUserIsNotGlobalAdmin_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var dto = new GlobalAdminTransferOwnershipDto { NewOwnerId = Guid.NewGuid() };

        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .ThrowsAsync(new UnauthorizedAccessException("Global admin privileges required"));

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _service.TransferOwnershipAsync(teamId, dto));
    }

    [Fact]
    public async Task TransferOwnershipAsync_WhenTeamDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        
        var teamId = Guid.NewGuid();
        var dto = new GlobalAdminTransferOwnershipDto { NewOwnerId = Guid.NewGuid() };

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _service.TransferOwnershipAsync(teamId, dto));
        exception.Message.ShouldBe($"Team with ID {teamId} not found");
    }

    [Fact]
    public async Task TransferOwnershipAsync_WhenNewOwnerDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        
        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        var team = await CreateTestTeamAsync("Test Team", "test-team", owner.Id);
        
        var nonExistentUserId = Guid.NewGuid();
        var dto = new GlobalAdminTransferOwnershipDto { NewOwnerId = nonExistentUserId };

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _service.TransferOwnershipAsync(team.Id, dto));
        exception.Message.ShouldBe($"User with ID {nonExistentUserId} not found");
    }

    [Fact]
    public async Task TransferOwnershipAsync_WhenValidData_ShouldTransferOwnership()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(true);
        
        var admin = await CreateTestUserAsync("admin@test.com", "admin", "user", MockCurrentUserService.Object.UserId);
        
        var oldOwner = await CreateTestUserAsync("oldowner@test.com", "Old", "Owner");
        var newOwner = await CreateTestUserAsync("newowner@test.com", "New", "Owner");
        var team = await CreateTestTeamAsync("Test Team", "test-team", oldOwner.Id);

        // Create a pending ownership transfer to test cancellation
        var pendingTransfer = new OwnershipTransfer
        {
            TeamId = team.Id,
            InitiatedByUserId = admin.Id,
            NewOwnerEmail = "newowner@test.com",
            Status = OwnershipTransferStatus.Pending,
            ExpiresOn = DateTime.UtcNow.AddDays(7),
            TransferToken = Guid.NewGuid().ToString("N"),
            CreatedOn = DateTime.UtcNow
        };
        DbContext.OwnershipTransfers.Add(pendingTransfer);
        await DbContext.SaveChangesAsync();

        var dto = new GlobalAdminTransferOwnershipDto 
        { 
            NewOwnerId = newOwner.Id,
            Message = "Admin transfer"
        };

        // Act
        var result = await _service.TransferOwnershipAsync(team.Id, dto);

        // Assert
        result.ShouldNotBeNull();
        result.OwnerId.ShouldBe(newOwner.Id);
        result.OwnerEmail.ShouldBe("newowner@test.com");

        // Verify team ownership was updated
        var updatedTeam = await DbContext.Teams.FindAsync(team.Id);
        updatedTeam.ShouldNotBeNull();
        updatedTeam.OwnerId.ShouldBe(newOwner.Id);

        // Verify old owner role was changed to admin
        var oldOwnerTeam = await DbContext.UserTeams
            .FirstOrDefaultAsync(ut => ut.UserId == oldOwner.Id && ut.TeamId == team.Id);
        oldOwnerTeam.ShouldNotBeNull();
        oldOwnerTeam.MemberType.ShouldBe(MemberType.Coach);
        oldOwnerTeam.Role.ShouldBe(TeamRole.TeamMember);

        // Verify new owner role was set to owner
        var newOwnerTeam = await DbContext.UserTeams
            .FirstOrDefaultAsync(ut => ut.UserId == newOwner.Id && ut.TeamId == team.Id);
        newOwnerTeam.ShouldNotBeNull();
        newOwnerTeam.Role.ShouldBe(TeamRole.TeamOwner);
        newOwnerTeam.MemberType.ShouldBe(MemberType.Coach);
        newOwnerTeam.IsActive.ShouldBeTrue();

        // Verify pending transfers were cancelled
        var cancelledTransfer = await DbContext.OwnershipTransfers.FindAsync(pendingTransfer.Id);
        cancelledTransfer.ShouldNotBeNull();
        cancelledTransfer.Status.ShouldBe(OwnershipTransferStatus.Completed);
    }

    [Fact]
    public async Task TransferOwnershipAsync_WhenNewOwnerAlreadyOwnsAnotherTeam_ShouldAllowMultipleOwnership()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync()).Returns(Task.CompletedTask);
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(true);
        
        var oldOwner = await CreateTestUserAsync("oldowner@test.com", "Old", "Owner");
        var newOwner = await CreateTestUserAsync("newowner@test.com", "New", "Owner");
        var team1 = await CreateTestTeamAsync("Team 1", "team-1", oldOwner.Id);
        var team2 = await CreateTestTeamAsync("Team 2", "team-2", newOwner.Id); // newOwner already owns this team

        var dto = new GlobalAdminTransferOwnershipDto 
        { 
            NewOwnerId = newOwner.Id,
            Message = "Transfer to existing owner"
        };

        // Act
        var result = await _service.TransferOwnershipAsync(team1.Id, dto);

        // Assert - Should succeed as users can own multiple teams per requirements
        result.ShouldNotBeNull();
        result.OwnerId.ShouldBe(newOwner.Id);
        result.OwnerEmail.ShouldBe("newowner@test.com");

        // Verify both teams are now owned by newOwner
        var updatedTeam1 = await DbContext.Teams.FindAsync(team1.Id);
        var updatedTeam2 = await DbContext.Teams.FindAsync(team2.Id);
        
        updatedTeam1.ShouldNotBeNull();
        updatedTeam2.ShouldNotBeNull();
        updatedTeam1.OwnerId.ShouldBe(newOwner.Id);
        updatedTeam2.OwnerId.ShouldBe(newOwner.Id);

        // Verify UserTeam relationships - newOwner should be owner of both teams
        var ownershipCount = await DbContext.UserTeams
            .CountAsync(ut => ut.UserId == newOwner.Id && ut.Role == TeamRole.TeamOwner && ut.IsActive);
        ownershipCount.ShouldBe(2);
    }

    #endregion

    #region IsSubdomainAvailableAsync Tests

    [Fact]
    public async Task IsSubdomainAvailableAsync_WhenSubdomainIsAvailable_ShouldReturnTrue()
    {
        // Arrange
        var subdomain = "available-subdomain";

        // Act
        var result = await _service.IsSubdomainAvailableAsync(subdomain);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsSubdomainAvailableAsync_WhenSubdomainIsTaken_ShouldReturnFalse()
    {
        // Arrange
        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        await CreateTestTeamAsync("Test Team", "taken-subdomain", owner.Id);

        // Act
        var result = await _service.IsSubdomainAvailableAsync("taken-subdomain");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task IsSubdomainAvailableAsync_WhenSubdomainIsTakenButExcluded_ShouldReturnTrue()
    {
        // Arrange
        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        var team = await CreateTestTeamAsync("Test Team", "test-subdomain", owner.Id);

        // Act
        var result = await _service.IsSubdomainAvailableAsync("test-subdomain", team.Id);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsSubdomainAvailableAsync_WhenSubdomainIsTakenByDeletedTeam_ShouldReturnTrue()
    {
        // Arrange
        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        var team = await CreateTestTeamAsync("Test Team", "deleted-subdomain", owner.Id);
        team.IsDeleted = true;
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.IsSubdomainAvailableAsync("deleted-subdomain");

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region Helper Methods

    private async Task<ApplicationUser> CreateTestUserAsync(string email, string firstName, string lastName, Guid? id = null)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true,
            IsActive = true,
            Status = UserStatus.Active,
            CreatedOn = DateTime.UtcNow
        };
        
        if (id.HasValue)
        {
            user.Id = id.Value;
        }

        var result = await _userManager.CreateAsync(user, "Password123!");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return user;
    }

    private async Task<Team> CreateTestTeamAsync(string name, string subdomain, Guid ownerId)
    {
        var team = new Team
        {
            Name = name,
            Subdomain = subdomain,
            OwnerId = ownerId,
            Status = TeamStatus.Active,
            Tier = TeamTier.Free,
            PrimaryColor = "#000000",
            SecondaryColor = "#FFFFFF",
            CreatedOn = DateTime.UtcNow
        };

        DbContext.Teams.Add(team);

        var userTeam = new UserTeam
        {
            UserId = ownerId,
            TeamId = team.Id,
            Role = TeamRole.TeamOwner,
            MemberType = MemberType.Coach,
            IsActive = true,
            IsDefault = true,
            JoinedOn = DateTime.UtcNow,
            CreatedOn = DateTime.UtcNow
        };

        DbContext.UserTeams.Add(userTeam);
        await DbContext.SaveChangesAsync();

        return team;
    }

    #endregion
} 