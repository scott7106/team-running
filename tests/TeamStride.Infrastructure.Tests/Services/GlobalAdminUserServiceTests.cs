using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using TeamStride.Application.Common.Services;
using TeamStride.Application.Users.Dtos;
using TeamStride.Domain.Entities;
using TeamStride.Domain.Identity;
using TeamStride.Infrastructure.Mapping;
using TeamStride.Infrastructure.Services;

namespace TeamStride.Infrastructure.Tests.Services;

public class GlobalAdminUserServiceTests : BaseSecuredTest
{
    private readonly Mock<IAuthorizationService> _mockAuthorizationService;
    private readonly Mock<ILogger<GlobalAdminUserService>> _mockLogger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IMapper _mapper;

    public GlobalAdminUserServiceTests()
    {
        _mockAuthorizationService = new Mock<IAuthorizationService>();
        _mockLogger = new Mock<ILogger<GlobalAdminUserService>>();
        _userManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        _roleManager = ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();
    }

    private GlobalAdminUserService CreateService()
    {
        return new GlobalAdminUserService(
            DbContext,
            _mockAuthorizationService.Object,
            _userManager,
            _roleManager,
            _mapper,
            _mockLogger.Object,
            MockCurrentUserService.Object);
    }

    #region GetUsersAsync Tests

    [Fact]
    public async Task GetUsersAsync_AsGlobalAdmin_ShouldReturnAllUsers()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user1 = await CreateTestUserAsync("user1@test.com", "John", "Doe");
        var user2 = await CreateTestUserAsync("user2@test.com", "Jane", "Smith");

        // Act
        var result = await service.GetUsersAsync();

        // Assert
        result.Items.Count.ShouldBeGreaterThanOrEqualTo(2);
        result.Items.ShouldContain(u => u.Email == "user1@test.com");
        result.Items.ShouldContain(u => u.Email == "user2@test.com");
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetUsersAsync_AsNonGlobalAdmin_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        SetupStandardUserContext(teamId);
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .ThrowsAsync(new UnauthorizedAccessException("Global admin privileges required"));

