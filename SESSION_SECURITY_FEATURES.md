# TeamStride Session Security Features

## Overview

The TeamStride application implements a comprehensive multi-layered session security system that combines client-side and server-side mechanisms to ensure secure user sessions and protect against various security threats. The system includes idle timeout management, heartbeat validation, session fingerprinting, role-based authorization, and administrative force logout capabilities.

---

## 1. Authentication & Token Management

### JWT Token Authentication
- **Implementation**: JWT Bearer token authentication using HMAC-SHA256 signing
- **Location**: `src/TeamStride.Infrastructure/Identity/JwtTokenService.cs`
- **Configuration**: Configurable JWT settings in `appsettings.json`

#### JWT Token Claims:
- `sub`: User ID 
- `email`: User email address
- `first_name`: User's first name
- `last_name`: User's last name
- `team_id`: User's associated team ID
- `team_role`: User's role within the team (TeamOwner, TeamAdmin, TeamMember)
- `member_type`: User's member type (Coach, Athlete, Parent)
- `is_global_admin`: Global administrator flag

#### Token Configuration:
```json
{
  "Authentication": {
    "JwtExpirationMinutes": 60,
    "JwtIssuer": "https://api.teamstride.com",
    "JwtAudience": "https://teamstride.com"
  }
}
```

### Refresh Token System
- **Expiration**: 7 days from creation
- **Security**: Cryptographically secure random tokens
- **Tracking**: IP address tracking for refresh token usage
- **Revocation**: Old refresh tokens are revoked when new ones are issued

---

## 2. Role-Based Authorization

### 3-Tier Authorization Model

#### Team Roles Hierarchy (Highest to Lowest):
1. **TeamOwner**: Full control including ownership transfer and billing
2. **TeamAdmin**: Team management except ownership and billing
3. **TeamMember**: Basic access based on MemberType

### Authorization Attributes

#### RequireTeamAccessAttribute
- **Location**: `src/TeamStride.Api/Authorization/RequireTeamAccessAttribute.cs`
- **Features**:
  - Role hierarchy enforcement
  - Global admin bypass
  - Team context validation
  - Route parameter validation

#### RequireGlobalAdminAttribute  
- **Location**: `src/TeamStride.Api/Authorization/RequireGlobalAdminAttribute.cs`
- **Purpose**: Restricts access to global administrators only

### Global Admin Privileges
- **Bypass**: Global admins bypass all team-based restrictions
- **Detection**: Via `is_global_admin` JWT claim
- **Access**: Can access any team or resource regardless of membership

---

## 3. Idle Timeout System

### Client-Side Implementation

#### Core Hook: useIdleTimeout
- **Location**: `web/src/utils/useIdleTimeout.ts`
- **Timeout Duration**: 5 minutes total (4 minutes idle + 1 minute warning)
- **Monitored Events**: 
  - `mousedown`, `mousemove`, `keypress`, `scroll`, `touchstart`, `click`

#### Activity Detection Features:
- **Throttling**: 1-second intervals to prevent excessive resets
- **Warning System**: Shows modal 1 minute before logout
- **Countdown Timer**: Real-time countdown during warning period
- **Modal Interaction Prevention**: Modal interactions don't reset idle timer

#### Idle Timeout Components:
- **AuthenticatedIdleTimeout**: Main component integrating idle timeout with auth
- **IdleTimeoutProvider**: Application-wide provider
- **IdleTimeoutModal**: Warning modal with countdown and user actions

### Configuration:
```typescript
const TIMEOUT_DURATION = 5 * 60 * 1000;  // 5 minutes
const WARNING_DURATION = 1 * 60 * 1000;  // 1 minute warning
```

---

## 4. Heartbeat Validation System

### Client-Side Heartbeat
- **Location**: `web/src/components/SessionSecurityProvider.tsx`
- **Interval**: 90 seconds
- **Failure Tolerance**: Maximum 5 missed heartbeats before logout
- **Endpoint**: `POST /api/authentication/heartbeat`

### Server-Side Validation
- **Location**: `src/TeamStride.Api/Controllers/AuthenticationController.cs`
- **Validates**:
  - JWT token validity
  - User existence and active status
  - Session fingerprint match
  - Force logout status

#### Heartbeat Process:
1. Client sends heartbeat request with session fingerprint
2. Server validates JWT token and user status
3. Server checks for force logout conditions
4. Server updates user's last activity timestamp
5. Server validates/stores session fingerprint
6. Returns 200 OK for valid sessions, 401 Unauthorized for invalid

---

## 5. Session Fingerprinting

### Fingerprint Generation
- **Location**: `web/src/utils/auth.ts`
- **Components**:
  - User agent (first 100 characters)
  - Browser language
  - Timezone
  - Screen resolution

### Fingerprint Validation
- **Client-Side**: Validates fingerprint consistency
- **Server-Side**: Stores and validates fingerprints in `UserSessions` table
- **Security**: Detects potential session hijacking attempts

