using Xunit;

namespace TeamStride.Domain.Tests;

/// <summary>
/// Base class for all domain unit tests.
/// Provides common setup and utilities for testing domain entities and business logic.
/// </summary>
public abstract class BaseTest : IDisposable
{
    protected BaseTest()
    {
        // Common setup for all domain tests
    }

    public virtual void Dispose()
    {
        // Common cleanup for all domain tests
    }
} 