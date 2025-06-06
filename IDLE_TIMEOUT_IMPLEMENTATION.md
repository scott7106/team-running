# Idle Timeout Implementation Documentation

## Overview

The TeamStride application implements a comprehensive idle timeout system to automatically log out users after periods of inactivity, ensuring security and session management. The implementation combines client-side activity detection with server-side session validation through a heartbeat mechanism.

## Architecture Components

### 1. Client-Side Components

#### Core Hook: `useIdleTimeout`
**Location**: `web/src/utils/useIdleTimeout.ts`

The `useIdleTimeout` hook is the foundation of the idle timeout system, providing:

- **Activity Detection**: Monitors user activity through DOM events
- **Warning System**: Shows warnings before automatic logout
- **Countdown Timer**: Displays remaining time during warning period
- **Configurable Timeouts**: Supports customizable timeout and warning durations

**Key Features**:
- Monitors activity events: `mousedown`, `mousemove`, `keypress`, `scroll`, `touchstart`, `click`
- Uses refs to prevent unnecessary re-renders
- Throttles activity detection (1-second intervals)
- Provides countdown functionality during warning period

**Configuration Interface**:
```typescript
interface UseIdleTimeoutOptions {
  timeout: number;        // Total timeout in milliseconds
  warningTime: number;    // Warning duration in milliseconds
  onIdle?: () => void;    // Callback when user becomes idle
  onActive?: () => void;  // Callback when user becomes active
  onWarning?: () => void; // Callback when warning is shown
}
```

#### Authenticated Idle Timeout Component
**Location**: `web/src/components/AuthenticatedIdleTimeout.tsx`

This component integrates the idle timeout hook with authentication logic:

- **Timeout Configuration**: 5 minutes total, 1 minute warning
- **Authentication Check**: Only runs for authenticated users
- **Automatic Logout**: Calls `logout()` function when timeout expires
- **Storage Monitoring**: Listens for token changes across browser tabs

#### Idle Timeout Provider
**Location**: `web/src/components/IdleTimeoutProvider.tsx`

Wraps the application to provide idle timeout functionality:

- **Route-Based Activation**: Only active on authenticated pages
- **Public Page Exclusion**: Excludes `/` and `/login` routes
- **Global Integration**: Integrated at the root layout level

#### Idle Timeout Modal
**Location**: `web/src/components/IdleTimeoutModal.tsx`

Provides user interface for the warning system:

- **Modern UI**: Positioned in top-right with backdrop blur
- **Countdown Display**: Shows remaining time in MM:SS format
- **User Actions**: "Continue" to extend session or "Logout" to end session
- **Keyboard Support**: ESC key continues the session
- **Event Prevention**: Prevents modal interactions from being detected as activity

### 2. Server-Side Components

#### Heartbeat Mechanism
**Location**: `src/TeamStride.Api/Controllers/AuthenticationController.cs`

The heartbeat endpoint validates ongoing user sessions:

**Endpoint**: `POST /api/authentication/heartbeat`

**Functionality**:
- Validates JWT token from Authorization header
- Checks user existence and active status
- Validates session fingerprint for security
- Updates user's last activity timestamp
- Returns 200 OK for valid sessions, 401 Unauthorized for invalid

**Request Structure**:
```csharp
public class HeartbeatRequestDto
{
    [Required]
    [MaxLength(10000)]
    public required string Fingerprint { get; set; }
}
```

#### Authentication Service
**Location**: `src/TeamStride.Infrastructure/Identity/AuthenticationService.cs`

The `ValidateHeartbeatAsync` method implements server-side session validation:

```csharp
public async Task<bool> ValidateHeartbeatAsync(Guid userId, string fingerprint)
```

**Validation Steps**:
1. Verify user exists and is active
2. Check if user has been force-logged out
3. Update last activity timestamp
4. Validate session fingerprint
5. Return validation result

#### Session Security Provider
**Location**: `web/src/components/SessionSecurityProvider.tsx`

Manages the heartbeat mechanism on the client side:

- **Heartbeat Interval**: 90 seconds
- **Failure Tolerance**: Maximum 5 missed heartbeats
- **Automatic Logout**: Triggers security violation logout after failures
- **Focus Validation**: Additional validation when window regains focus

## Configuration Settings

### Client-Side Timeouts
```typescript
const TIMEOUT_DURATION = 5 * 60 * 1000;  // 5 minutes
const WARNING_DURATION = 1 * 60 * 1000;  // 1 minute warning
```