#### Implementation:
```typescript
const fingerprint = {
  userAgent: navigator.userAgent.substring(0, 100),
  language: navigator.language,
  timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
  screen: `${screen.width}x${screen.height}`
  // Note: timestamp removed to ensure consistent fingerprints
};
```

---

## 6. Database Session Management

### UserSession Entity
- **Location**: `src/TeamStride.Domain/Entities/UserSession.cs`
- **Fields**:
  - `Id`: Unique session identifier
  - `UserId`: Associated user
  - `Fingerprint`: Session fingerprint (max 2000 chars)
  - `CreatedOn`: Session creation timestamp
  - `LastActiveOn`: Last activity timestamp
  - `IsActive`: Session status flag

### User Security Fields
- **LastActivityOn**: Tracks user's last activity
- **ForceLogoutAfter**: Timestamp for administrative logout enforcement

---

## 7. Force Logout System

### Administrative Control
- **Endpoint**: `/api/authentication/force-logout/{userId}`
- **Functionality**:
  - Sets `ForceLogoutAfter` timestamp
  - Revokes all refresh tokens
  - Causes subsequent heartbeat validations to fail

### Implementation Process:
1. Admin triggers force logout for specific user
2. System sets ForceLogoutAfter timestamp to current time
3. All refresh tokens for user are revoked
4. Next heartbeat validation fails due to timestamp check
5. User is automatically logged out

---

## 8. Focus-Based Validation

### Focus-Based Validation
- **Trigger**: Window focus/visibility change events
- **Validation**:
  - Token expiration check
  - Session fingerprint validation
  - Optional immediate heartbeat request

---

## 9. Security Violation Handling

### Immediate Logout Scenarios:
- Heartbeat validation failures (5 consecutive failures)
- Session fingerprint mismatches
- Token expiration detection
- Force logout enforcement
- Focus validation failures

### Security Logout Process:
1. Clear all session data from localStorage
2. Stop all timers and intervals
3. Hard page reload to ensure clean state
4. No warning modal displayed

---

## 10. Password Security

### Password Requirements
- **Minimum Length**: 8 characters
- **Requirements**:
  - At least one digit
  - At least one lowercase letter
  - At least one uppercase letter
  - At least one non-alphanumeric character

### Password Management Features:
- Password change with current password verification
- Password reset via email with secure tokens
- Account lockout protection

---

## 11. Email Verification & Security

### Email Confirmation
- **Requirement**: Required for account activation
- **Implementation**: Secure token-based confirmation
- **Link Format**: `https://teamstride.com/confirm-email?userId={id}&token={token}`

### Password Reset
- **Security**: Time-limited, single-use tokens
- **Process**: Email-based password reset with secure links

---

## 12. External Authentication Support

### Supported Providers:
- Microsoft Account
- Google
- Facebook
- Twitter

### OAuth Configuration:
- Client ID and secret management
- Secure callback handling
- User profile integration

---

## 13. Security Middleware

### Exception Handling Middleware
- **Location**: `src/TeamStride.Api/Middleware/ExceptionHandlingMiddleware.cs`
- **Purpose**: Secure error handling without information disclosure

### Team Context Middleware
- **Location**: `src/TeamStride.Api/Middleware/ApiTeamContextMiddleware.cs`
- **Purpose**: Secure team context resolution and validation

---

## 14. Configuration & Environment Security

### Production Security Settings:
- HTTPS enforcement
- Secure cookie settings
- CORS configuration
- Environment-specific configurations

### Development vs Production:
- Swagger UI only in development
- Static file serving configuration
- Different authentication flows

---

## 15. Monitoring & Logging

### Security Event Logging:
- Failed authentication attempts
- Heartbeat validation failures
- Session fingerprint mismatches
- Force logout events
- Token validation errors

### Activity Tracking:
- User login timestamps
- Last activity tracking
- Session creation and termination

---

## 16. Security Best Practices Implemented

### Token Security:
- Short JWT expiration (60 minutes)
- Secure refresh token rotation
- Token validation on every request

### Session Security:
- Idle timeout enforcement
- Activity monitoring
- Fingerprint validation

### Administrative Security:
- Force logout capabilities
- Role-based access control
- Global admin privileges

### Communication Security:
- HTTPS enforcement
- Secure headers
- CORS protection

---

## Configuration Summary

### Client-Side Timeouts:
```typescript
IDLE_TIMEOUT = 5 minutes (300,000ms)
WARNING_TIME = 1 minute (60,000ms)
HEARTBEAT_INTERVAL = 90 seconds (90,000ms)
MAX_MISSED_HEARTBEATS = 5
```

### Server-Side Settings:
```csharp
JWT_EXPIRATION = 60 minutes
REFRESH_TOKEN_EXPIRATION = 7 days
FINGERPRINT_MAX_LENGTH = 2000 characters
```

This comprehensive security implementation ensures robust session management, protects against common attack vectors, and provides administrators with the tools needed to maintain security across the application. 