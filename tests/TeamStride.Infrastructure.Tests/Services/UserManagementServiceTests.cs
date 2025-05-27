using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using TeamStride.Application.Common.Models;
using TeamStride.Application.Users.Services;
using TeamStride.Domain.Identity;
using TeamStride.Infrastructure.Services;

namespace TeamStride.Infrastructure.Tests.Services;

public class UserManagementServiceTests : BaseIntegrationTest
{
    private readonly UserManagementService _userManagementService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly List<ApplicationUser> _testUsers;

    public UserManagementServiceTests()
    {
        // Get UserManager from the service provider
        _userManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        // Create the service
        _userManagementService = new UserManagementService(_userManager);
        
        // Setup test data
        _testUsers = CreateTestUsersAsync().Result;
    }

    private async Task<List<ApplicationUser>> CreateTestUsersAsync()
    {
        var user1 = new ApplicationUser
        {
            Email = "john.doe@example.com",
            UserName = "john.doe@example.com",
            FirstName = "John",
            LastName = "Doe",
            IsActive = true,
            IsDeleted = false,
            LockoutEnd = null,
            CreatedOn = DateTime.UtcNow
        };

        var user2 = new ApplicationUser
        {
            Email = "jane.smith@example.com",
            UserName = "jane.smith@example.com",
            FirstName = "Jane",
            LastName = "Smith",
            IsActive = true,
            IsDeleted = false,
            LockoutEnd = DateTime.UtcNow.AddDays(1),
            CreatedOn = DateTime.UtcNow
        };

        var user3 = new ApplicationUser
        {
            Email = "deleted.user@example.com",
            UserName = "deleted.user@example.com",
            FirstName = "Deleted",
            LastName = "User",
            IsActive = false,
            IsDeleted = true,
            DeletedOn = DateTime.UtcNow.AddDays(-1),
            LockoutEnd = null,
            CreatedOn = DateTime.UtcNow.AddDays(-2)
        };

        // Create users using UserManager to ensure proper initialization
        await _userManager.CreateAsync(user1);
        await _userManager.CreateAsync(user2);
        await _userManager.CreateAsync(user3);

        // Set global admin status for user2 after it's been created
        user2.SetGlobalAdmin(true);
        await _userManager.UpdateAsync(user2);

        return new List<ApplicationUser> { user1, user2, user3 };
    }

    #region GetUsersAsync Tests

    [Fact]
    public async Task GetUsersAsync_WithValidParameters_ReturnsCorrectPaginatedList()
    {
        // Arrange
        var pageNumber = 1;
        var pageSize = 10;

        // Act
        var result = await _userManagementService.GetUsersAsync(pageNumber, pageSize);

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(3);
        result.PageNumber.ShouldBe(pageNumber);
        result.TotalCount.ShouldBe(3);
        
        // Verify ordering by LastName, then FirstName
        var itemsList = result.Items.ToList();
        itemsList[0].DisplayName.ShouldBe("Doe, John");
        itemsList[1].DisplayName.ShouldBe("Smith, Jane");
        itemsList[2].DisplayName.ShouldBe("User, Deleted");
    }

    [Fact]
    public async Task GetUsersAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var pageNumber = 2;
        var pageSize = 1;

        // Act
        var result = await _userManagementService.GetUsersAsync(pageNumber, pageSize);

