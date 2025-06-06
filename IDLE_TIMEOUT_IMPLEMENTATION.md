# Idle Timeout Implementation

## Overview

The idle timeout feature automatically logs users out after 5 minutes of inactivity, with a warning modal appearing 1 minute before logout. This enhances security by ensuring that unattended sessions are terminated.

## Features

- **5-minute total timeout**: Users are logged out after 5 minutes of inactivity
- **1-minute warning**: A modal appears 4 minutes after inactivity starts
- **Real-time countdown**: The warning modal shows remaining time in MM:SS format
- **Activity detection**: Monitors mouse movement, clicks, keyboard input, scrolling, and touch events
- **Scope limitation**: Only active on authenticated pages (not on public pages like `/` and `/login`)
- **Cross-tab awareness**: Listens for storage changes to handle logout in other tabs

## Implementation Details

### Files Created/Modified

1. **`web/src/utils/useIdleTimeout.ts`** - Custom React hook for idle timeout logic
2. **`web/src/components/IdleTimeoutModal.tsx`** - Warning modal component
3. **`web/src/components/AuthenticatedIdleTimeout.tsx`** - Authentication-aware timeout wrapper
4. **`web/src/components/IdleTimeoutProvider.tsx`** - Provider component for route-based timeout
5. **`web/src/app/layout.tsx`** - Modified to include the IdleTimeoutProvider
6. **`web/src/app/test-idle/page.tsx`** - Test page for demonstrating the functionality

### Architecture

```
RootLayout
└── IdleTimeoutProvider (checks if page requires auth)
    └── AuthenticatedIdleTimeout (checks if user is authenticated)
        ├── useIdleTimeout hook (manages timers and activity detection)
        └── IdleTimeoutModal (displays warning and countdown)
```

### Activity Events Monitored

- `mousedown` - Mouse button press
- `mousemove` - Mouse movement
- `keypress` - Keyboard input
- `scroll` - Page scrolling
- `touchstart` - Touch screen interaction
- `click` - Mouse clicks

### Timer Logic

1. **Initial state**: 5-minute timer starts when user is on an authenticated page
2. **Activity detection**: Any monitored event resets the timer
3. **Warning phase**: At 4 minutes, warning modal appears with 1-minute countdown
4. **Logout**: At 5 minutes total, user is automatically logged out

## Configuration

The timeout durations are configurable in `AuthenticatedIdleTimeout.tsx`:

```typescript
const TIMEOUT_DURATION = 5 * 60 * 1000; // 5 minutes in milliseconds
const WARNING_DURATION = 1 * 60 * 1000; // 1 minute warning in milliseconds
```

## Testing

### Manual Testing

1. Navigate to `/test-idle` in your browser
2. The page will automatically set up a test authentication token
3. Stop all activity (don't move mouse, don't press keys)
4. After 4 minutes, the warning modal should appear
5. You can either:
   - Click "Continue Session" to extend the session
   - Click "Logout Now" to logout immediately
   - Wait 1 minute for automatic logout

### Automated Testing Considerations

For faster testing during development, you can temporarily modify the timeout values:

```typescript
// For testing - reduce to 30 seconds total, 10 seconds warning
const TIMEOUT_DURATION = 30 * 1000; // 30 seconds
const WARNING_DURATION = 10 * 1000; // 10 seconds warning
```

## Security Considerations

1. **Client-side only**: This implementation is client-side and should be complemented by server-side session management
2. **Token validation**: The system checks for the presence of a token but doesn't validate its expiration
3. **Cross-tab behavior**: Logout in one tab affects all tabs through localStorage events
4. **Activity throttling**: Activity detection is throttled to 1 second to prevent excessive timer resets

## Browser Compatibility

- **Modern browsers**: Full support for all features
- **Mobile devices**: Touch events are properly detected
- **Background tabs**: Timers continue running in background tabs
- **Storage events**: Cross-tab communication works in all modern browsers

## Customization Options

### Changing Timeout Duration

Modify the constants in `AuthenticatedIdleTimeout.tsx`:

```typescript
const TIMEOUT_DURATION = 10 * 60 * 1000; // 10 minutes
const WARNING_DURATION = 2 * 60 * 1000;  // 2 minutes warning
```

### Adding/Removing Activity Events

Modify the `ACTIVITY_EVENTS` array in `useIdleTimeout.ts`:

```typescript
const ACTIVITY_EVENTS = [
  'mousedown',
  'mousemove', 
  'keypress',
  'scroll',
  'touchstart',
  'click',
  'focus', // Add focus events
  'blur'   // Add blur events
] as const;
```

### Excluding Additional Pages

Add pages to the `PUBLIC_PAGES` array in `IdleTimeoutProvider.tsx`:

```typescript
const PUBLIC_PAGES = ['/', '/login', '/register', '/forgot-password'];
```

## Troubleshooting

### Modal Not Appearing

1. Check if you're on an authenticated page (not `/` or `/login`)
2. Verify that a token exists in localStorage
3. Ensure you've stopped all activity for the full timeout duration

### Timer Not Resetting

1. Check browser console for any JavaScript errors
2. Verify that activity events are being detected
3. Ensure the page hasn't lost focus (some browsers pause timers in background tabs)

### Cross-tab Issues

1. Verify that localStorage is working properly
2. Check that storage event listeners are properly attached
3. Ensure the same domain is being used across tabs

## Future Enhancements

1. **Server-side validation**: Integrate with backend session management
2. **Configurable per user**: Allow different timeout values for different user roles
3. **Activity logging**: Track user activity patterns for analytics
4. **Grace period**: Add a brief grace period after warning before logout
5. **Custom warning messages**: Allow customization of warning text based on user context 