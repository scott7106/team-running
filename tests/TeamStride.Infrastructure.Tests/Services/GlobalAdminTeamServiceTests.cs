using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using TeamStride.Application.Common.Services;
using TeamStride.Application.Teams.Dtos;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;
using TeamStride.Infrastructure.Mapping;
using TeamStride.Infrastructure.Services;

namespace TeamStride.Infrastructure.Tests.Services;

public class GlobalAdminTeamServiceTests : BaseSecuredTest
{
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
    }

    [Fact]
    public async Task GetTeamsAsync_AsGlobalAdmin_ShouldReturnAllTeams()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var owner1 = await CreateTestUserAsync("owner1@test.com", "Owner1", "User");
        var owner2 = await CreateTestUserAsync("owner2@test.com", "Owner2", "User");
        
        var team1 = await CreateTestTeamAsync("Team Alpha", "team-alpha", owner1.Id);
        var team2 = await CreateTestTeamAsync("Team Beta", "team-beta", owner2.Id);

        // Act
        var result = await service.GetTeamsAsync();

        // Assert
        result.Items.Count.ShouldBe(2);
        result.Items.ShouldContain(t => t.Name == "Team Alpha");
        result.Items.ShouldContain(t => t.Name == "Team Beta");
        result.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task GetTeamsAsync_AsNonGlobalAdmin_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        SetupStandardUserContext(teamId);
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .ThrowsAsync(new UnauthorizedAccessException("Global admin privileges required"));

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => service.GetTeamsAsync());
    }

    [Fact]
    public async Task GetTeamsAsync_WithSearchQuery_ShouldFilterResults()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var owner1 = await CreateTestUserAsync("owner1@test.com", "Owner1", "User");
        var owner2 = await CreateTestUserAsync("owner2@test.com", "Owner2", "User");
        
        await CreateTestTeamAsync("Alpha Team", "alpha-team", owner1.Id);
        await CreateTestTeamAsync("Beta Team", "beta-team", owner2.Id);

        // Act
        var result = await service.GetTeamsAsync(searchQuery: "alpha");

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items.First().Name.ShouldBe("Alpha Team");
    }

    [Fact]
    public async Task GetTeamByIdAsync_AsGlobalAdmin_ShouldReturnTeam()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner");
        var team = await CreateTestTeamAsync("Test Team", "test-team", owner.Id);

        // Act
        var result = await service.GetTeamByIdAsync(team.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Test Team");
        result.Subdomain.ShouldBe("test-team");
        result.OwnerEmail.ShouldBe("owner@test.com");
    }

    [Fact]
    public async Task CreateTeamWithNewOwnerAsync_AsGlobalAdmin_ShouldCreateTeamAndUser()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var dto = new CreateTeamWithNewOwnerDto
        {
            Name = "New Team",
            Subdomain = "new-team",
            OwnerEmail = "newowner@test.com",
            OwnerFirstName = "New",
            OwnerLastName = "Owner",
            OwnerPassword = "Password123!",
            Tier = TeamTier.Free
        };

        // Act
        var result = await service.CreateTeamWithNewOwnerAsync(dto);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("New Team");
        result.Subdomain.ShouldBe("new-team");
        result.OwnerEmail.ShouldBe("newowner@test.com");
        result.Tier.ShouldBe(TeamTier.Free);

        // Verify user was created
        var createdUser = await _userManager.FindByEmailAsync("newowner@test.com");
        createdUser.ShouldNotBeNull();
        createdUser.FirstName.ShouldBe("New");
        createdUser.LastName.ShouldBe("Owner");
    }

    [Fact]
    public async Task CreateTeamWithExistingOwnerAsync_AsGlobalAdmin_ShouldCreateTeam()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var existingOwner = await CreateTestUserAsync("existing@test.com", "Existing", "Owner");

        var dto = new CreateTeamWithExistingOwnerDto
        {
            Name = "New Team",
            Subdomain = "new-team",
            OwnerId = existingOwner.Id,
            Tier = TeamTier.Premium,
            PrimaryColor = "#FF0000",
            SecondaryColor = "#00FF00"
        };

        // Act
        var result = await service.CreateTeamWithExistingOwnerAsync(dto);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("New Team");
        result.Subdomain.ShouldBe("new-team");
        result.OwnerEmail.ShouldBe("existing@test.com");
        result.Tier.ShouldBe(TeamTier.Premium);
        result.PrimaryColor.ShouldBe("#FF0000");
        result.SecondaryColor.ShouldBe("#00FF00");
    }

    [Fact]
    public async Task UpdateTeamAsync_AsGlobalAdmin_ShouldUpdateTeam()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner");
        var team = await CreateTestTeamAsync("Original Team", "original-team", owner.Id);

        var dto = new GlobalAdminUpdateTeamDto
        {
            Name = "Updated Team",
            Subdomain = "updated-team",
            Tier = TeamTier.Premium,
            Status = TeamStatus.Active,
            PrimaryColor = "#123456",
            SecondaryColor = "#ABCDEF"
        };

        // Act
        var result = await service.UpdateTeamAsync(team.Id, dto);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Updated Team");
        result.Subdomain.ShouldBe("updated-team");
        result.Tier.ShouldBe(TeamTier.Premium);
        result.PrimaryColor.ShouldBe("#123456");
        result.SecondaryColor.ShouldBe("#ABCDEF");
    }

    [Fact]
    public async Task DeleteTeamAsync_AsGlobalAdmin_ShouldSoftDeleteTeam()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner");
        var team = await CreateTestTeamAsync("Team to Delete", "team-to-delete", owner.Id);

        // Act
        await service.DeleteTeamAsync(team.Id);

        // Assert
        var deletedTeam = await DbContext.Teams.FindAsync(team.Id);
        deletedTeam.ShouldNotBeNull();
        deletedTeam.IsDeleted.ShouldBeTrue();
        deletedTeam.DeletedOn.ShouldNotBeNull();
    }

    [Fact]
    public async Task TransferOwnershipAsync_AsGlobalAdmin_ShouldTransferOwnership()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var oldOwner = await CreateTestUserAsync("oldowner@test.com", "Old", "Owner");
        var newOwner = await CreateTestUserAsync("newowner@test.com", "New", "Owner");
        var team = await CreateTestTeamAsync("Transfer Team", "transfer-team", oldOwner.Id);

        var dto = new GlobalAdminTransferOwnershipDto
        {
            NewOwnerId = newOwner.Id,
            Message = "Transferring ownership"
        };

        // Act
        var result = await service.TransferOwnershipAsync(team.Id, dto);

        // Assert
        result.ShouldNotBeNull();
        result.OwnerId.ShouldBe(newOwner.Id);
        result.OwnerEmail.ShouldBe("newowner@test.com");

        // Verify ownership change in database
        var updatedTeam = await DbContext.Teams.FindAsync(team.Id);
        updatedTeam.ShouldNotBeNull();
        updatedTeam.OwnerId.ShouldBe(newOwner.Id);
    }

    [Fact]
    public async Task IsSubdomainAvailableAsync_WhenAvailable_ShouldReturnTrue()
    {
        // Arrange
        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        // Act
        var result = await service.IsSubdomainAvailableAsync("available-subdomain");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsSubdomainAvailableAsync_WhenTaken_ShouldReturnFalse()
    {
        // Arrange
        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        await CreateTestTeamAsync("Test Team", "taken-subdomain", owner.Id);

        // Act
        var result = await service.IsSubdomainAvailableAsync("taken-subdomain");

        // Assert
        result.ShouldBeFalse();
    }

    #region GetTeamsAsync Advanced Filtering Tests

    [Fact]
    public async Task GetTeamsAsync_WithStatusFilter_ShouldFilterResults()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        var activeTeam = await CreateTestTeamAsync("Active Team", "active-team", owner.Id);
        var suspendedTeam = await CreateTestTeamAsync("Suspended Team", "suspended-team", owner.Id);
        
        // Update one team to suspended status
        suspendedTeam.Status = TeamStatus.Suspended;
        await DbContext.SaveChangesAsync();

        // Act
        var result = await service.GetTeamsAsync(status: TeamStatus.Active);

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items.First().Name.ShouldBe("Active Team");
    }

    [Fact]
    public async Task GetTeamsAsync_WithTierFilter_ShouldFilterResults()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        var freeTeam = await CreateTestTeamAsync("Free Team", "free-team", owner.Id);
        var premiumTeam = await CreateTestTeamAsync("Premium Team", "premium-team", owner.Id);
        
        // Update one team to premium tier
        premiumTeam.Tier = TeamTier.Premium;
        await DbContext.SaveChangesAsync();

        // Act
        var result = await service.GetTeamsAsync(tier: TeamTier.Premium);

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items.First().Name.ShouldBe("Premium Team");
    }

    [Fact]
    public async Task GetTeamsAsync_WithDateFilters_ShouldFilterResults()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        var team1 = await CreateTestTeamAsync("Team 1", "team-1", owner.Id);
        var team2 = await CreateTestTeamAsync("Team 2", "team-2", owner.Id);
        
        // Set different expiration dates
        var futureDate = DateTime.UtcNow.AddDays(30);
        var farFutureDate = DateTime.UtcNow.AddDays(60);
        
        team1.ExpiresOn = futureDate;
        team2.ExpiresOn = farFutureDate;
        await DbContext.SaveChangesAsync();

        // Act
        var result = await service.GetTeamsAsync(
            expiresOnFrom: DateTime.UtcNow.AddDays(45), 
            expiresOnTo: DateTime.UtcNow.AddDays(90));

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items.First().Name.ShouldBe("Team 2");
    }

    [Fact]
    public async Task GetTeamsAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        
        // Create multiple teams
        for (int i = 1; i <= 5; i++)
        {
            await CreateTestTeamAsync($"Team {i:D2}", $"team-{i:D2}", owner.Id);
        }

        // Act
        var result = await service.GetTeamsAsync(pageNumber: 2, pageSize: 2);

        // Assert
        result.Items.Count.ShouldBe(2);
        result.TotalCount.ShouldBe(5);
        result.PageNumber.ShouldBe(2);
        result.TotalPages.ShouldBe(3);
    }

    #endregion

    #region GetTeamByIdAsync Additional Tests

    [Fact]
    public async Task GetTeamByIdAsync_WhenTeamNotFound_ShouldThrowException()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var nonExistentTeamId = Guid.NewGuid();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => service.GetTeamByIdAsync(nonExistentTeamId));
        
        exception.Message.ShouldContain($"Team with ID {nonExistentTeamId} not found");
    }

    [Fact]
    public async Task GetTeamByIdAsync_AsNonGlobalAdmin_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        SetupStandardUserContext(teamId);
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .ThrowsAsync(new UnauthorizedAccessException("Global admin privileges required"));

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => service.GetTeamByIdAsync(teamId));
    }

    #endregion

    #region GetDeletedTeamsAsync Tests

    [Fact]
    public async Task GetDeletedTeamsAsync_AsGlobalAdmin_ShouldReturnDeletedTeams()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        var activeTeam = await CreateTestTeamAsync("Active Team", "active-team", owner.Id);
        var deletedTeam = await CreateTestTeamAsync("Deleted Team", "deleted-team", owner.Id);
        
        // Soft delete one team
        deletedTeam.IsDeleted = true;
        deletedTeam.DeletedOn = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();

        // Act
        var result = await service.GetDeletedTeamsAsync();

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items.First().Name.ShouldBe("Deleted Team");
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetDeletedTeamsAsync_WithSearchQuery_ShouldFilterResults()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        var deletedTeam1 = await CreateTestTeamAsync("Alpha Deleted", "alpha-deleted", owner.Id);
        var deletedTeam2 = await CreateTestTeamAsync("Beta Deleted", "beta-deleted", owner.Id);
        
        // Soft delete both teams
        deletedTeam1.IsDeleted = true;
        deletedTeam1.DeletedOn = DateTime.UtcNow;
        deletedTeam2.IsDeleted = true;
        deletedTeam2.DeletedOn = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();

        // Act
        var result = await service.GetDeletedTeamsAsync(searchQuery: "alpha");

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items.First().Name.ShouldBe("Alpha Deleted");
    }

    [Fact]
    public async Task GetDeletedTeamsAsync_AsNonGlobalAdmin_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        SetupStandardUserContext(teamId);
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .ThrowsAsync(new UnauthorizedAccessException("Global admin privileges required"));

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => service.GetDeletedTeamsAsync());
    }

    #endregion

    #region PermanentlyDeleteTeamAsync Tests

    [Fact]
    public async Task PermanentlyDeleteTeamAsync_AsGlobalAdmin_ShouldPermanentlyDeleteTeam()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner");
        var team = await CreateTestTeamAsync("Team to Delete", "team-to-delete", owner.Id);

        // Act
        await service.PermanentlyDeleteTeamAsync(team.Id);

        // Assert
        var deletedTeam = await DbContext.Teams.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == team.Id);
        deletedTeam.ShouldBeNull();

        // Verify UserTeam relationship was also deleted
        var userTeam = await DbContext.UserTeams.IgnoreQueryFilters().FirstOrDefaultAsync(ut => ut.TeamId == team.Id);
        userTeam.ShouldBeNull();
    }

    [Fact]
    public async Task PermanentlyDeleteTeamAsync_WhenTeamNotFound_ShouldThrowException()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var nonExistentTeamId = Guid.NewGuid();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => service.PermanentlyDeleteTeamAsync(nonExistentTeamId));
        
        exception.Message.ShouldContain($"Team with ID {nonExistentTeamId} not found");
    }

    [Fact]
    public async Task PermanentlyDeleteTeamAsync_AsNonGlobalAdmin_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        SetupStandardUserContext(teamId);
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .ThrowsAsync(new UnauthorizedAccessException("Global admin privileges required"));

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => service.PermanentlyDeleteTeamAsync(teamId));
    }

    #endregion

    #region RecoverTeamAsync Tests

    [Fact]
    public async Task RecoverTeamAsync_AsGlobalAdmin_ShouldRecoverDeletedTeam()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner");
        var team = await CreateTestTeamAsync("Team to Recover", "team-to-recover", owner.Id);

        // Soft delete the team
        team.IsDeleted = true;
        team.DeletedOn = DateTime.UtcNow;
        team.Status = TeamStatus.Suspended;

        // Deactivate user-team relationship
        var userTeam = await DbContext.UserTeams.FirstAsync(ut => ut.TeamId == team.Id);
        userTeam.IsActive = false;
        await DbContext.SaveChangesAsync();

        // Act
        var result = await service.RecoverTeamAsync(team.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Team to Recover");

        // Verify team is recovered
        var recoveredTeam = await DbContext.Teams.FindAsync(team.Id);
        recoveredTeam.ShouldNotBeNull();
        recoveredTeam.IsDeleted.ShouldBeFalse();
        recoveredTeam.DeletedOn.ShouldBeNull();
        recoveredTeam.Status.ShouldBe(TeamStatus.Active);

        // Verify user-team relationship is reactivated
        var recoveredUserTeam = await DbContext.UserTeams.FirstAsync(ut => ut.TeamId == team.Id);
        recoveredUserTeam.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task RecoverTeamAsync_WhenTeamNotFound_ShouldThrowException()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var nonExistentTeamId = Guid.NewGuid();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => service.RecoverTeamAsync(nonExistentTeamId));
        
        exception.Message.ShouldContain($"Team with ID {nonExistentTeamId} not found");
    }

    [Fact]
    public async Task RecoverTeamAsync_WhenTeamNotDeleted_ShouldThrowException()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner");
        var team = await CreateTestTeamAsync("Active Team", "active-team", owner.Id);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => service.RecoverTeamAsync(team.Id));
        
        exception.Message.ShouldContain($"Team with ID {team.Id} is not deleted");
    }

    [Fact]
    public async Task RecoverTeamAsync_WhenSubdomainNowTaken_ShouldThrowException()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner");
        var deletedTeam = await CreateTestTeamAsync("Deleted Team", "conflicting-subdomain", owner.Id);
        
        // Soft delete the team
        deletedTeam.IsDeleted = true;
        deletedTeam.DeletedOn = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();

        // Create another team with the same subdomain
        var newOwner = await CreateTestUserAsync("newowner@test.com", "New", "Owner");
        await CreateTestTeamAsync("New Team", "conflicting-subdomain", newOwner.Id);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => service.RecoverTeamAsync(deletedTeam.Id));
        
        exception.Message.ShouldContain("Cannot recover team: subdomain 'conflicting-subdomain' is now taken");
    }

    #endregion

    #region Error Scenario Tests

    [Fact]
    public async Task CreateTeamWithNewOwnerAsync_WhenSubdomainTaken_ShouldThrowException()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        // Create existing team with subdomain
        var existingOwner = await CreateTestUserAsync("existing@test.com", "Existing", "Owner");
        await CreateTestTeamAsync("Existing Team", "taken-subdomain", existingOwner.Id);

        var dto = new CreateTeamWithNewOwnerDto
        {
            Name = "New Team",
            Subdomain = "taken-subdomain", // Same subdomain
            OwnerEmail = "newowner@test.com",
            OwnerFirstName = "New",
            OwnerLastName = "Owner",
            OwnerPassword = "Password123!",
            Tier = TeamTier.Free
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => service.CreateTeamWithNewOwnerAsync(dto));
        
        exception.Message.ShouldContain("Subdomain 'taken-subdomain' is already taken");
    }

    [Fact]
    public async Task CreateTeamWithExistingOwnerAsync_WhenOwnerNotFound_ShouldThrowException()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var dto = new CreateTeamWithExistingOwnerDto
        {
            Name = "New Team",
            Subdomain = "new-team",
            OwnerId = Guid.NewGuid(), // Non-existent user
            Tier = TeamTier.Free
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => service.CreateTeamWithExistingOwnerAsync(dto));
        
        exception.Message.ShouldContain($"User with ID {dto.OwnerId} not found");
    }

    [Fact]
    public async Task UpdateTeamAsync_WhenTeamNotFound_ShouldThrowException()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var nonExistentTeamId = Guid.NewGuid();
        var dto = new GlobalAdminUpdateTeamDto
        {
            Name = "Updated Team"
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => service.UpdateTeamAsync(nonExistentTeamId, dto));
        
        exception.Message.ShouldContain($"Team with ID {nonExistentTeamId} not found");
    }

    [Fact]
    public async Task UpdateTeamAsync_WhenSubdomainTaken_ShouldThrowException()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        var team1 = await CreateTestTeamAsync("Team 1", "team-1", owner.Id);
        var team2 = await CreateTestTeamAsync("Team 2", "team-2", owner.Id);

        var dto = new GlobalAdminUpdateTeamDto
        {
            Subdomain = "team-2" // Try to use team2's subdomain
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => service.UpdateTeamAsync(team1.Id, dto));
        
        exception.Message.ShouldContain("Subdomain 'team-2' is already taken");
    }

    [Fact]
    public async Task DeleteTeamAsync_WhenTeamNotFound_ShouldThrowException()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var nonExistentTeamId = Guid.NewGuid();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => service.DeleteTeamAsync(nonExistentTeamId));
        
        exception.Message.ShouldContain($"Team with ID {nonExistentTeamId} not found");
    }

    [Fact]
    public async Task DeleteTeamAsync_WhenTeamAlreadyDeleted_ShouldThrowException()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var owner = await CreateTestUserAsync("owner@test.com", "Team", "Owner");
        var team = await CreateTestTeamAsync("Team", "team", owner.Id);
        
        // Mark as already deleted
        team.IsDeleted = true;
        team.DeletedOn = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => service.DeleteTeamAsync(team.Id));
        
        exception.Message.ShouldContain($"Team with ID {team.Id} is already deleted");
    }

    [Fact]
    public async Task TransferOwnershipAsync_WhenNewOwnerNotFound_ShouldThrowException()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        var team = await CreateTestTeamAsync("Team", "team", owner.Id);

        var dto = new GlobalAdminTransferOwnershipDto
        {
            NewOwnerId = Guid.NewGuid(), // Non-existent user
            Message = "Transfer ownership"
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => service.TransferOwnershipAsync(team.Id, dto));
        
        exception.Message.ShouldContain($"User with ID {dto.NewOwnerId} not found");
    }

    [Fact]
    public async Task TransferOwnershipAsync_WhenNewOwnerIsSameAsCurrentOwner_ShouldThrowException()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        var team = await CreateTestTeamAsync("Team", "team", owner.Id);

        var dto = new GlobalAdminTransferOwnershipDto
        {
            NewOwnerId = owner.Id, // Same as current owner
            Message = "Transfer ownership"
        };

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => service.TransferOwnershipAsync(team.Id, dto));
        
        exception.Message.ShouldContain($"User {owner.Email} is already the owner of this team");
    }

    [Fact]
    public async Task IsSubdomainAvailableAsync_WhenExcludingTeam_ShouldReturnTrueForSameTeam()
    {
        // Arrange
        var service = new GlobalAdminTeamService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);

        var owner = await CreateTestUserAsync("owner@test.com", "Owner", "User");
        var team = await CreateTestTeamAsync("Test Team", "test-subdomain", owner.Id);

        // Act
        var result = await service.IsSubdomainAvailableAsync("test-subdomain", team.Id);

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    // Helper methods
    private async Task<ApplicationUser> CreateTestUserAsync(string email, string firstName, string lastName, Guid? id = null)
    {
        var user = new ApplicationUser
        {
            Id = id ?? Guid.NewGuid(),
            Email = email,
            NormalizedEmail = email.ToUpper(),
            UserName = email,
            NormalizedUserName = email.ToUpper(),
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true,
            Status = UserStatus.Active,
            CreatedOn = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, "TestPassword123!");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return user;
    }

    private async Task<Team> CreateTestTeamAsync(string name, string subdomain, Guid ownerId, Guid? id = null)
    {
        var team = new Team
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Subdomain = subdomain,
            OwnerId = ownerId,
            Tier = TeamTier.Free,
            Status = TeamStatus.Active,
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
            JoinedOn = DateTime.UtcNow,
            CreatedOn = DateTime.UtcNow
        };

        DbContext.UserTeams.Add(userTeam);
        await DbContext.SaveChangesAsync();
        return team;
    }
} 