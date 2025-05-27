using System;
using Shouldly;
using TeamStride.Domain.Identity;
using Xunit;

namespace TeamStride.Domain.Tests.Entities;

public class ApplicationUserTests
{
    private ApplicationUser CreateUser()
    {
        return new ApplicationUser
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            UserName = "test@example.com"
        };
    }

    [Fact]
    public void SetGlobalAdmin_WhenNotGlobalAdmin_SetsIsGlobalAdminToTrue()
    {
        // Arrange
        var user = CreateUser();
        var beforeModifiedOn = user.ModifiedOn;

        // Act
        user.SetGlobalAdmin(true);

        // Assert
        user.IsGlobalAdmin.ShouldBeTrue();
        user.ModifiedOn.ShouldNotBe(beforeModifiedOn);
    }

    [Fact]
    public void SetGlobalAdmin_WhenAlreadyGlobalAdmin_ThrowsInvalidOperationException()
    {
        // Arrange
        var user = CreateUser();
        user.SetGlobalAdmin(true);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => user.SetGlobalAdmin(true))
            .Message.ShouldBe("User is already a global admin.");
    }

    [Fact]
    public void RevokeGlobalAdmin_WhenGlobalAdmin_SetsIsGlobalAdminToFalse()
    {
        // Arrange
        var user = CreateUser();
        user.SetGlobalAdmin(true);
        var beforeModifiedOn = user.ModifiedOn;

        // Act
        user.SetGlobalAdmin(false);

        // Assert
        user.IsGlobalAdmin.ShouldBeFalse();
        user.ModifiedOn.ShouldNotBe(beforeModifiedOn);
    }

    [Fact]
    public void RevokeGlobalAdmin_WhenNotGlobalAdmin_ThrowsInvalidOperationException()
    {
        // Arrange
        var user = CreateUser();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => user.SetGlobalAdmin(false))
            .Message.ShouldBe("User is not a global admin.");
    }
} 