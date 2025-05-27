# TeamStride Authorization Attributes

This directory contains custom authorization attributes for the TeamStride application that implement the simplified 3-tier authorization model.

## RequireTeamAccessAttribute

The `RequireTeamAccessAttribute` is a custom authorization attribute that enforces team-based access control with role hierarchy support.

### Features

- **Role Hierarchy**: Supports the 3-tier role system (TeamOwner > TeamAdmin > TeamMember)
- **Global Admin Bypass**: Global admins automatically bypass all team restrictions
- **Team Context Validation**: Optionally validates that route team IDs match user's team context
- **Flexible Configuration**: Configurable minimum role requirements and route validation

### Usage

#### Basic Usage

```csharp
[RequireTeamAccess] // Default: TeamMember role required
public IActionResult GetTeamData() { ... }
```

#### Specify Minimum Role

```csharp
[RequireTeamAccess(TeamRole.TeamAdmin)] // Requires TeamAdmin or TeamOwner
public IActionResult ManageTeam() { ... }

[RequireTeamAccess(TeamRole.TeamOwner)] // Requires TeamOwner only
public IActionResult TransferOwnership() { ... }
```

#### Disable Route Validation

```csharp
[RequireTeamAccess(TeamRole.TeamMember, requireTeamIdFromRoute: false)]
public IActionResult GetMyTeamData() { ... }
```

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `minimumRequiredRole` | `TeamRole` | `TeamRole.TeamMember` | Minimum role required to access the resource |
| `requireTeamIdFromRoute` | `bool` | `true` | Whether to validate team ID from route matches user's team |

### Role Hierarchy

The attribute respects the following role hierarchy:

1. **TeamOwner** (highest privileges)
   - Can access all TeamOwner, TeamAdmin, and TeamMember endpoints
   - Full control over team including ownership transfer and billing

2. **TeamAdmin** (moderate privileges)
   - Can access TeamAdmin and TeamMember endpoints
   - Cannot access TeamOwner-only endpoints
   - Full team management except ownership and billing

3. **TeamMember** (basic privileges)
   - Can only access TeamMember endpoints
   - Limited access based on MemberType (Coach, Athlete, Parent)

### Global Admin Bypass

Users with the `is_global_admin` claim set to `true` automatically bypass all team access restrictions and can access any endpoint regardless of team membership or role.

### Route Parameter Detection

The attribute automatically detects team IDs from common route parameter names:
- `teamId` (preferred)
- `id` (when used in team-related controllers)

### JWT Claims Required

The attribute expects the following claims in the JWT token:

| Claim | Description | Required |
|-------|-------------|----------|
| `is_global_admin` | Global admin flag | No (defaults to false) |
| `team_id` | User's team ID | Yes (unless global admin) |
| `team_role` | User's role in the team | Yes (unless global admin) |

### Response Codes

| Status Code | Scenario |
|-------------|----------|
| `200 OK` | User has sufficient access |
| `401 Unauthorized` | User is not authenticated |
| `403 Forbidden` | User lacks sufficient role or team access |

### Examples

#### Team Management Endpoints

```csharp
[ApiController]
[Route("api/teams")]
public class TeamController : ControllerBase
{
    // Any team member can view team info
    [HttpGet("{teamId:guid}")]
    [RequireTeamAccess(TeamRole.TeamMember)]
    public IActionResult GetTeam(Guid teamId) { ... }

    // Only admins can update team settings
    [HttpPut("{teamId:guid}")]
    [RequireTeamAccess(TeamRole.TeamAdmin)]
    public IActionResult UpdateTeam(Guid teamId) { ... }

    // Only owners can delete teams
    [HttpDelete("{teamId:guid}")]
    [RequireTeamAccess(TeamRole.TeamOwner)]
    public IActionResult DeleteTeam(Guid teamId) { ... }
}
```

#### User Context Endpoints

```csharp
[ApiController]
[Route("api/my")]
public class MyController : ControllerBase
{
    // Works with user's current team context
    [HttpGet("team")]
    [RequireTeamAccess(requireTeamIdFromRoute: false)]
    public IActionResult GetMyTeam() { ... }

    // Admin actions on user's team
    [HttpPost("team/invite")]
    [RequireTeamAccess(TeamRole.TeamAdmin, requireTeamIdFromRoute: false)]
    public IActionResult InviteUser() { ... }
}
```

#### Combined with Other Attributes

```csharp
[HttpGet("teams/{teamId:guid}/sensitive-data")]
[RequireTeamAccess(TeamRole.TeamAdmin)]
[ProducesResponseType(typeof(SensitiveDataDto), 200)]
[ProducesResponseType(401)]
[ProducesResponseType(403)]
public IActionResult GetSensitiveData(Guid teamId) { ... }
```

### Error Messages

The attribute provides descriptive error messages for different failure scenarios:

- `"User is not associated with any team"`
- `"User team role is not specified or invalid"`
- `"Access denied: User does not have access to the specified team"`
- `"Access denied: Minimum required role is {requiredRole}, but user has {userRole}"`

### Testing

Comprehensive unit tests are available in `RequireTeamAccessAttributeTests.cs` covering:
- Authentication validation
- Global admin bypass
- Role hierarchy enforcement
- Team ID validation
- Route parameter detection
- Error scenarios

## RequireGlobalAdminAttribute

The `RequireGlobalAdminAttribute` restricts access to global administrators only.

### Usage

```csharp
[RequireGlobalAdmin]
public IActionResult ManageAllTeams() { ... }
```

This attribute checks for the `is_global_admin` claim in the JWT token and denies access if the user is not a global admin. 