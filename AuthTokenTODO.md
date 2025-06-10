# Auth Token Refactor TODO

## Overview
Refactor the JWT token structure to include all team memberships instead of just the current team. The current team is determined by the subdomain context.

## Changes Made
- ✅ Refactored `AuthResponseDto` to include `List<TeamMembershipDto> Teams`
- ✅ Updated JWT token generation to include team memberships

## Backend Changes Needed

### 1. Update TeamMembershipDto ✅
**File:** `src/TeamStride.Application/Authentication/Dtos/AuthResponseDto.cs`
- ✅ Add `TeamSubdomain` property to `TeamMembershipDto`
- ✅ Update all locations where TeamMembershipDto is created to include TeamSubdomain

### 2. Update CurrentUserService ✅
**File:** `src/TeamStride.Infrastructure/Services/CurrentUserService.cs`
- ✅ Remove `TeamId` property (no longer in claims)
- ✅ Remove direct `TeamRole` property (no longer in claims) 
- ✅ Remove direct `MemberType` property (no longer in claims)
- ✅ Keep `CurrentTeamId` but delegate to `ICurrentTeamService`
- ✅ Add `CurrentTeamRole` property that delegates to `ICurrentTeamService`
- ✅ Add `CurrentMemberType` property that delegates to `ICurrentTeamService`
- ✅ Update helper methods (`IsTeamOwner`, `IsTeamAdmin`, `IsTeamMember`) to use delegation

### 3. Update CurrentTeamService Interface ✅
**File:** `src/TeamStride.Domain/Interfaces/ICurrentTeamService.cs`
- ✅ Add methods to parse team memberships from JWT claims
- ✅ Add properties for `CurrentTeamRole` and `CurrentMemberType`
- ✅ Add `TeamMembershipInfo` record for parsed memberships

### 4. Update CurrentTeamService Implementation ✅
**File:** `src/TeamStride.Infrastructure/Services/CurrentTeamService.cs`
- ✅ Parse `team_memberships` claim (JSON array of memberships)
- ✅ Match current subdomain with team membership to find current team context
- ✅ Implement `CurrentTeamRole` and `CurrentMemberType` properties
- ✅ Handle cases where user doesn't have membership in current subdomain team
- ✅ Update `CanAccessTeam` and `HasMinimumTeamRole` methods

### 5. Update JWT Token Generation ✅
**File:** JWT token service (need to locate)
- ✅ Include team memberships with `TeamSubdomain` in claims
- ✅ Serialize team memberships as JSON in claims

### 6. Update Authorization
**File:** Controllers with `RequireTeamAccess` attribute
- [ ] Ensure proper forbidden response when user lacks team membership
- [ ] Verify authorization logic works with new structure

## Frontend Changes Needed

### 1. Update Auth Utils ✅
**File:** `web/src/utils/auth.ts`
- ✅ Update `JwtClaims` interface to replace individual team properties with `team_memberships`
- ✅ Update `getTeamContextFromToken()` to parse memberships and match with subdomain
- ✅ Add logic to find current team membership based on subdomain
- ✅ Handle cases where user doesn't belong to current subdomain team
- ✅ Add helper functions for parsing team memberships
- ✅ Update subdomain refresh logic to check team memberships

## Testing Required
- [ ] Test user with multiple team memberships
- [ ] Test user accessing team they don't belong to (should get forbidden)
- [ ] Test team switching via subdomain changes
- [ ] Test global admin access across all teams
- [ ] Test JWT claims parsing and validation

## Edge Cases to Handle
- [ ] User with no team memberships
- [ ] User accessing subdomain for team they don't belong to
- [ ] Global admin accessing any team context
- [ ] Malformed team membership claims
- [ ] Missing subdomain context 