# TeamStride API Controllers Implementation Summary

This document summarizes the API controllers that have been implemented for the TeamStride team management platform.

## Implemented Controllers

### 1. TeamsController (`/api/teams`)
**Purpose**: Core team management operations including CRUD, subdomain management, and tier limits.

**Endpoints**:
- `GET /api/teams` - Get paginated list of teams user has access to
- `GET /api/teams/{teamId}` - Get team by ID
- `GET /api/teams/subdomain/{subdomain}` - Get team by subdomain
- `POST /api/teams` - Create new team
- `PUT /api/teams/{teamId}` - Update team basic properties
- `DELETE /api/teams/{teamId}` - Delete team (soft delete)
- `GET /api/teams/subdomain/{subdomain}/availability` - Check subdomain availability
- `PUT /api/teams/{teamId}/subdomain` - Update team subdomain
- `GET /api/teams/tiers/{tier}/limits` - Get tier limits (no team access required)
- `GET /api/teams/{teamId}/can-add-athlete` - Check if team can add more athletes (no team access required)

**Authorization**:
- All endpoints require `RequireTeamAccess` attribute except `GetTierLimits` and `CanAddAthlete`
- Different role requirements: TeamMember, TeamAdmin, or TeamOwner based on operation
- Global admins bypass all team access restrictions

### 2. OwnershipTransferController (`/api/ownershiptransfer`)
**Purpose**: Team ownership transfer operations.

**Endpoints**:
- `POST /api/ownershiptransfer/teams/{teamId}/transfer` - Initiate ownership transfer
- `POST /api/ownershiptransfer/complete/{transferToken}` - Complete ownership transfer (Anonymous)
- `DELETE /api/ownershiptransfer/{transferId}` - Cancel ownership transfer
- `GET /api/ownershiptransfer/teams/{teamId}/pending` - Get pending transfers for team

### 3. TeamSubscriptionController (`/api/teamsubscription`)
**Purpose**: Team subscription and branding management.

**Endpoints**:
- `PUT /api/teamsubscription/teams/{teamId}/subscription` - Update team subscription
- `PUT /api/teamsubscription/teams/{teamId}/branding` - Update team branding

### 4. TeamMembersController (`/api/teammembers`)
**Purpose**: Team member management operations.

**Endpoints**:
- `GET /api/teammembers/teams/{teamId}/members` - Get paginated list of team members
- `PUT /api/teammembers/teams/{teamId}/members/{userId}/role` - Update member role
- `DELETE /api/teammembers/teams/{teamId}/members/{userId}` - Remove member from team

### 5. GlobalAdminTeamsController (`/api/admin/teams`)
**Purpose**: Global admin team management operations (bypasses normal team access restrictions).

**Endpoints**:
- `GET /api/admin/teams` - Get all teams with advanced filtering
- `GET /api/admin/teams/deleted` - Get deleted teams that can be recovered
- `GET /api/admin/teams/{teamId}` - Get team by ID (admin view)
- `POST /api/admin/teams/with-new-owner` - Create team with new user as owner
- `POST /api/admin/teams/with-existing-owner` - Create team with existing user as owner
- `PUT /api/admin/teams/{teamId}` - Update team (all properties)
- `DELETE /api/admin/teams/{teamId}` - Soft delete team
- `DELETE /api/admin/teams/{teamId}/permanent` - Permanently delete team
- `POST /api/admin/teams/{teamId}/recover` - Recover soft-deleted team
- `POST /api/admin/teams/{teamId}/transfer-ownership` - Immediate ownership transfer

### 6. PublicTeamsController (`/api/public/teams`)
**Purpose**: Handles public team operations that don't require authentication (primarily for team registration).

**Endpoints**:
- `GET /api/public/teams/subdomain-availability` - Check if subdomain is available for new team registration

**Authorization**:
- All endpoints are public (no authentication required)
- Designed for anonymous users during team registration process

### 7. AuthenticationController (`/api/Authentication`)
**Purpose**: User authentication and account management operations.

**Endpoints**:
- `POST /api/Authentication/register` - Register new user account
- `POST /api/Authentication/login` - User login with credentials
- `POST /api/Authentication/refresh-token` - Refresh JWT token
- `GET /api/Authentication/confirm-email` - Confirm user email address (Anonymous)
- `POST /api/Authentication/forgot-password` - Initiate password reset process
- `POST /api/Authentication/reset-password` - Reset password with token (Anonymous)
- `POST /api/Authentication/change-password` - Change password for authenticated user
- `POST /api/Authentication/logout` - User logout
- `GET /api/Authentication/external-login/{provider}` - Get external login URL
- `GET /api/Authentication/external-login/{provider}/callback` - Get external login callback URL
- `POST /api/Authentication/external-login` - Process external authentication

**Authorization**:
- Most endpoints are public (registration, login, password reset)
- `change-password` and `logout` require authentication
- Email confirmation and password reset support anonymous access via tokens

