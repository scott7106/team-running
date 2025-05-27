Add remaining unit tests for the TeamManagementService
Roles should be Owner, Admin, Coach, Parent, Athlete (Host will be a shadow role for IsGlobalAdmin users)
Implement a custom Role and RoleManager which has User, Role and Team. Remove UserTeams as it will not be needed.
Implement custom UserManager with IsInTeamAsync, IsGlobalAdminAsync and IsInRoleAsync which accounts for team correctly
Implement a way to disable Team global filter without disabling the deleted filter for global admins
Create a set of unit tests for the IHasTenant interface which tests visibility (keep this separate from specific unit tests for things like athletes, teams, etc.)
IsGlobalAdmin disables the global team filter when the user selects HOST as their selected team (this sets CurrentTeamService.TeamId to null). Otherwise the filter applies.
Refactor TeamManagementService into several partial classes, put the get functions into the main class, the crud operations into a separate file and helpers into a third file

## Security Refactoring

### Phase 1: Domain Model Updates
1. **Update UserRole Entity**
   - Add `TeamId` property to `UserRole` table
   - Create migration to add `TeamId` column and foreign key relationship
   - Update `UserRole` to include team-specific role assignments

2. **Simplify Role Enum**
   - Update `TeamRole` enum to: `Owner`, `Admin`, `Member`
   - Remove `Host`, `Coach`, `Parent`, `Athlete` from authorization roles
   - Add `MemberType` enum for business logic: `Coach`, `Athlete`, `Parent`

3. **Update User Entity**
   - Add `MemberType` property to `User` entity for business logic
   - Remove complex role calculations that depend on multiple tables

### Phase 2: Authorization Infrastructure
4. **Create Custom Authorization Services**
   - Implement `ICurrentUserService` with simplified methods:
     - `IsGlobalAdminAsync()`
     - `IsTeamOwnerAsync(teamId)`
     - `IsTeamAdminAsync(teamId)` 
     - `HasTeamAccessAsync(teamId)`
   - Remove complex role checking logic from controllers

5. **Implement Authorization Attributes**
   - Create `RequireGlobalAdminAttribute` for platform-wide operations
   - Create `RequireTeamAccessAttribute` for team-specific operations
   - Replace existing authorization logic with these two patterns

6. **Update Entity Framework Filters**
   - Modify global team filter to respect `CurrentTeamService.TeamId`
   - Implement logic to disable team filter when `TeamId` is null (Global Admin "HOST" mode)
   - Ensure filter applies correctly for non-global admin users

### Phase 3: Service Layer Updates
7. **Update TeamManagementService**
   - Simplify authorization checks using new `ICurrentUserService` methods
   - Remove complex permission calculations
   - Update all methods to use `RequireGlobalAdmin` or `RequireTeamAccess` patterns

8. **Update AthleteService**
   - Replace current authorization logic with simplified team access checks
   - Ensure athletes can only be accessed within user's team context
   - Update CRUD operations to respect team boundaries

9. **Update Other Domain Services**
   - Apply same authorization patterns to all services (Schedules, Results, etc.)
   - Ensure consistent security model across all domain operations

### Phase 4: API Controller Updates
10. **Update Controller Authorization**
    - Replace existing `[Authorize]` attributes with `[RequireGlobalAdmin]` or `[RequireTeamAccess]`
    - Remove manual permission checks from controller actions
    - Ensure consistent authorization pattern across all controllers

11. **Update Team Selection Logic**
    - Implement "HOST" mode for Global Admins (sets `CurrentTeamService.TeamId = null`)
    - For non-global users, auto-set team context based on user's team membership
    - Remove team switching UI for non-global admin users

### Phase 5: Database Migration & Cleanup
12. **Data Migration**
    - Create migration script to populate `UserRole.TeamId` from existing `UserTeams` data
    - Verify data integrity before removing `UserTeams` table
    - Update existing role assignments to new simplified structure

13. **Remove Deprecated Tables**
    - Remove `UserTeams` entity and table
    - Clean up unused role-related code
    - Remove complex authorization helpers

### Phase 6: Testing & Validation
14. **Update Unit Tests**
    - Refactor existing authorization tests for new simplified model
    - Add comprehensive tests for `ICurrentUserService` methods
    - Test Global Admin "HOST" mode functionality
    - Verify team isolation works correctly for non-global users

15. **Integration Testing**
    - Test end-to-end authorization flows
    - Verify subdomain-based team selection works with new model
    - Test that Global Admins can access all teams in "HOST" mode
    - Ensure team members can only access their assigned team

### Phase 7: UI/UX Updates
16. **Update Frontend Authorization**
    - Remove team switcher for non-global admin users
    - Implement Global Admin team switcher with "HOST" mode option
    - Update navigation to reflect user's authorization level
    - Ensure consistent user experience based on role

17. **Documentation Updates**
    - Update API documentation to reflect new authorization model
    - Create developer guide for the simplified authorization patterns
    - Document the two main authorization attributes and their usage