using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using TeamStride.Domain.Interfaces;

namespace TeamStride.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return value != null ? Guid.Parse(value) : null;
        }
    }

    public string? UserEmail => _httpContextAccessor.HttpContext?.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public bool IsGlobalAdmin => _httpContextAccessor.HttpContext?.User?.Claims.Any(c => c.Type == "IsGlobalAdmin" && c.Value == "true") ?? false;
} 