### 8. TenantSwitcherController (`/api/tenant-switcher`)
**Purpose**: Multi-tenant operations for users with access to multiple teams.

**Endpoints**:
- `GET /api/tenant-switcher/tenants` - Get all tenants (teams) user has access to

**Authorization**:
- All endpoints require authentication
- Returns teams the current user can switch between

### 9. HealthController (`/api/Health`)
**Purpose**: API health monitoring and status checks.

**Endpoints**:
- `GET /api/Health` - Get API health status and timestamp

**Authorization**:
- Public endpoint (no authentication required)
- Returns health status and current timestamp

## Key Features

### Authentication & Authorization
- All endpoints require authentication except health check, ownership transfer completion, email confirmation, and password reset
- Role-based access control using `RequireTeamAccess` and `RequireGlobalAdmin` attributes
- Hierarchical team roles: TeamOwner > TeamAdmin > TeamMember
- Global admins bypass all team access restrictions
- Support for external authentication providers
- JWT token-based authentication with refresh token support
- Proper error handling for unauthorized access

### Data Transfer Objects (DTOs)
- `TeamDto` - Standard team information with statistics
- `CreateTeamDto` - Team creation data
- `UpdateTeamDto` - Team update data
- `GlobalAdminTeamDto` - Enhanced team information for admin operations
- `OwnershipTransferDto` - Transfer status and details
- `UpdateSubscriptionDto` - Subscription update data
- `UpdateTeamBrandingDto` - Branding update data
- `TeamMemberDto` - Team member information
- `TeamTierLimitsDto` - Tier limitations and features
- `TenantDto` - Tenant/team information for switching
- `AuthResponseDto` - Authentication response with tokens
- `RegisterRequestDto` - User registration data
- `LoginRequestDto` - User login credentials
- `ExternalAuthRequestDto` - External authentication data

### Error Handling
- Consistent error responses with trace IDs
- Proper HTTP status codes (200, 201, 400, 401, 403, 404, 500)
- Detailed error messages for validation failures
- Exception handling middleware integration

### Validation
- Model validation using Data Annotations
- Custom validation for business rules
- Subdomain format validation
- Color code validation for branding
- Email format validation
- Password complexity requirements

### Pagination
- Built-in pagination support for list endpoints
- Configurable page size and number
- Search and filtering capabilities

## Service Integration

All controllers integrate with the appropriate services:
- **TeamsController**: Uses `IStandardTeamService` for standard team operations
- **GlobalAdminTeamsController**: Uses `IGlobalAdminTeamService` for admin operations
- **AuthenticationController**: Uses `ITeamStrideAuthenticationService` for authentication operations
- **TenantSwitcherController**: Uses `ITenantSwitcherService` for multi-tenant operations
- **Other Controllers**: Use `IStandardTeamService` for their specific domains

Services provide:
- Team CRUD operations
- Ownership transfer management
- Subscription and branding updates
- Member management
- Authorization checks
- Tier limit validation
- User authentication and account management
- Multi-tenant access management

## Testing

Basic integration tests have been created to verify:
- Controller endpoints are accessible
- Authentication requirements are enforced
- Proper HTTP status codes are returned
- Error handling works correctly

## Next Steps

1. **Athletes Management**: Implement AthletesController for athlete CRUD operations
2. **Advanced Testing**: Add comprehensive authenticated test scenarios for all controllers
3. **API Documentation**: Enhance Swagger documentation with examples and detailed descriptions
4. **Rate Limiting**: Implement rate limiting for public endpoints
5. **Caching**: Add caching for frequently accessed data like tier limits
6. **Monitoring**: Add detailed logging and metrics collection
7. **External Integration**: Complete external authentication provider setup
8. **Email Templates**: Implement branded email templates for notifications

## Architecture Notes

The controllers follow clean architecture principles:
- Controllers handle HTTP concerns only
- Business logic is in the service layer
- Data access is abstracted through repositories
- DTOs provide clear API contracts
- Proper separation of concerns throughout

All controllers inherit from `BaseApiController` which provides:
- Consistent error handling
- Logging integration
- Common HTTP response helpers
- Trace ID generation for debugging

## Authorization Model

The new `RequireTeamAccess` attribute provides:
- Automatic team ID validation from route parameters
- Role hierarchy enforcement (TeamOwner > TeamAdmin > TeamMember)
- Global admin bypass functionality
- Flexible team ID source configuration (route-based or context-based)

The `RequireGlobalAdmin` attribute provides:
- Global administrator access control
- Bypass mechanism for all team-level restrictions
- Centralized admin operation authorization

## Notable Missing Features

While the application has a complete `Athlete` domain model with services and DTOs, there is currently **no AthletesController** implemented. The athlete management functionality exists in the service layer but lacks HTTP API endpoints. This represents a significant gap that should be addressed in the next development iteration.

The athlete domain includes:
- Complete CRUD operations in `IAthleteService`
- Rich athlete profile management
- Team roster operations
- Captain role management
- Physical and waiver status tracking
- Athletic performance data 