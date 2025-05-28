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

public class TeamServiceTests : BaseIntegrationTest
{
    private readonly TeamService _service;
    private readonly Mock<IAuthorizationService> _mockAuthorizationService;
    private readonly Mock<ILogger<TeamService>> _mockLogger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;

    // Test data
    private Guid _currentUserId;
    private Guid _teamId;
    private ApplicationUser? _teamOwner;
    private Team? _team;

    public TeamServiceTests()
    {
        _mockAuthorizationService = new Mock<IAuthorizationService>();
        _mockLogger = new Mock<ILogger<TeamService>>();
        _userManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();

        // Setup test user as Team Owner, not Global Admin
        _currentUserId = Guid.NewGuid();
        _teamId = Guid.NewGuid();
        
        MockCurrentUserService.Setup(x => x.UserId).Returns(_currentUserId);
        MockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(false); // Not global admin
        
        MockCurrentTeamService.Setup(x => x.TeamId).Returns(_teamId);

        _service = new TeamService(
            DbContext,
            _mockAuthorizationService.Object,
            MockCurrentUserService.Object,
            MockCurrentTeamService.Object,
            _userManager,
            _mapper,
            _mockLogger.Object);

        // Initialize test data
        InitializeTestDataAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeTestDataAsync()
    {
        // Create team owner user
        _teamOwner = await CreateTestUserAsync("owner@test.com", "Team", "Owner", _currentUserId);
        
        // Create test team
        _team = await CreateTestTeamAsync("Test Team", "test-team", _teamOwner.Id, _teamId);
        
        // Create owner relationship
        var userTeam = new UserTeam
        {
            UserId = _teamOwner.Id,
            TeamId = _team.Id,
            Role = TeamRole.TeamOwner,
            MemberType = MemberType.Coach,
            IsActive = true,
            IsDefault = true,
            JoinedOn = DateTime.UtcNow,
            CreatedOn = DateTime.UtcNow
        };
        
        DbContext.UserTeams.Add(userTeam);
        await DbContext.SaveChangesAsync();
    }

    #region GetTeamsAsync Tests

    [Fact]
    public async Task GetTeamsAsync_WhenUserIsNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        MockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(false);

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _service.GetTeamsAsync());
    }

    [Fact]
    public async Task GetTeamsAsync_WhenUserHasTeamAccess_ShouldReturnUserTeams()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(It.IsAny<Guid>(), It.IsAny<TeamRole>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.GetTeamsAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(1);
        result.TotalCount.ShouldBe(1);
        
        var teamDto = result.Items.First();
        teamDto.Name.ShouldBe("Test Team");
        teamDto.Subdomain.ShouldBe("test-team");
    }

    [Fact]
    public async Task GetTeamsAsync_WithSearchQuery_ShouldFilterResults()
    {
        // Arrange
        var anotherOwner = await CreateTestUserAsync("owner2@test.com", "Another", "Owner");
        await CreateTestTeamAsync("Alpha Team", "alpha-team", anotherOwner.Id);
        await CreateUserTeamRelationship(_currentUserId, _teamId);

        // Act
        var result = await _service.GetTeamsAsync(searchQuery: "test");

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items.First().Name.ShouldBe("Test Team");
    }

    [Fact]
    public async Task GetTeamsAsync_WithStatusFilter_ShouldFilterResults()
    {
        // Arrange
        var suspendedTeam = await CreateTestTeamAsync("Suspended Team", "suspended-team", _teamOwner!.Id);
        suspendedTeam.Status = TeamStatus.Suspended;
        await CreateUserTeamRelationship(_currentUserId, suspendedTeam.Id);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetTeamsAsync(status: TeamStatus.Active);

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items.First().Status.ShouldBe(TeamStatus.Active);
    }

    #endregion

    #region GetTeamByIdAsync Tests

    [Fact]
    public async Task GetTeamByIdAsync_WhenTeamExists_ShouldReturnTeam()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(_teamId, TeamRole.TeamMember))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.GetTeamByIdAsync(_teamId);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Test Team");
        result.Subdomain.ShouldBe("test-team");
        result.Owner.ShouldNotBeNull();
        result.Owner.Email.ShouldBe("owner@test.com");
    }

    [Fact]
    public async Task GetTeamByIdAsync_WhenTeamDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var nonExistentTeamId = Guid.NewGuid();
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(nonExistentTeamId, TeamRole.TeamMember))
            .Returns(Task.CompletedTask);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _service.GetTeamByIdAsync(nonExistentTeamId));
    }

    #endregion

    #region GetTeamBySubdomainAsync Tests

    [Fact]
    public async Task GetTeamBySubdomainAsync_WhenTeamExists_ShouldReturnTeam()
    {
        // Act
        var result = await _service.GetTeamBySubdomainAsync("test-team");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Test Team");
        result.Subdomain.ShouldBe("test-team");
    }

    [Fact]
    public async Task GetTeamBySubdomainAsync_WhenTeamDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _service.GetTeamBySubdomainAsync("non-existent"));
    }

    [Fact]
    public async Task GetTeamBySubdomainAsync_WhenNullSubdomain_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => _service.GetTeamBySubdomainAsync(null!));
    }

    #endregion

    #region CreateTeamAsync Tests

    [Fact]
    public async Task CreateTeamAsync_RequiresGlobalAdmin_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .ThrowsAsync(new UnauthorizedAccessException("Global admin privileges required"));

        var dto = new CreateTeamDto
        {
            Name = "New Team",
            Subdomain = "new-team",
            OwnerEmail = "newowner@test.com"
        };

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => _service.CreateTeamAsync(dto));
    }

    #endregion

    #region UpdateTeamAsync Tests

    [Fact]
    public async Task UpdateTeamAsync_WhenValidData_ShouldUpdateTeam()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(_teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var dto = new UpdateTeamDto
        {
            Name = "Updated Team Name",
            Status = TeamStatus.Suspended
        };

        // Act
        var result = await _service.UpdateTeamAsync(_teamId, dto);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Updated Team Name");
        result.Status.ShouldBe(TeamStatus.Suspended);
        result.ModifiedOn.ShouldNotBeNull();
    }

    [Fact]
    public async Task UpdateTeamAsync_WhenTeamDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var nonExistentTeamId = Guid.NewGuid();
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(nonExistentTeamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var dto = new UpdateTeamDto { Name = "Updated Name" };

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _service.UpdateTeamAsync(nonExistentTeamId, dto));
    }

    [Fact]
    public async Task UpdateTeamAsync_WhenNullDto_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => _service.UpdateTeamAsync(_teamId, null!));
    }

    #endregion

    #region DeleteTeamAsync Tests

    [Fact]
    public async Task DeleteTeamAsync_WhenValidTeam_ShouldSoftDeleteTeam()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireTeamOwnershipAsync(_teamId))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteTeamAsync(_teamId);

        // Assert
        var deletedTeam = await DbContext.Teams.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == _teamId);
        
        deletedTeam.ShouldNotBeNull();
        deletedTeam.IsDeleted.ShouldBeTrue();
        deletedTeam.DeletedOn.ShouldNotBeNull();
        deletedTeam.Status.ShouldBe(TeamStatus.Suspended);

        // Verify user team relationships are deactivated
        var userTeams = await DbContext.UserTeams
            .Where(ut => ut.TeamId == _teamId)
            .ToListAsync();
        
        userTeams.ShouldAllBe(ut => !ut.IsActive);
    }

    [Fact]
    public async Task DeleteTeamAsync_WhenTeamDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var nonExistentTeamId = Guid.NewGuid();
        _mockAuthorizationService.Setup(x => x.RequireTeamOwnershipAsync(nonExistentTeamId))
            .Returns(Task.CompletedTask);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _service.DeleteTeamAsync(nonExistentTeamId));
    }

    #endregion

    #region UpdateSubscriptionAsync Tests

    [Fact]
    public async Task UpdateSubscriptionAsync_WhenValidUpgrade_ShouldUpdateSubscription()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireTeamOwnershipAsync(_teamId))
            .Returns(Task.CompletedTask);

        var dto = new UpdateSubscriptionDto
        {
            NewTier = TeamTier.Premium,
            ExpiresOn = DateTime.UtcNow.AddYears(1)
        };

        // Act
        var result = await _service.UpdateSubscriptionAsync(_teamId, dto);

        // Assert
        result.ShouldNotBeNull();
        result.Tier.ShouldBe(TeamTier.Premium);
        result.ExpiresOn.ShouldNotBeNull();
    }

    [Fact]
    public async Task UpdateSubscriptionAsync_WhenDowngradeExceedsLimits_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireTeamOwnershipAsync(_teamId))
            .Returns(Task.CompletedTask);

        // Set team to Premium with many athletes
        _team!.Tier = TeamTier.Premium;
        await DbContext.SaveChangesAsync();

        // Add athletes beyond Free tier limit (7)
        for (int i = 0; i < 10; i++)
        {
            var athlete = await CreateTestUserAsync($"athlete{i}@test.com", $"Athlete{i}", "User");
            await CreateUserTeamRelationship(athlete.Id, _teamId, TeamRole.TeamMember, MemberType.Athlete);
        }

        var dto = new UpdateSubscriptionDto { NewTier = TeamTier.Free };

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _service.UpdateSubscriptionAsync(_teamId, dto));
    }

    #endregion

    #region UpdateBrandingAsync Tests

    [Fact]
    public async Task UpdateBrandingAsync_WhenValidData_ShouldUpdateBranding()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(_teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        // Set team to Premium to allow custom branding
        _team!.Tier = TeamTier.Premium;
        await DbContext.SaveChangesAsync();

        var dto = new UpdateTeamBrandingDto
        {
            PrimaryColor = "#FF0000",
            SecondaryColor = "#00FF00",
            LogoUrl = "https://example.com/logo.png"
        };

        // Act
        var result = await _service.UpdateBrandingAsync(_teamId, dto);

        // Assert
        result.ShouldNotBeNull();
        result.PrimaryColor.ShouldBe("#FF0000");
        result.SecondaryColor.ShouldBe("#00FF00");
        result.LogoUrl.ShouldBe("https://example.com/logo.png");
    }

    [Fact]
    public async Task UpdateBrandingAsync_WhenFreeTier_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(_teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        // Ensure team is Free tier
        _team!.Tier = TeamTier.Free;
        await DbContext.SaveChangesAsync();

        var dto = new UpdateTeamBrandingDto { PrimaryColor = "#FF0000" };

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _service.UpdateBrandingAsync(_teamId, dto));
    }

    #endregion

    #region GetTeamMembersAsync Tests

    [Fact]
    public async Task GetTeamMembersAsync_WhenTeamHasMembers_ShouldReturnPaginatedMembers()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(_teamId, TeamRole.TeamMember))
            .Returns(Task.CompletedTask);

        // Add some team members
        var member1 = await CreateTestUserAsync("member1@test.com", "Member1", "User");
        var member2 = await CreateTestUserAsync("member2@test.com", "Member2", "User");
        
        await CreateUserTeamRelationship(member1.Id, _teamId, TeamRole.TeamMember);
        await CreateUserTeamRelationship(member2.Id, _teamId, TeamRole.TeamAdmin);

        // Act
        var result = await _service.GetTeamMembersAsync(_teamId);

        // Assert
        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(3); // Owner + 2 members
        result.Items.Count.ShouldBe(3);
        
        // Should be ordered by role (owner first, then admin, then members)
        result.Items.First().Role.ShouldBe(TeamRole.TeamOwner);
        result.Items.Skip(1).First().Role.ShouldBe(TeamRole.TeamAdmin);
    }

    [Fact]
    public async Task GetTeamMembersAsync_WithRoleFilter_ShouldFilterByRole()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(_teamId, TeamRole.TeamMember))
            .Returns(Task.CompletedTask);

        var member = await CreateTestUserAsync("member@test.com", "Member", "User");
        await CreateUserTeamRelationship(member.Id, _teamId, TeamRole.TeamMember);

        // Act
        var result = await _service.GetTeamMembersAsync(_teamId, role: TeamRole.TeamOwner);

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items.First().Role.ShouldBe(TeamRole.TeamOwner);
    }

    [Fact]
    public async Task GetTeamMembersAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(_teamId, TeamRole.TeamMember))
            .Returns(Task.CompletedTask);

        // Add many members
        for (int i = 1; i <= 15; i++)
        {
            var member = await CreateTestUserAsync($"member{i}@test.com", $"Member{i:D2}", "User");
            await CreateUserTeamRelationship(member.Id, _teamId, TeamRole.TeamMember);
        }

        // Act
        var result = await _service.GetTeamMembersAsync(_teamId, pageNumber: 2, pageSize: 10);

        // Assert
        result.TotalCount.ShouldBe(16); // 15 members + 1 owner
        result.PageNumber.ShouldBe(2);
        result.Items.Count.ShouldBe(6); // Remaining items on page 2
    }

    #endregion

    #region UpdateMemberRoleAsync Tests

    [Fact]
    public async Task UpdateMemberRoleAsync_WhenValidRoleChange_ShouldUpdateRole()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(_teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var member = await CreateTestUserAsync("member@test.com", "Member", "User");
        await CreateUserTeamRelationship(member.Id, _teamId, TeamRole.TeamMember);

        // Act
        var result = await _service.UpdateMemberRoleAsync(_teamId, member.Id, TeamRole.TeamAdmin);

        // Assert
        result.ShouldNotBeNull();
        result.Role.ShouldBe(TeamRole.TeamAdmin);

        // Verify in database
        var userTeam = await DbContext.UserTeams
            .FirstOrDefaultAsync(ut => ut.UserId == member.Id && ut.TeamId == _teamId);
        userTeam.ShouldNotBeNull();
        userTeam.Role.ShouldBe(TeamRole.TeamAdmin);
        userTeam.ModifiedOn.ShouldNotBeNull();
    }

    [Fact]
    public async Task UpdateMemberRoleAsync_WhenUserNotMember_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(_teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var nonMember = await CreateTestUserAsync("nonmember@test.com", "Non", "Member");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _service.UpdateMemberRoleAsync(_teamId, nonMember.Id, TeamRole.TeamAdmin));
    }

    [Fact]
    public async Task UpdateMemberRoleAsync_WhenChangingOwnerRole_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(_teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _service.UpdateMemberRoleAsync(_teamId, _teamOwner!.Id, TeamRole.TeamMember));
    }

    [Fact]
    public async Task UpdateMemberRoleAsync_WhenPromotingToOwner_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(_teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var member = await CreateTestUserAsync("member@test.com", "Member", "User");
        await CreateUserTeamRelationship(member.Id, _teamId, TeamRole.TeamMember);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _service.UpdateMemberRoleAsync(_teamId, member.Id, TeamRole.TeamOwner));
    }

    #endregion

    #region RemoveMemberAsync Tests

    [Fact]
    public async Task RemoveMemberAsync_WhenValidMember_ShouldDeactivateMember()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(_teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var member = await CreateTestUserAsync("member@test.com", "Member", "User");
        await CreateUserTeamRelationship(member.Id, _teamId, TeamRole.TeamMember);

        // Act
        await _service.RemoveMemberAsync(_teamId, member.Id);

        // Assert
        var userTeam = await DbContext.UserTeams
            .FirstOrDefaultAsync(ut => ut.UserId == member.Id && ut.TeamId == _teamId);
        
        userTeam.ShouldNotBeNull();
        userTeam.IsActive.ShouldBeFalse();
        userTeam.ModifiedOn.ShouldNotBeNull();
    }

    [Fact]
    public async Task RemoveMemberAsync_WhenUserNotMember_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(_teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var nonMember = await CreateTestUserAsync("nonmember@test.com", "Non", "Member");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _service.RemoveMemberAsync(_teamId, nonMember.Id));
    }

    [Fact]
    public async Task RemoveMemberAsync_WhenRemovingOwner_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(_teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _service.RemoveMemberAsync(_teamId, _teamOwner!.Id));
    }

    #endregion

    #region InitiateOwnershipTransferAsync Tests

    [Fact]
    public async Task InitiateOwnershipTransferAsync_WhenValidData_ShouldCreateTransfer()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireTeamOwnershipAsync(_teamId))
            .Returns(Task.CompletedTask);

        var dto = new InitiateOwnershipTransferDto
        {
            NewOwnerEmail = "newowner@test.com",
            NewOwnerFirstName = "New",
            NewOwnerLastName = "Owner",
            Message = "Transfer ownership test"
        };

        // Act
        var result = await _service.InitiateOwnershipTransferAsync(_teamId, dto);

        // Assert
        result.ShouldNotBeNull();
        result.TeamId.ShouldBe(_teamId);
        result.NewOwnerEmail.ShouldBe("newowner@test.com");
        result.Status.ShouldBe(OwnershipTransferStatus.Pending);
        result.TransferToken.ShouldNotBeNullOrEmpty();
        result.ExpiresOn.ShouldBeGreaterThan(DateTime.UtcNow);

        // Verify in database
        var transfer = await DbContext.OwnershipTransfers
            .FirstOrDefaultAsync(ot => ot.Id == result.Id);
        transfer.ShouldNotBeNull();
        transfer.Status.ShouldBe(OwnershipTransferStatus.Pending);
    }

    [Fact]
    public async Task InitiateOwnershipTransferAsync_WhenPendingTransferExists_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireTeamOwnershipAsync(_teamId))
            .Returns(Task.CompletedTask);

        // Create existing pending transfer
        var existingTransfer = new OwnershipTransfer
        {
            Id = Guid.NewGuid(),
            TeamId = _teamId,
            InitiatedByUserId = _currentUserId,
            NewOwnerEmail = "existing@test.com",
            Status = OwnershipTransferStatus.Pending,
            ExpiresOn = DateTime.UtcNow.AddDays(7),
            TransferToken = Guid.NewGuid().ToString("N"),
            CreatedOn = DateTime.UtcNow
        };
        DbContext.OwnershipTransfers.Add(existingTransfer);
        await DbContext.SaveChangesAsync();

        var dto = new InitiateOwnershipTransferDto
        {
            NewOwnerEmail = "newowner@test.com"
        };

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _service.InitiateOwnershipTransferAsync(_teamId, dto));
    }

    [Fact]
    public async Task InitiateOwnershipTransferAsync_WhenTeamDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var nonExistentTeamId = Guid.NewGuid();
        _mockAuthorizationService.Setup(x => x.RequireTeamOwnershipAsync(nonExistentTeamId))
            .Returns(Task.CompletedTask);

        var dto = new InitiateOwnershipTransferDto
        {
            NewOwnerEmail = "newowner@test.com"
        };

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _service.InitiateOwnershipTransferAsync(nonExistentTeamId, dto));
    }

    #endregion

    #region CompleteOwnershipTransferAsync Tests

    [Fact]
    public async Task CompleteOwnershipTransferAsync_WhenValidToken_ShouldCompleteTransfer()
    {
        // Arrange
        var newOwner = await CreateTestUserAsync("newowner@test.com", "New", "Owner");
        
        // Setup current user as the new owner
        MockCurrentUserService.Setup(x => x.UserId).Returns(newOwner.Id);

        var transfer = new OwnershipTransfer
        {
            Id = Guid.NewGuid(),
            TeamId = _teamId,
            InitiatedByUserId = _currentUserId,
            NewOwnerEmail = newOwner.Email!,
            Status = OwnershipTransferStatus.Pending,
            ExpiresOn = DateTime.UtcNow.AddDays(7),
            TransferToken = Guid.NewGuid().ToString("N"),
            CreatedOn = DateTime.UtcNow,
            Team = _team!,
            InitiatedByUser = _teamOwner!
        };
        DbContext.OwnershipTransfers.Add(transfer);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.CompleteOwnershipTransferAsync(transfer.TransferToken);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(_teamId);

        // Verify ownership change
        var updatedTeam = await DbContext.Teams.FirstOrDefaultAsync(t => t.Id == _teamId);
        updatedTeam.ShouldNotBeNull();
        updatedTeam.OwnerId.ShouldBe(newOwner.Id);

        // Verify transfer completion
        var completedTransfer = await DbContext.OwnershipTransfers
            .FirstOrDefaultAsync(ot => ot.Id == transfer.Id);
        completedTransfer.ShouldNotBeNull();
        completedTransfer.Status.ShouldBe(OwnershipTransferStatus.Completed);
        completedTransfer.CompletedOn.ShouldNotBeNull();
        completedTransfer.CompletedByUserId.ShouldBe(newOwner.Id);

        // Verify role changes
        var newOwnerTeam = await DbContext.UserTeams
            .FirstOrDefaultAsync(ut => ut.UserId == newOwner.Id && ut.TeamId == _teamId);
        newOwnerTeam.ShouldNotBeNull();
        newOwnerTeam.Role.ShouldBe(TeamRole.TeamOwner);

        var oldOwnerTeam = await DbContext.UserTeams
            .FirstOrDefaultAsync(ut => ut.UserId == _teamOwner!.Id && ut.TeamId == _teamId);
        oldOwnerTeam.ShouldNotBeNull();
        oldOwnerTeam.Role.ShouldBe(TeamRole.TeamAdmin);
    }

    [Fact]
    public async Task CompleteOwnershipTransferAsync_WhenInvalidToken_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _service.CompleteOwnershipTransferAsync("invalid-token"));
    }

    [Fact]
    public async Task CompleteOwnershipTransferAsync_WhenExpiredToken_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var newOwner = await CreateTestUserAsync("newowner@test.com", "New", "Owner");
        MockCurrentUserService.Setup(x => x.UserId).Returns(newOwner.Id);

        var expiredTransfer = new OwnershipTransfer
        {
            Id = Guid.NewGuid(),
            TeamId = _teamId,
            InitiatedByUserId = _currentUserId,
            NewOwnerEmail = newOwner.Email!,
            Status = OwnershipTransferStatus.Pending,
            ExpiresOn = DateTime.UtcNow.AddDays(-1), // Expired
            TransferToken = Guid.NewGuid().ToString("N"),
            CreatedOn = DateTime.UtcNow,
            Team = _team!,
            InitiatedByUser = _teamOwner!
        };
        DbContext.OwnershipTransfers.Add(expiredTransfer);
        await DbContext.SaveChangesAsync();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _service.CompleteOwnershipTransferAsync(expiredTransfer.TransferToken));

        // Verify transfer is marked as expired
        var transfer = await DbContext.OwnershipTransfers
            .FirstOrDefaultAsync(ot => ot.Id == expiredTransfer.Id);
        transfer.ShouldNotBeNull();
        transfer.Status.ShouldBe(OwnershipTransferStatus.Expired);
    }

    #endregion

    #region CancelOwnershipTransferAsync Tests

    [Fact]
    public async Task CancelOwnershipTransferAsync_WhenValidTransfer_ShouldCancelTransfer()
    {
        // Arrange
        var transfer = new OwnershipTransfer
        {
            Id = Guid.NewGuid(),
            TeamId = _teamId,
            InitiatedByUserId = _currentUserId,
            NewOwnerEmail = "newowner@test.com",
            Status = OwnershipTransferStatus.Pending,
            ExpiresOn = DateTime.UtcNow.AddDays(7),
            TransferToken = Guid.NewGuid().ToString("N"),
            CreatedOn = DateTime.UtcNow
        };
        DbContext.OwnershipTransfers.Add(transfer);
        await DbContext.SaveChangesAsync();

        _mockAuthorizationService.Setup(x => x.RequireTeamOwnershipAsync(_teamId))
            .Returns(Task.CompletedTask);

        // Act
        await _service.CancelOwnershipTransferAsync(transfer.Id);

        // Assert
        var cancelledTransfer = await DbContext.OwnershipTransfers
            .FirstOrDefaultAsync(ot => ot.Id == transfer.Id);
        
        cancelledTransfer.ShouldNotBeNull();
        cancelledTransfer.Status.ShouldBe(OwnershipTransferStatus.Cancelled);
        cancelledTransfer.ModifiedOn.ShouldNotBeNull();
    }

    [Fact]
    public async Task CancelOwnershipTransferAsync_WhenTransferNotFound_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _service.CancelOwnershipTransferAsync(Guid.NewGuid()));
    }

    #endregion

    #region GetPendingTransfersAsync Tests

    [Fact]
    public async Task GetPendingTransfersAsync_WhenPendingTransfersExist_ShouldReturnTransfers()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(_teamId, TeamRole.TeamAdmin))
            .Returns(Task.CompletedTask);

        var transfer1 = new OwnershipTransfer
        {
            Id = Guid.NewGuid(),
            TeamId = _teamId,
            InitiatedByUserId = _currentUserId,
            NewOwnerEmail = "newowner1@test.com",
            Status = OwnershipTransferStatus.Pending,
            ExpiresOn = DateTime.UtcNow.AddDays(7),
            TransferToken = Guid.NewGuid().ToString("N"),
            CreatedOn = DateTime.UtcNow,
            Team = _team!,
            InitiatedByUser = _teamOwner!
        };

        var transfer2 = new OwnershipTransfer
        {
            Id = Guid.NewGuid(),
            TeamId = _teamId,
            InitiatedByUserId = _currentUserId,
            NewOwnerEmail = "newowner2@test.com",
            Status = OwnershipTransferStatus.Completed, // Not pending
            ExpiresOn = DateTime.UtcNow.AddDays(7),
            TransferToken = Guid.NewGuid().ToString("N"),
            CreatedOn = DateTime.UtcNow.AddDays(-1),
            Team = _team!,
            InitiatedByUser = _teamOwner!
        };

        DbContext.OwnershipTransfers.AddRange(transfer1, transfer2);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetPendingTransfersAsync(_teamId);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(1); // Only pending transfer
        result.First().Id.ShouldBe(transfer1.Id);
        result.First().Status.ShouldBe(OwnershipTransferStatus.Pending);
    }

    #endregion

    #region Utility Method Tests

    [Fact]
    public async Task IsSubdomainAvailableAsync_WhenSubdomainIsAvailable_ShouldReturnTrue()
    {
        // Act
        var result = await _service.IsSubdomainAvailableAsync("available-subdomain");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsSubdomainAvailableAsync_WhenSubdomainIsTaken_ShouldReturnFalse()
    {
        // Act
        var result = await _service.IsSubdomainAvailableAsync("test-team");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task IsSubdomainAvailableAsync_WhenNullOrEmptySubdomain_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => _service.IsSubdomainAvailableAsync(null!));

        await Should.ThrowAsync<ArgumentException>(
            () => _service.IsSubdomainAvailableAsync(""));

        await Should.ThrowAsync<ArgumentException>(
            () => _service.IsSubdomainAvailableAsync("   "));
    }

    [Fact]
    public async Task UpdateSubdomainAsync_WhenValidSubdomain_ShouldUpdateSubdomain()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireTeamOwnershipAsync(_teamId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateSubdomainAsync(_teamId, "new-subdomain");

        // Assert
        result.ShouldNotBeNull();
        result.Subdomain.ShouldBe("new-subdomain");

        // Verify in database
        var updatedTeam = await DbContext.Teams.FirstOrDefaultAsync(t => t.Id == _teamId);
        updatedTeam.ShouldNotBeNull();
        updatedTeam.Subdomain.ShouldBe("new-subdomain");
        updatedTeam.ModifiedOn.ShouldNotBeNull();
    }

    [Fact]
    public async Task UpdateSubdomainAsync_WhenSubdomainIsTaken_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireTeamOwnershipAsync(_teamId))
            .Returns(Task.CompletedTask);

        // Create another team with the desired subdomain
        var anotherOwner = await CreateTestUserAsync("another@test.com", "Another", "Owner");
        await CreateTestTeamAsync("Another Team", "taken-subdomain", anotherOwner.Id);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _service.UpdateSubdomainAsync(_teamId, "taken-subdomain"));
    }

    [Fact]
    public async Task GetTierLimitsAsync_ShouldReturnCorrectLimits()
    {
        // Act
        var freeLimits = await _service.GetTierLimitsAsync(TeamTier.Free);
        var standardLimits = await _service.GetTierLimitsAsync(TeamTier.Standard);
        var premiumLimits = await _service.GetTierLimitsAsync(TeamTier.Premium);

        // Assert
        freeLimits.Tier.ShouldBe(TeamTier.Free);
        freeLimits.MaxAthletes.ShouldBe(7);
        freeLimits.AllowCustomBranding.ShouldBeFalse();

        standardLimits.Tier.ShouldBe(TeamTier.Standard);
        standardLimits.MaxAthletes.ShouldBe(30);
        standardLimits.AllowCustomBranding.ShouldBeFalse();

        premiumLimits.Tier.ShouldBe(TeamTier.Premium);
        premiumLimits.MaxAthletes.ShouldBe(int.MaxValue);
        premiumLimits.AllowCustomBranding.ShouldBeTrue();
    }

    [Fact]
    public async Task CanAddAthleteAsync_WhenUnderLimit_ShouldReturnTrue()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(_teamId, TeamRole.TeamMember))
            .Returns(Task.CompletedTask);

        // Team is Free tier (limit 7), currently has 0 athletes

        // Act
        var result = await _service.CanAddAthleteAsync(_teamId);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task CanAddAthleteAsync_WhenAtLimit_ShouldReturnFalse()
    {
        // Arrange
        _mockAuthorizationService.Setup(x => x.RequireTeamAccessAsync(_teamId, TeamRole.TeamMember))
            .Returns(Task.CompletedTask);

        // Add 7 athletes (Free tier limit)
        for (int i = 0; i < 7; i++)
        {
            var athlete = await CreateTestUserAsync($"athlete{i}@test.com", $"Athlete{i}", "User");
            await CreateUserTeamRelationship(athlete.Id, _teamId, TeamRole.TeamMember, MemberType.Athlete);
        }

        // Act
        var result = await _service.CanAddAthleteAsync(_teamId);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region Helper Methods

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
        await DbContext.SaveChangesAsync();
        return team;
    }

    private async Task<UserTeam> CreateUserTeamRelationship(
        Guid userId, 
        Guid teamId, 
        TeamRole role = TeamRole.TeamMember,
        MemberType memberType = MemberType.Coach)
    {
        var userTeam = new UserTeam
        {
            UserId = userId,
            TeamId = teamId,
            Role = role,
            MemberType = memberType,
            IsActive = true,
            JoinedOn = DateTime.UtcNow,
            CreatedOn = DateTime.UtcNow
        };

        DbContext.UserTeams.Add(userTeam);
        await DbContext.SaveChangesAsync();
        return userTeam;
    }

    #endregion
} 