        // Assert
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(1);
        result.PageNumber.ShouldBe(pageNumber);
        result.HasNextPage.ShouldBeTrue();
        result.HasPreviousPage.ShouldBeTrue();
    }

    [Fact]
    public async Task GetUsersAsync_IncludesDeletedUsers_ReturnsAllUsers()
    {
        // Arrange
        var pageNumber = 1;
        var pageSize = 10;

        // Act
        var result = await _userManagementService.GetUsersAsync(pageNumber, pageSize);

        // Assert
        result.ShouldNotBeNull();
        result.Items.Any(u => u.IsDeleted).ShouldBeTrue();
        result.Items.Count(u => u.IsDeleted).ShouldBe(1);
    }

    #endregion

    #region SetGlobalAdminStatusAsync Tests

    [Fact]
    public async Task SetGlobalAdminStatusAsync_WithValidUser_SetsGlobalAdminStatus()
    {
        // Arrange
        var user = _testUsers.First(u => !u.IsDeleted && !u.IsGlobalAdmin);
        var userId = user.Id;
        var isGlobalAdmin = true;

        // Act
        await _userManagementService.SetGlobalAdminStatusAsync(userId, isGlobalAdmin);

        // Assert
        var updatedUser = await DbContext.Users.FindAsync(userId);
        updatedUser.ShouldNotBeNull();
        updatedUser.IsGlobalAdmin.ShouldBe(isGlobalAdmin);
        updatedUser.ModifiedOn.ShouldNotBeNull();
    }

    [Fact]
    public async Task SetGlobalAdminStatusAsync_WithDeletedUser_ThrowsInvalidOperationException()
    {
        // Arrange
        var deletedUser = _testUsers.First(u => u.IsDeleted);
        var userId = deletedUser.Id;
        var isGlobalAdmin = true;

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _userManagementService.SetGlobalAdminStatusAsync(userId, isGlobalAdmin));
        
        exception.Message.ShouldBe("User is deleted and cannot be updated.");
    }

    [Fact]
    public async Task SetGlobalAdminStatusAsync_WithNonExistentUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        var isGlobalAdmin = true;

        // Act & Assert
        var exception = await Should.ThrowAsync<KeyNotFoundException>(
            () => _userManagementService.SetGlobalAdminStatusAsync(nonExistentUserId, isGlobalAdmin));
        
        exception.Message.ShouldBe($"User with ID {nonExistentUserId} not found.");
    }

    #endregion

    #region RemoveLockoutAsync Tests

    [Fact]
    public async Task RemoveLockoutAsync_WithValidUser_RemovesLockout()
    {
        // Arrange
        var lockedUser = _testUsers.First(u => u.LockoutEnd.HasValue);
        var userId = lockedUser.Id;

        // Act
        await _userManagementService.RemoveLockoutAsync(userId);

        // Assert
        var updatedUser = await DbContext.Users.FindAsync(userId);
        updatedUser.ShouldNotBeNull();
        updatedUser.LockoutEnd.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveLockoutAsync_WithNonExistentUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act & Assert
        var exception = await Should.ThrowAsync<KeyNotFoundException>(
            () => _userManagementService.RemoveLockoutAsync(nonExistentUserId));
        
        exception.Message.ShouldBe($"User with ID {nonExistentUserId} not found.");
    }

    #endregion

    #region DeleteUserAsync Tests

    [Fact]
    public async Task DeleteUserAsync_WithValidUser_SoftDeletesUser()
    {
        // Arrange
        var activeUser = _testUsers.First(u => !u.IsDeleted);
        var userId = activeUser.Id;

        // Act
        await _userManagementService.DeleteUserAsync(userId);

        // Assert
        var updatedUser = await DbContext.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == userId);
        updatedUser.IsDeleted.ShouldBeTrue();
        updatedUser.DeletedOn.ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteUserAsync_WithAlreadyDeletedUser_ThrowsInvalidOperationException()
    {
        // Arrange
        var deletedUser = _testUsers.First(u => u.IsDeleted);
        var userId = deletedUser.Id;

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _userManagementService.DeleteUserAsync(userId));
        
        exception.Message.ShouldBe("User is already deleted.");
    }

    [Fact]
    public async Task DeleteUserAsync_WithNonExistentUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act & Assert
        var exception = await Should.ThrowAsync<KeyNotFoundException>(
            () => _userManagementService.DeleteUserAsync(nonExistentUserId));
        
        exception.Message.ShouldBe($"User with ID {nonExistentUserId} not found.");
    }

    #endregion

    #region RestoreUserAsync Tests

    [Fact]
    public async Task RestoreUserAsync_WithDeletedUser_RestoresUser()
    {
        // Arrange
        var deletedUser = _testUsers.First(u => u.IsDeleted);
        var userId = deletedUser.Id;

        // Act
        await _userManagementService.RestoreUserAsync(userId);

        // Assert
        var updatedUser = await DbContext.Users.FindAsync(userId);
        updatedUser.ShouldNotBeNull();
        updatedUser.IsDeleted.ShouldBeFalse();
        updatedUser.DeletedOn.ShouldBeNull();
        updatedUser.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public async Task RestoreUserAsync_WithActiveUser_ThrowsInvalidOperationException()
    {
        // Arrange
        var activeUser = _testUsers.First(u => !u.IsDeleted);
        var userId = activeUser.Id;

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _userManagementService.RestoreUserAsync(userId));
        
        exception.Message.ShouldBe("User is not deleted and cannot be restored.");
    }

    [Fact]
    public async Task RestoreUserAsync_WithNonExistentUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act & Assert
        var exception = await Should.ThrowAsync<KeyNotFoundException>(
            () => _userManagementService.RestoreUserAsync(nonExistentUserId));
        
        exception.Message.ShouldBe($"User with ID {nonExistentUserId} not found.");
    }

    #endregion

    #region SetUserActiveStatusAsync Tests

    [Fact]
    public async Task SetUserActiveStatusAsync_WithValidUser_SetsActiveStatus()
    {
        // Arrange
        var user = _testUsers.First(u => !u.IsDeleted);
        var userId = user.Id;
        var isActive = false;

        // Act
        await _userManagementService.SetUserActiveStatusAsync(userId, isActive);

        // Assert
        var updatedUser = await DbContext.Users.FindAsync(userId);
        updatedUser.ShouldNotBeNull();
        updatedUser.IsActive.ShouldBe(isActive);
        updatedUser.ModifiedOn.ShouldNotBeNull();
    }

    [Fact]
    public async Task SetUserActiveStatusAsync_WithDeletedUser_ThrowsInvalidOperationException()
    {
        // Arrange
        var deletedUser = _testUsers.First(u => u.IsDeleted);
        var userId = deletedUser.Id;
        var isActive = true;

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _userManagementService.SetUserActiveStatusAsync(userId, isActive));
        
        exception.Message.ShouldBe("User is deleted and cannot be updated.");
    }

    [Fact]
    public async Task SetUserActiveStatusAsync_WithNonExistentUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        var isActive = true;

        // Act & Assert
        var exception = await Should.ThrowAsync<KeyNotFoundException>(
            () => _userManagementService.SetUserActiveStatusAsync(nonExistentUserId, isActive));
        
        exception.Message.ShouldBe($"User with ID {nonExistentUserId} not found.");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task UserManagementService_WithCompleteWorkflow_WorksCorrectly()
    {
        // Arrange
        var user = _testUsers.First(u => !u.IsDeleted && !u.IsGlobalAdmin);
        var userId = user.Id;

        // Act & Assert - Complete workflow
        // 1. Set as global admin
        await _userManagementService.SetGlobalAdminStatusAsync(userId, true);
        var updatedUser = await DbContext.Users.FindAsync(userId);
        updatedUser!.IsGlobalAdmin.ShouldBeTrue();

        // 2. Remove lockout (set a lockout first)
        await _userManager.SetLockoutEndDateAsync(updatedUser, DateTime.UtcNow.AddDays(1));
        await _userManagementService.RemoveLockoutAsync(userId);
        updatedUser = await DbContext.Users.FindAsync(userId);
        updatedUser!.LockoutEnd.ShouldBeNull();

        // 3. Set inactive
        await _userManagementService.SetUserActiveStatusAsync(userId, false);
        updatedUser = await DbContext.Users.FindAsync(userId);
        updatedUser!.IsActive.ShouldBeFalse();

        // 4. Delete user
        await _userManagementService.DeleteUserAsync(userId);
        updatedUser = await DbContext.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == userId);
        updatedUser.IsDeleted.ShouldBeTrue();

        // 5. Restore user
        await _userManagementService.RestoreUserAsync(userId);
        updatedUser = await DbContext.Users.FindAsync(userId);
        updatedUser.ShouldNotBeNull();
        updatedUser.IsDeleted.ShouldBeFalse();
    }

    #endregion

    #region CurrentUser.IsGlobalAdmin Mock Tests

    [Fact]
    public async Task UserManagementService_RequiresGlobalAdminAccess_MockedAsTrue()
    {
        // Arrange - Mock CurrentUser.IsGlobalAdmin as True (as requested)
        MockCurrentUserService.Setup(x => x.IsGlobalAdmin).Returns(true);
        
        var user = _testUsers.First(u => !u.IsDeleted);
        var userId = user.Id;

        // Act - All operations should work since CurrentUser.IsGlobalAdmin is mocked as true
        await _userManagementService.SetGlobalAdminStatusAsync(userId, true);
        await _userManagementService.SetUserActiveStatusAsync(userId, false);
        await _userManagementService.DeleteUserAsync(userId);
        await _userManagementService.RestoreUserAsync(userId);

        // Assert - Operations completed successfully
        var finalUser = await DbContext.Users.FindAsync(userId);
        finalUser.ShouldNotBeNull();
        finalUser.IsGlobalAdmin.ShouldBeTrue();
        finalUser.IsDeleted.ShouldBeFalse(); // Restored
    }

    #endregion
} 