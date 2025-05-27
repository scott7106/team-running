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

    // TODO: Add tests for other ApplicationUser functionality as needed
} 