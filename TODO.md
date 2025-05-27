# General
- Refactor TeamManagementService

## Simplified Authorization TODO
- Reduce roles to 3-tier system (Global Admin, Team Owner/Admin, Team Member)
- Add MemberType field (Coach, Athlete, Parent) separate from authorization role
- Implement single team context for all non-global operations
- Simplify authorization to two main patterns: RequireGlobalAdmin and RequireTeamAccess
- Update TeamManagementService to use simplified role checks
- Remove complex role matrix and replace with simple owner/admin/member checks

## Security Refactoring

### **Phase 1: Authorization Model Simplification**

#### **1.1 Update Role Enum and Domain Model**
- **Task**: Simplify TeamRole enum to 3-tier system and add MemberType enum
- **Files**: `src/TeamStride.Domain/Entities/UserTeam.cs`
- **Suggested Prompt**: "Update the TeamRole enum in UserTeam.cs to implement the simplified 3-tier authorization model (GlobalAdmin, TeamOwner, TeamAdmin, TeamMember) and add a separate MemberType enum (Coach, Athlete, Parent) for business logic. Update the UserTeam entity to include both Role and MemberType properties."
--done

#### **1.2 Create Database Migration for Role Changes**
- **Task**: Generate migration to update existing role data and add MemberType column
- **Files**: New migration file
- **Suggested Prompt**: "Create a database migration to update the existing TeamRole values to the new simplified 3-tier system, add a MemberType column to UserTeam table, and migrate existing role data to appropriate MemberType values (Coach->Coach, Athlete->Athlete, Parent->Parent, Admin->Coach, Host->Coach)."
-- skipped

#### **1.3 Update ApplicationUser Global Admin Logic**
- **Task**: Ensure IsGlobalAdmin property works with simplified roles
- **Files**: `src/TeamStride.Domain/Identity/ApplicationUser.cs`
- **Suggested Prompt**: "Review and update the ApplicationUser.IsGlobalAdmin property and SetGlobalAdmin method to work correctly with the simplified authorization model. Ensure global admins are properly identified and can bypass team-level restrictions."
-- removed

### **Phase 2: Authorization Attributes and Middleware**

#### **2.1 Create RequireGlobalAdmin Authorization Attribute**
- **Task**: Implement custom authorization attribute for global admin operations
- **Files**: New file `src/TeamStride.Api/Authorization/RequireGlobalAdminAttribute.cs`
- **Suggested Prompt**: "Create a custom authorization attribute called RequireGlobalAdminAttribute that checks if the current user has global admin privileges. This should verify the IsGlobalAdmin claim in the JWT token and deny access if the user is not a global admin."
--done

#### **2.2 Create RequireTeamAccess Authorization Attribute**
- **Task**: Implement custom authorization attribute for team-level operations
- **Files**: New file `src/TeamStride.Api/Authorization/RequireTeamAccessAttribute.cs`
- **Suggested Prompt**: "Create a custom authorization attribute called RequireTeamAccessAttribute that checks if the current user has access to a specific team. This should support parameters for required roles (TeamOwner, TeamAdmin, TeamMember) and verify the user's team membership and role level."
-- done

#### **2.3 Create Authorization Policy Provider**
- **Task**: Implement policy-based authorization for the simplified model
- **Files**: New file `src/TeamStride.Api/Authorization/TeamStrideAuthorizationPolicyProvider.cs`
- **Suggested Prompt**: "Create an authorization policy provider that defines policies for 'GlobalAdmin' and 'TeamAccess' with support for role-based requirements. Include policies for 'TeamOwner', 'TeamAdmin', and 'TeamMember' access levels."
-- not required; skipped

### **Phase 3: JWT Token and Claims Updates**

#### **3.1 Update JWT Token Generation**
- **Task**: Modify JWT token to include simplified role claims and team context
- **Files**: `src/TeamStride.Infrastructure/Identity/JwtTokenService.cs`
- **Suggested Prompt**: "Update the JwtTokenService.GenerateJwtToken method to include claims for the simplified authorization model. Add 'IsGlobalAdmin', 'TeamRole', 'MemberType', and 'TeamId' claims. Ensure global admins get appropriate claims that allow them to bypass team restrictions."
-- done

#### **3.2 Update CurrentUserService**
- **Task**: Add methods to retrieve simplified role information from claims
- **Files**: `src/TeamStride.Infrastructure/Services/CurrentUserService.cs`
- **Suggested Prompt**: "Update CurrentUserService to include properties and methods for the simplified authorization model: TeamRole, MemberType, TeamId from JWT claims. Add helper methods like IsTeamOwner, IsTeamAdmin, IsTeamMember, and CanAccessTeam(Guid teamId)."
-- done

### **Phase 4: Team Context and Multi-Tenancy**

#### **4.1 Enhance Team Context Service**
- **Task**: Improve team context management for single team operations
- **Files**: `src/TeamStride.Infrastructure/Services/CurrentTeamService.cs`
- **Suggested Prompt**: "Enhance the CurrentTeamService to better support single team context for non-global operations. Add methods for team validation, automatic team resolution from JWT claims, and ensure proper team isolation. Include logging for team context changes."
-- done

