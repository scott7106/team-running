# TeamStride Authorization Service

The `IAuthorizationService` provides centralized authorization logic for the simplified 3-tier authorization model across all application services.

## Overview

This service consolidates authorization patterns into reusable methods, ensuring consistent security enforcement throughout the application while reducing code duplication.

## Key Benefits

1. **Consistency**: Standardized authorization logic across all services
2. **Maintainability**: Single source of truth for authorization rules
3. **Security**: Centralized validation reduces risk of missing authorization checks
4. **Testability**: Authorization logic can be unit tested in isolation
5. **Clean Code**: Business logic services focus on their core functionality

## Authorization Patterns

The service implements two main authorization patterns from the requirements:

- **RequireGlobalAdmin**: For platform-wide operations
- **RequireTeamAccess**: For team-specific operations with role hierarchy

## Interface Methods

### Global Admin Operations

```csharp
// Requires global admin privileges
await _authorizationService.RequireGlobalAdminAsync();
```

### Team Access Operations

```csharp
// Basic team access (any team member)
await _authorizationService.RequireTeamAccessAsync(teamId);

// Specific role requirements
await _authorizationService.RequireTeamAccessAsync(teamId, TeamRole.TeamAdmin);
await _authorizationService.RequireTeamOwnershipAsync(teamId);
await _authorizationService.RequireTeamAdminAsync(teamId);
```

### Permission Checking

```csharp
// Check permissions without throwing exceptions
var canAccess = await _authorizationService.CanAccessTeamAsync(teamId);
var isOwner = await _authorizationService.IsTeamOwnerAsync(teamId);
var isAdmin = await _authorizationService.IsTeamAdminAsync(teamId);
```

### Resource-Based Authorization

```csharp
// For entities that implement ITeamResource
await _authorizationService.RequireResourceAccessAsync(athlete, TeamRole.TeamAdmin);
var canAccess = await _authorizationService.CanAccessResourceAsync(practice);
```

## Usage Examples

### In Application Services

```csharp
public class AthleteService
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IAthleteRepository _athleteRepository;

    public AthleteService(
        IAuthorizationService authorizationService,
        IAthleteRepository athleteRepository)
    {
        _authorizationService = authorizationService;
        _athleteRepository = athleteRepository;
    }

    public async Task<AthleteDto> GetAthleteAsync(Guid athleteId)
    {
        var athlete = await _athleteRepository.GetByIdAsync(athleteId);
        if (athlete == null)
            throw new NotFoundException("Athlete not found");

        // Ensure user can access this athlete's team
        await _authorizationService.RequireResourceAccessAsync(athlete);

        return MapToDto(athlete);
    }

    public async Task UpdateAthleteAsync(Guid athleteId, UpdateAthleteDto dto)
    {
        var athlete = await _athleteRepository.GetByIdAsync(athleteId);
        if (athlete == null)
            throw new NotFoundException("Athlete not found");

        // Require admin access to update athletes
        await _authorizationService.RequireResourceAccessAsync(athlete, TeamRole.TeamAdmin);

        // Update logic here...
    }

    public async Task DeleteAthleteAsync(Guid athleteId)
    {
        var athlete = await _athleteRepository.GetByIdAsync(athleteId);
        if (athlete == null)
            throw new NotFoundException("Athlete not found");

        // Only team owners can delete athletes
        await _authorizationService.RequireResourceAccessAsync(athlete, TeamRole.TeamOwner);

        await _athleteRepository.DeleteAsync(athlete);
    }
}
```

### In Controllers

```csharp
[ApiController]
[Route("api/teams")]
[Authorize]
public class TeamController : ControllerBase
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IStandardTeamService _standardTeamService;

    public TeamController(
        IAuthorizationService authorizationService,
        IStandardTeamService standardTeamService)
    {
        _authorizationService = authorizationService;
        _standardTeamService = standardTeamService;
    }

    [HttpGet("{teamId:guid}")]
    public async Task<IActionResult> GetTeam(Guid teamId)
    {
        // Ensure user can access this team
        await _authorizationService.RequireTeamAccessAsync(teamId);
        
        var team = await _standardTeamService.GetTeamAsync(teamId);
        return Ok(team);
    }

    [HttpPut("{teamId:guid}")]
    public async Task<IActionResult> UpdateTeam(Guid teamId, UpdateTeamDto dto)
    {
        // Require admin access to update team
        await _authorizationService.RequireTeamAdminAsync(teamId);
        
        await _standardTeamService.UpdateTeamAsync(teamId, dto);
        return NoContent();
    }

    [HttpDelete("{teamId:guid}")]
    public async Task<IActionResult> DeleteTeam(Guid teamId)
    {
        // Only team owners can delete teams
        await _authorizationService.RequireTeamOwnershipAsync(teamId);
        
        await _standardTeamService.DeleteTeamAsync(teamId);
        return NoContent();
    }
}
```