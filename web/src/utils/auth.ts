import { decodeJwt } from 'jose';

export interface JwtClaims {
  sub: string; // user id
  email: string;
  first_name?: string;
  last_name?: string;
  is_global_admin?: string;
  team_id?: string;
  team_role?: string;
  member_type?: string;
  exp: number;
  iat: number;
  jti: string;
}

export function decodeToken(token: string): JwtClaims | null {
  try {
    return decodeJwt(token) as JwtClaims;
  } catch (error) {
    console.error('Failed to decode JWT token:', error);
    return null;
  }
}

export function getUserFromToken(): { firstName: string; lastName: string; email: string } | null {
  const token = localStorage.getItem('token');
  if (!token) return null;
  
  const claims = decodeToken(token);
  if (!claims) return null;
  
  return {
    firstName: claims.first_name || '',
    lastName: claims.last_name || '',
    email: claims.email || ''
  };
}

export function isTokenExpired(token: string): boolean {
  const claims = decodeToken(token);
  if (!claims) return true;
  
  const now = Math.floor(Date.now() / 1000);
  return claims.exp < now;
}

// SESSION FINGERPRINTING
export function generateFingerprint(): string {
  try {
    const fingerprint = {
      userAgent: navigator.userAgent.substring(0, 100),
      language: navigator.language,
      timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
      screen: `${screen.width}x${screen.height}`
    };
    
    return btoa(JSON.stringify(fingerprint));
  } catch (error) {
    console.warn('Error generating fingerprint:', error);
    // Fallback fingerprint
    return btoa(JSON.stringify({
      userAgent: "fallback"
    }));
  }
}

export function validateSessionFingerprint(): boolean {
  const storedFingerprint = localStorage.getItem('sessionFingerprint');
  if (!storedFingerprint) {
    console.warn('No stored session fingerprint found');
    return false;
  }
  
  try {
    const currentFingerprint = generateFingerprint();
    const isValid = storedFingerprint === currentFingerprint;
    
    if (!isValid) {
      console.warn('Session fingerprint mismatch detected');
      // Add debugging information to help troubleshoot
      try {
        const storedData = JSON.parse(atob(storedFingerprint));
        const currentData = JSON.parse(atob(currentFingerprint));
        console.warn('Stored fingerprint:', storedData);
        console.warn('Current fingerprint:', currentData);
      } catch {
        console.warn('Could not decode fingerprints for comparison');
      }
    }
    
    return isValid;
  } catch (error) {
    console.error('Error validating session fingerprint:', error);
    return false;
  }
}

export function setSessionFingerprint(): void {
  const fingerprint = generateFingerprint();
  localStorage.setItem('sessionFingerprint', fingerprint);
}

// HEARTBEAT MECHANISM
let heartbeatInterval: NodeJS.Timeout | null = null;
let missedHeartbeats = 0;
const MAX_MISSED_HEARTBEATS = 5;

export function startHeartbeat(): void {
  if (heartbeatInterval) {
    clearInterval(heartbeatInterval);
  }
  
  heartbeatInterval = setInterval(async () => {
    const token = localStorage.getItem('token');
    if (!token) {
      stopHeartbeat();
      return;
    }
    
    try {
      const response = await fetch('/api/authentication/heartbeat', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          fingerprint: generateFingerprint()
        })
      });
      
      if (!response.ok) {
        logoutForSecurityViolation('heartbeat-rejected');
        return;
      }
      
      // Reset missed heartbeats on success
      missedHeartbeats = 0;
      
    } catch {
      missedHeartbeats++;
      
      if (missedHeartbeats >= MAX_MISSED_HEARTBEATS) {
        logoutForSecurityViolation('heartbeat-timeout');
      }
    }
  }, 90000); // 1.5 minutes
}

export function stopHeartbeat(): void {
  if (heartbeatInterval) {
    clearInterval(heartbeatInterval);
    heartbeatInterval = null;
  }
  missedHeartbeats = 0;
}

// SESSION VALIDATION ON FOCUS
let focusListenerAttached = false;

const handleFocus = async () => {
  const token = localStorage.getItem('token');
  
  if (!token) {
    return; // Already logged out
  }
  
  // Check if token is expired
  if (isTokenExpired(token)) {
    logoutForSecurityViolation('token-expired');
    return;
  }
  
  // Validate fingerprint
  if (!validateSessionFingerprint()) {
    logoutForSecurityViolation('fingerprint-mismatch');
    return;
  }
  
  // Optional: Trigger an immediate heartbeat
  try {
    const response = await fetch('/api/authentication/heartbeat', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        fingerprint: generateFingerprint()
      })
    });
    
    if (!response.ok) {
      logoutForSecurityViolation('focus-heartbeat-failed');
    }
  } catch (error) {
    // Don't logout on network errors during focus validation
    console.warn('Focus heartbeat error:', error);
  }
};

export function enableFocusValidation(): void {
  if (focusListenerAttached) return;
  
  window.addEventListener('focus', handleFocus);
  focusListenerAttached = true;
}

export function disableFocusValidation(): void {
  if (focusListenerAttached) {
    window.removeEventListener('focus', handleFocus);
    focusListenerAttached = false;
  }
}

// LOGOUT FUNCTIONS

// For security violations - immediate logout without modal
export function logoutForSecurityViolation(reason: string): void {
  console.error(`Security violation logout: ${reason}`);
  
  // Clear session data
  localStorage.removeItem('token');
  localStorage.removeItem('refreshToken');
  localStorage.removeItem('sessionFingerprint');
  
  // Force a hard reload to ensure all state is cleared
  window.location.replace('/');
}

// For idle timeout and manual logout - allows modal to show
export function logout(): void {
  // Clear session data
  localStorage.removeItem('token');
  localStorage.removeItem('refreshToken');
  localStorage.removeItem('sessionFingerprint');
  
  // Force a hard reload to ensure all state is cleared
  window.location.replace('/');
}

// Session initialization function
export function initializeSession(): void {
  const token = localStorage.getItem('token');
  
  if (token && !isTokenExpired(token)) {
    // Set fingerprint if not already set
    if (!localStorage.getItem('sessionFingerprint')) {
      setSessionFingerprint();
    } else {
      // Only validate fingerprint if it was previously set
      const fingerprintValid = validateSessionFingerprint();
      if (!fingerprintValid) {
        console.warn('Session fingerprint validation failed during initialization');
      }
    }
    
    // Start security measures
    startHeartbeat();
    enableFocusValidation();
  }
}

// Enhanced login function (call this after successful login)
export function onLoginSuccess(): void {
  setSessionFingerprint();
  // NOTE: Heartbeat is now handled by SessionSecurityProvider to avoid conflicts
} 