        var service = CreateService();

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => service.GetUsersAsync());
    }

    [Fact]
    public async Task GetUsersAsync_WithSearchQuery_ShouldFilterResults()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        await CreateTestUserAsync("john.doe@test.com", "John", "Doe");
        await CreateTestUserAsync("jane.smith@test.com", "Jane", "Smith");

        // Act
        var result = await service.GetUsersAsync(searchQuery: "john");

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items.First().Email.ShouldBe("john.doe@test.com");
    }

    [Fact]
    public async Task GetUsersAsync_WithStatusFilter_ShouldFilterResults()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var activeUser = await CreateTestUserAsync("active@test.com", "Active", "User");
        var suspendedUser = await CreateTestUserAsync("suspended@test.com", "Suspended", "User");
        
        // Update user status
        suspendedUser.Status = UserStatus.Suspended;
        await _userManager.UpdateAsync(suspendedUser);

        // Act
        var result = await service.GetUsersAsync(status: UserStatus.Active);

        // Assert
        result.Items.ShouldContain(u => u.Email == "active@test.com");
        result.Items.ShouldNotContain(u => u.Email == "suspended@test.com");
    }

    [Fact]
    public async Task GetUsersAsync_WithIsActiveFilter_ShouldFilterResults()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var activeUser = await CreateTestUserAsync("active@test.com", "Active", "User");
        var inactiveUser = await CreateTestUserAsync("inactive@test.com", "Inactive", "User");
        
        // Update user active status
        inactiveUser.IsActive = false;
        await _userManager.UpdateAsync(inactiveUser);

        // Act
        var result = await service.GetUsersAsync(isActive: true);

        // Assert
        result.Items.ShouldContain(u => u.Email == "active@test.com");
        result.Items.ShouldNotContain(u => u.Email == "inactive@test.com");
    }

    [Fact]
    public async Task GetUsersAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Create multiple users
        for (int i = 1; i <= 5; i++)
        {
            await CreateTestUserAsync($"user{i:D2}@test.com", $"User{i:D2}", "Test");
        }

        // Act
        var result = await service.GetUsersAsync(pageNumber: 2, pageSize: 2);

        // Assert
        result.PageNumber.ShouldBe(2);
        result.Items.Count.ShouldBe(2);
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(5);
    }

    #endregion

    #region GetDeletedUsersAsync Tests

    [Fact]
    public async Task GetDeletedUsersAsync_AsGlobalAdmin_ShouldReturnDeletedUsers()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user = await CreateTestUserAsync("deleted@test.com", "Deleted", "User");
        
        // Soft delete the user
        user.IsDeleted = true;
        user.DeletedOn = DateTime.UtcNow;
        user.DeletedBy = MockCurrentUserService.Object.UserId;
        await DbContext.SaveChangesAsync();

        // Act
        var result = await service.GetDeletedUsersAsync();

        // Assert
        result.Items.ShouldContain(u => u.Email == "deleted@test.com");
        result.Items.First(u => u.Email == "deleted@test.com").DeletedOn.ShouldNotBe(DateTime.MinValue);
    }

    [Fact]
    public async Task GetDeletedUsersAsync_WithSearchQuery_ShouldFilterResults()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user1 = await CreateTestUserAsync("deleted1@test.com", "Deleted", "One");
        var user2 = await CreateTestUserAsync("deleted2@test.com", "Deleted", "Two");
        
        // Soft delete both users
        user1.IsDeleted = true;
        user1.DeletedOn = DateTime.UtcNow;
        user2.IsDeleted = true;
        user2.DeletedOn = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();

        // Act
        var result = await service.GetDeletedUsersAsync(searchQuery: "One");

        // Assert
        result.Items.Count.ShouldBe(1);
        result.Items.First().Email.ShouldBe("deleted1@test.com");
    }

    #endregion

    #region GetUserByIdAsync Tests

    [Fact]
    public async Task GetUserByIdAsync_AsGlobalAdmin_ShouldReturnUser()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user = await CreateTestUserAsync("test@test.com", "Test", "User");

        // Act
        var result = await service.GetUserByIdAsync(user.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Email.ShouldBe("test@test.com");
        result.FirstName.ShouldBe("Test");
        result.LastName.ShouldBe("User");
    }

    [Fact]
    public async Task GetUserByIdAsync_WithNonExistentUser_ShouldThrowInvalidOperationException()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();
        var nonExistentUserId = Guid.NewGuid();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => service.GetUserByIdAsync(nonExistentUserId));
    }

    #endregion

    #region CreateUserAsync Tests

    [Fact]
    public async Task CreateUserAsync_AsGlobalAdmin_ShouldCreateUser()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Ensure test role exists
        await CreateTestRoleAsync("TestRole");

        var dto = new GlobalAdminCreateUserDto
        {
            Email = "newuser@test.com",
            Password = "Password123!",
            FirstName = "New",
            LastName = "User",
            PhoneNumber = "+1234567890",
            ApplicationRoles = new List<string> { "TestRole" },
            RequirePasswordChange = true
        };

        // Act
        var result = await service.CreateUserAsync(dto);

        // Assert
        result.ShouldNotBeNull();
        result.Email.ShouldBe("newuser@test.com");
        result.FirstName.ShouldBe("New");
        result.LastName.ShouldBe("User");
        result.ApplicationRoles.ShouldContain("TestRole");

        // Verify user was created in database
        var createdUser = await _userManager.FindByEmailAsync("newuser@test.com");
        createdUser.ShouldNotBeNull();
        createdUser.EmailConfirmed.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateUserAsync_WithExistingEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        await CreateTestUserAsync("existing@test.com", "Existing", "User");

        var dto = new GlobalAdminCreateUserDto
        {
            Email = "existing@test.com",
            Password = "Password123!",
            FirstName = "New",
            LastName = "User"
        };

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => service.CreateUserAsync(dto));
    }

    [Fact]
    public async Task CreateUserAsync_WithNonExistentRole_ShouldThrowInvalidOperationException()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var dto = new GlobalAdminCreateUserDto
        {
            Email = "newuser@test.com",
            Password = "Password123!",
            FirstName = "New",
            LastName = "User",
            ApplicationRoles = new List<string> { "NonExistentRole" }
        };

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => service.CreateUserAsync(dto));
    }

    #endregion

    #region UpdateUserAsync Tests

    [Fact]
    public async Task UpdateUserAsync_AsGlobalAdmin_ShouldUpdateUser()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user = await CreateTestUserAsync("original@test.com", "Original", "User");

        var dto = new GlobalAdminUpdateUserDto
        {
            Email = "updated@test.com",
            FirstName = "Updated",
            LastName = "User",
            PhoneNumber = "+1234567890",
            Status = UserStatus.Active,
            IsActive = true,
            EmailConfirmed = true,
            PhoneNumberConfirmed = false
        };

        // Act
        var result = await service.UpdateUserAsync(user.Id, dto);

        // Assert
        result.ShouldNotBeNull();
        result.Email.ShouldBe("updated@test.com");
        result.FirstName.ShouldBe("Updated");
        result.LastName.ShouldBe("User");
        result.PhoneNumber.ShouldBe("+1234567890");
    }

    [Fact]
    public async Task UpdateUserAsync_WithExistingEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user1 = await CreateTestUserAsync("user1@test.com", "User", "One");
        var user2 = await CreateTestUserAsync("user2@test.com", "User", "Two");

        var dto = new GlobalAdminUpdateUserDto
        {
            Email = "user1@test.com", // Try to use existing email
            FirstName = "Updated",
            LastName = "User",
            Status = UserStatus.Active,
            IsActive = true,
            EmailConfirmed = true
        };

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => service.UpdateUserAsync(user2.Id, dto));
    }

    #endregion

    #region DeleteUserAsync Tests

    [Fact]
    public async Task DeleteUserAsync_AsGlobalAdmin_ShouldSoftDeleteUser()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user = await CreateTestUserAsync("todelete@test.com", "To", "Delete");

        // Act
        await service.DeleteUserAsync(user.Id);

        // Assert
        await DbContext.Entry(user).ReloadAsync();
        user.IsDeleted.ShouldBeTrue();
        user.DeletedOn.ShouldNotBeNull();
        user.DeletedBy.ShouldBe(MockCurrentUserService.Object.UserId);
        user.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteUserAsync_WithAlreadyDeletedUser_ShouldThrowInvalidOperationException()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user = await CreateTestUserAsync("deleted@test.com", "Already", "Deleted");
        user.IsDeleted = true;
        await DbContext.SaveChangesAsync();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => service.DeleteUserAsync(user.Id));
    }

    #endregion

    #region PermanentlyDeleteUserAsync Tests

    [Fact]
    public async Task PermanentlyDeleteUserAsync_AsGlobalAdmin_ShouldPermanentlyDeleteUser()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user = await CreateTestUserAsync("permanent@test.com", "Permanent", "Delete");
        var userId = user.Id;

        // Act
        await service.PermanentlyDeleteUserAsync(userId);

        // Assert
        var deletedUser = await DbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        deletedUser.ShouldBeNull();
    }

    #endregion

    #region RecoverUserAsync Tests

    [Fact]
    public async Task RecoverUserAsync_AsGlobalAdmin_ShouldRecoverDeletedUser()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user = await CreateTestUserAsync("recover@test.com", "To", "Recover");
        
        // Soft delete the user first
        user.IsDeleted = true;
        user.DeletedOn = DateTime.UtcNow;
        user.DeletedBy = Guid.NewGuid();
        user.IsActive = false;
        await DbContext.SaveChangesAsync();

        // Act
        var result = await service.RecoverUserAsync(user.Id);

        // Assert
        result.ShouldNotBeNull();
        result.IsDeleted.ShouldBeFalse();
        result.IsActive.ShouldBeTrue();
        
        await DbContext.Entry(user).ReloadAsync();
        user.IsDeleted.ShouldBeFalse();
        user.DeletedOn.ShouldBeNull();
        user.DeletedBy.ShouldBeNull();
        user.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task RecoverUserAsync_WithNonDeletedUser_ShouldThrowInvalidOperationException()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user = await CreateTestUserAsync("active@test.com", "Active", "User");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => service.RecoverUserAsync(user.Id));
    }

    #endregion

    #region ResetLockoutAsync Tests

    [Fact]
    public async Task ResetLockoutAsync_AsGlobalAdmin_ShouldResetLockout()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user = await CreateTestUserAsync("locked@test.com", "Locked", "User");
        
        // Simulate lockout
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(30));
        await _userManager.AccessFailedAsync(user);

        // Act
        var result = await service.ResetLockoutAsync(user.Id);

        // Assert
        result.ShouldNotBeNull();
        result.LockoutEnd.ShouldBeNull();
        result.AccessFailedCount.ShouldBe(0);
    }

    #endregion

    #region ResetPasswordAsync Tests

    [Fact]
    public async Task ResetPasswordAsync_WithNewPassword_ShouldResetToSpecifiedPassword()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user = await CreateTestUserAsync("reset@test.com", "Reset", "User");

        var dto = new GlobalAdminResetPasswordDto
        {
            NewPassword = "NewPassword123!",
            RequirePasswordChange = true,
            SendPasswordByEmail = false,
            SendPasswordBySms = false
        };

        // Act
        var result = await service.ResetPasswordAsync(user.Id, dto);

        // Assert
        result.ShouldNotBeNull();
        result.UserId.ShouldBe(user.Id);
        result.Email.ShouldBe("reset@test.com");
        result.TemporaryPassword.ShouldBeNull(); // Should be null when password is specified
        result.RequirePasswordChange.ShouldBeTrue();

        // Verify password was changed
        var passwordCheck = await _userManager.CheckPasswordAsync(user, "NewPassword123!");
        passwordCheck.ShouldBeTrue();
    }

    [Fact]
    public async Task ResetPasswordAsync_WithoutNewPassword_ShouldGenerateTemporaryPassword()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user = await CreateTestUserAsync("reset@test.com", "Reset", "User");

        var dto = new GlobalAdminResetPasswordDto
        {
            NewPassword = null, // Generate temporary password
            RequirePasswordChange = true,
            SendPasswordByEmail = false,
            SendPasswordBySms = false
        };

        // Act
        var result = await service.ResetPasswordAsync(user.Id, dto);

        // Assert
        result.ShouldNotBeNull();
        result.TemporaryPassword.ShouldNotBeNullOrEmpty();
        result.RequirePasswordChange.ShouldBeTrue();

        // Verify generated password works
        var passwordCheck = await _userManager.CheckPasswordAsync(user, result.TemporaryPassword!);
        passwordCheck.ShouldBeTrue();
    }

    #endregion

    #region IsEmailAvailableAsync Tests

    [Fact]
    public async Task IsEmailAvailableAsync_WithAvailableEmail_ShouldReturnTrue()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.IsEmailAvailableAsync("available@test.com");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsEmailAvailableAsync_WithTakenEmail_ShouldReturnFalse()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        await CreateTestUserAsync("taken@test.com", "Taken", "User");

        // Act
        var result = await service.IsEmailAvailableAsync("taken@test.com");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task IsEmailAvailableAsync_WithExcludedUser_ShouldReturnTrue()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user = await CreateTestUserAsync("user@test.com", "Test", "User");

        // Act - Check if email is available excluding the user who already has it
        var result = await service.IsEmailAvailableAsync("user@test.com", user.Id);

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region Role Management Tests

    [Fact]
    public async Task GetApplicationRolesAsync_AsGlobalAdmin_ShouldReturnAllRoles()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        await CreateTestRoleAsync("TestRole1");
        await CreateTestRoleAsync("TestRole2");

        // Act
        var result = await service.GetApplicationRolesAsync();

        // Assert
        result.ShouldNotBeEmpty();
        result.ShouldContain(r => r.Name == "TestRole1");
        result.ShouldContain(r => r.Name == "TestRole2");
    }

    [Fact]
    public async Task GetUserApplicationRolesAsync_AsGlobalAdmin_ShouldReturnUserRoles()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user = await CreateTestUserAsync("user@test.com", "Test", "User");
        await CreateTestRoleAsync("TestRole");
        await _userManager.AddToRoleAsync(user, "TestRole");

        // Act
        var result = await service.GetUserApplicationRolesAsync(user.Id);

        // Assert
        result.ShouldContain("TestRole");
    }

    [Fact]
    public async Task AddUserToRoleAsync_AsGlobalAdmin_ShouldAddRole()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user = await CreateTestUserAsync("user@test.com", "Test", "User");
        await CreateTestRoleAsync("TestRole");

        // Act
        var result = await service.AddUserToRoleAsync(user.Id, "TestRole");

        // Assert
        result.ShouldNotBeNull();
        result.ApplicationRoles.ShouldContain("TestRole");

        // Verify in database
        var isInRole = await _userManager.IsInRoleAsync(user, "TestRole");
        isInRole.ShouldBeTrue();
    }

    [Fact]
    public async Task AddUserToRoleAsync_WithNonExistentRole_ShouldThrowInvalidOperationException()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user = await CreateTestUserAsync("user@test.com", "Test", "User");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => service.AddUserToRoleAsync(user.Id, "NonExistentRole"));
    }

    [Fact]
    public async Task RemoveUserFromRoleAsync_AsGlobalAdmin_ShouldRemoveRole()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user = await CreateTestUserAsync("user@test.com", "Test", "User");
        await CreateTestRoleAsync("TestRole");
        await _userManager.AddToRoleAsync(user, "TestRole");

        // Act
        var result = await service.RemoveUserFromRoleAsync(user.Id, "TestRole");

        // Assert
        result.ShouldNotBeNull();
        result.ApplicationRoles.ShouldNotContain("TestRole");

        // Verify in database
        var isInRole = await _userManager.IsInRoleAsync(user, "TestRole");
        isInRole.ShouldBeFalse();
    }

    [Fact]
    public async Task SetUserRolesAsync_AsGlobalAdmin_ShouldSetRoles()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var user = await CreateTestUserAsync("user@test.com", "Test", "User");
        await CreateTestRoleAsync("Role1");
        await CreateTestRoleAsync("Role2");
        await CreateTestRoleAsync("Role3");
        
        // Add initial roles
        await _userManager.AddToRoleAsync(user, "Role1");
        await _userManager.AddToRoleAsync(user, "Role2");

        var dto = new SetUserRolesDto
        {
            RoleNames = new List<string> { "Role2", "Role3" }
        };

        // Act
        var result = await service.SetUserRolesAsync(user.Id, dto);

        // Assert
        result.ShouldNotBeNull();
        result.ApplicationRoles.ShouldContain("Role2");
        result.ApplicationRoles.ShouldContain("Role3");
        result.ApplicationRoles.ShouldNotContain("Role1");

        // Verify in database
        var userRoles = await _userManager.GetRolesAsync(user);
        userRoles.ShouldContain("Role2");
        userRoles.ShouldContain("Role3");
        userRoles.ShouldNotContain("Role1");
    }

    [Fact]
    public async Task RoleExistsAsync_WithExistingRole_ShouldReturnTrue()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        await CreateTestRoleAsync("ExistingRole");

        // Act
        var result = await service.RoleExistsAsync("ExistingRole");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task RoleExistsAsync_WithNonExistentRole_ShouldReturnFalse()
    {
        // Arrange
        SetupGlobalAdminContext();
        
        _mockAuthorizationService.Setup(x => x.RequireGlobalAdminAsync())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.RoleExistsAsync("NonExistentRole");

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region Helper Methods

    private async Task<ApplicationUser> CreateTestUserAsync(
        string email, 
        string firstName, 
        string lastName, 
        Guid? id = null)
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
            IsActive = true,
            Status = UserStatus.Active,
            SecurityStamp = Guid.NewGuid().ToString("D"),
            CreatedOn = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, "TestPassword123!");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return user;
    }

    private async Task<Team> CreateTestTeamAsync(
        string name, 
        string subdomain, 
        Guid ownerId, 
        Guid? id = null,
        TeamStatus status = TeamStatus.Active,
        TeamTier tier = TeamTier.Free)
    {
        var teamId = id ?? Guid.NewGuid();
        var team = new Team
        {
            Id = teamId,
            Name = name,
            Subdomain = subdomain,
            OwnerId = ownerId,
            Status = status,
            Tier = tier,
            PrimaryColor = "#000000",
            SecondaryColor = "#FFFFFF",
            CreatedOn = DateTime.UtcNow
        };

        DbContext.Teams.Add(team);
        await DbContext.SaveChangesAsync();
        
        // Always create the owner relationship for the team
        await CreateUserTeamRelationshipAsync(ownerId, teamId, TeamRole.TeamOwner, MemberType.Coach);
        
        return team;
    }

    private async Task<UserTeam> CreateUserTeamRelationshipAsync(
        Guid userId,
        Guid teamId,
        TeamRole role = TeamRole.TeamMember,
        MemberType memberType = MemberType.Coach)
    {
        var userTeam = new UserTeam
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TeamId = teamId,
            Role = role,
            MemberType = memberType,
            IsActive = true,
            IsDefault = role == TeamRole.TeamOwner,
            JoinedOn = DateTime.UtcNow,
            CreatedOn = DateTime.UtcNow
        };

        DbContext.UserTeams.Add(userTeam);
        await DbContext.SaveChangesAsync();
        return userTeam;
    }

    private async Task<ApplicationRole> CreateTestRoleAsync(string roleName)
    {
        var role = new ApplicationRole
        {
            Name = roleName,
            NormalizedName = roleName.ToUpper(),
            Description = $"Test role {roleName}"
        };

        await _roleManager.CreateAsync(role);
        return role;
    }

    #endregion
} 