namespace TeamStride.Domain.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserEmail { get; }
    bool IsAuthenticated { get; }
    bool IsGlobalAdmin { get; }
} 