### Server-Side Settings
```typescript
const HEARTBEAT_INTERVAL = 90 * 1000;    // 90 seconds
const MAX_MISSED_HEARTBEATS = 5;         // Maximum failures before logout
```

### JWT Token Expiration
**Location**: `src/TeamStride.Domain/Identity/AuthenticationConfiguration.cs`

JWT tokens have a configurable expiration time set via `JwtExpirationMinutes` in the authentication configuration.

### Refresh Token Expiration
Refresh tokens expire after 7 days from creation:
```csharp
ExpiresOn = DateTime.UtcNow.AddDays(7)
```

## Security Features

### Session Fingerprinting
Each session includes a unique fingerprint for additional security validation during heartbeat checks.

### Force Logout Capability
Administrators can force logout users through the `/api/authentication/force-logout/{userId}` endpoint, which:
- Sets a `ForceLogoutAfter` timestamp
- Revokes all refresh tokens
- Causes subsequent heartbeat validations to fail

### Multi-Tab Synchronization
The system monitors localStorage changes to handle logout events across multiple browser tabs.

### Focus-Based Validation
Additional security validation occurs when the browser window regains focus, checking:
- Token expiration
- Session fingerprint validity
- Immediate heartbeat validation

## User Experience Flow

### Normal Operation
1. User performs activities (mouse movement, clicks, keystrokes)
2. Activity resets the idle timer
3. Heartbeat requests maintain server-side session validity
4. No user interruption occurs

### Idle Timeout Sequence
1. User becomes inactive for 4 minutes (timeout - warning time)
2. Warning modal appears in top-right corner
3. Countdown timer shows remaining 1 minute
4. User can click "Continue" to extend session
5. If no action taken, automatic logout occurs
6. User is redirected to login page

### Security Violations
When security violations occur (heartbeat failures, fingerprint mismatches):
1. Immediate logout without warning modal
2. Session data cleared from localStorage
3. Hard page reload to ensure clean state

## Error Handling

### Network Failures
- Heartbeat failures are tracked and tolerated up to the maximum threshold
- Temporary network issues don't immediately trigger logout
- Focus validation errors are logged but don't force logout

### Token Expiration
- Expired JWT tokens trigger immediate logout
- Refresh token expiration requires re-authentication
- Server validates token expiration on every heartbeat

### Modal Interaction Prevention
- Modal clicks and keyboard interactions don't reset the idle timer
- Only genuine user activity outside the modal counts as activity
- Prevents accidental session extension through modal interaction

## Integration Points

### Root Layout Integration
The idle timeout system is integrated at the application root level:

```tsx
<SessionSecurityProvider>
  <IdleTimeoutProvider>
    {children}
  </IdleTimeoutProvider>
</SessionSecurityProvider>
```

### Authentication Integration
- Works seamlessly with JWT-based authentication
- Integrates with refresh token mechanism
- Supports external authentication providers

### Database Integration
- Tracks user activity through `LastActivityOn` field
- Supports force logout through `ForceLogoutAfter` field
- Maintains refresh token lifecycle

## Troubleshooting

### Common Issues

1. **Heartbeat Validation Failures**
   - Check JWT token validity
   - Verify session fingerprint consistency
   - Ensure user account is active

2. **Modal Not Appearing**
   - Verify user is on authenticated route
   - Check timeout configuration values
   - Ensure IdleTimeoutProvider is properly integrated

3. **Premature Logouts**
   - Review activity event detection
   - Check heartbeat failure tolerance
   - Verify network connectivity

### Debug Information
The system includes extensive logging for troubleshooting:
- Heartbeat validation steps
- JWT claim information
- Activity detection events
- Modal state changes

## Future Enhancements

### Potential Improvements
1. **Configurable Timeouts**: Allow per-user or per-role timeout configurations
2. **Activity Granularity**: Different timeout values for different types of activities
3. **Mobile Optimization**: Enhanced touch and gesture detection for mobile devices
4. **Analytics Integration**: Track idle timeout patterns for user experience insights
5. **Progressive Warnings**: Multiple warning stages before final logout

### Scalability Considerations
- Heartbeat mechanism scales with user base
- Database activity updates are lightweight
- Client-side timers have minimal resource impact
- Modal rendering is optimized for performance

This idle timeout implementation provides a robust, secure, and user-friendly session management system that balances security requirements with user experience considerations. 