#### **4.2 Update Global Query Filters**
- **Task**: Refine global query filters for simplified authorization model
- **Files**: `src/TeamStride.Infrastructure/Data/ApplicationDbContext.cs`
- **Suggested Prompt**: "Update the global query filters in ApplicationDbContext to work with the simplified authorization model. Ensure that standard users are properly restricted to their team data, while global admins can access all data when needed. Update the Team entity filter to use the new role structure."
-- done

#### **4.3 Create Team Context Middleware**
- **Task**: Enhance middleware to automatically set team context from subdomain/JWT
- **Files**: `src/TeamStride.Api/Middleware/TeamMiddleware.cs`
- **Suggested Prompt**: "Enhance the TeamMiddleware to automatically resolve and set team context from subdomain or JWT claims. Ensure proper team validation and context setting for both subdomain-based access and API access. Add error handling for invalid team contexts."

### **Phase 5: Service Layer Authorization Updates**

#### **5.1 Update TeamManagementService Authorization**
- **Task**: Replace complex authorization checks with simplified RequireGlobalAdmin and RequireTeamAccess patterns
- **Files**: `src/TeamStride.Infrastructure/Services/TeamManagementService.cs`
- **Suggested Prompt**: "Refactor all authorization methods in TeamManagementService to use the simplified authorization model. Replace the current complex role checks with simple RequireGlobalAdmin and RequireTeamAccess patterns. Update methods like EnsureCanManageTeamAsync, EnsureCanDeleteTeamAsync, etc."

#### **5.2 Update AthleteService Authorization**
- **Task**: Implement team-based authorization for athlete operations
- **Files**: `src/TeamStride.Infrastructure/Services/AthleteService.cs`
- **Suggested Prompt**: "Update AthleteService to use the simplified authorization model. Ensure all operations respect team context and user roles. Add authorization checks that verify users can only access athletes from their team (unless they're global admins)."

#### **5.3 Create Authorization Helper Service**
- **Task**: Create centralized authorization logic for reuse across services
- **Files**: New file `src/TeamStride.Application/Common/Services/IAuthorizationService.cs` and implementation
- **Suggested Prompt**: "Create a centralized authorization service that implements the two main authorization patterns: RequireGlobalAdmin and RequireTeamAccess. This service should be used by all other services to ensure consistent authorization logic across the application."

### **Phase 6: Controller Updates**

#### **6.1 Update Controller Authorization Attributes**
- **Task**: Apply new authorization attributes to all controllers
- **Files**: All controllers in `src/TeamStride.Api/Controllers/`
- **Suggested Prompt**: "Update all API controllers to use the new authorization attributes (RequireGlobalAdmin, RequireTeamAccess) instead of the current authorization logic. Ensure proper role-based access control is applied to all endpoints according to the requirements."

#### **6.2 Add Team Context Validation**
- **Task**: Ensure controllers validate team context for team-specific operations
- **Files**: Controllers that work with team-specific data
- **Suggested Prompt**: "Add team context validation to controllers that handle team-specific operations. Ensure that team IDs in requests match the user's authorized team context (unless the user is a global admin)."

### **Phase 7: Security Enhancements**

#### **7.1 Implement Audit Logging for Security Events**
- **Task**: Add comprehensive audit logging for authentication and authorization events
- **Files**: New audit logging service and updates to existing services
- **Suggested Prompt**: "Implement comprehensive audit logging for security events including login attempts, authorization failures, team access changes, and role modifications. Create an audit service that logs security-relevant events with correlation IDs and user context."

#### **7.2 Add Rate Limiting and Security Headers**
- **Task**: Implement rate limiting and security headers middleware
- **Files**: New middleware files and startup configuration
- **Suggested Prompt**: "Add rate limiting middleware to prevent abuse of authentication endpoints and implement security headers middleware (HSTS, CSP, X-Frame-Options, etc.). Configure appropriate rate limits for login, registration, and password reset operations."

#### **7.3 Enhance Token Security**
- **Task**: Implement token rotation and improved refresh token security
- **Files**: `src/TeamStride.Infrastructure/Identity/JwtTokenService.cs` and related files
- **Suggested Prompt**: "Enhance JWT token security by implementing automatic token rotation, secure refresh token handling, and token revocation capabilities. Add token blacklisting for logout and ensure refresh tokens are properly secured and rotated."

### **Phase 8: Testing and Validation**

#### **8.1 Update Authorization Tests**
- **Task**: Update all authorization-related tests for the simplified model
- **Files**: Test files in `tests/` directories
- **Suggested Prompt**: "Update all authorization and security tests to work with the simplified 3-tier authorization model. Ensure comprehensive test coverage for global admin access, team-level access, and proper isolation between teams."

#### **8.2 Create Security Integration Tests**
- **Task**: Add comprehensive security integration tests
- **Files**: New test files for security scenarios
- **Suggested Prompt**: "Create comprehensive security integration tests that verify the complete authorization flow from JWT token generation through API access. Test scenarios including cross-team access attempts, privilege escalation attempts, and proper team isolation."

