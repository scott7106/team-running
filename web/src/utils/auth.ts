import { decodeJwt } from 'jose';

export interface TeamMembershipInfo {
  teamId: string;
  teamSubdomain: string;
  teamRole: string;
  memberType: string;
}

export interface JwtClaims {
  sub: string; // user id
  email: string;
  first_name?: string;
  last_name?: string;
  is_global_admin?: string;
  team_memberships?: string; // JSON array of team memberships
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

export function parseTeamMemberships(claims: JwtClaims): TeamMembershipInfo[] {
  if (!claims.team_memberships) {
    return [];
  }

  try {
    const memberships = JSON.parse(claims.team_memberships);
    return Array.isArray(memberships) ? memberships.map((m: {
      TeamId?: string;
      teamId?: string;
      TeamSubdomain?: string;
      teamSubdomain?: string;
      TeamRole?: string | number;
      teamRole?: string | number;
      MemberType?: string | number;
      memberType?: string | number;
    }) => {
      // Convert numeric team role to string equivalent
      const rawTeamRole = m.TeamRole || m.teamRole;
      let teamRole = '';
      if (typeof rawTeamRole === 'number') {
        // Map numeric enum values to string names (based on C# enum)
        switch (rawTeamRole) {
          case 0: teamRole = 'TeamOwner'; break;
          case 1: teamRole = 'TeamAdmin'; break;
          case 2: teamRole = 'TeamMember'; break;
          default: teamRole = 'TeamMember'; break; // fallback
        }
      } else {
        teamRole = rawTeamRole || '';
      }

      // Convert numeric member type to string equivalent
      const rawMemberType = m.MemberType || m.memberType;
      let memberType = '';
      if (typeof rawMemberType === 'number') {
        // Map numeric enum values to string names (based on C# enum)
        switch (rawMemberType) {
          case 0: memberType = 'Coach'; break;
          case 1: memberType = 'Athlete'; break;
          case 2: memberType = 'Parent'; break;
          default: memberType = 'Coach'; break; // fallback
        }
      } else {
        memberType = rawMemberType || '';
      }

      return {
        teamId: m.TeamId || m.teamId || '',
        teamSubdomain: m.TeamSubdomain || m.teamSubdomain || '',
        teamRole,
        memberType
      };
    }) : [];
  } catch (error) {
    console.error('Failed to parse team memberships:', error);
    return [];
  }
}

export function getCurrentTeamMembership(currentSubdomain: string): TeamMembershipInfo | null {
  if (typeof window === 'undefined') return null;
  
  const token = localStorage.getItem('token');
  if (!token) return null;
  
  const claims = decodeToken(token);
  if (!claims) return null;
  
  const memberships = parseTeamMemberships(claims);
  return memberships.find(m => m.teamSubdomain.toLowerCase() === currentSubdomain.toLowerCase()) || null;
}

export function getUserFromToken(): { firstName: string; lastName: string; email: string } | null {
  if (typeof window === 'undefined') return null;
  
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

export function getTeamContextFromToken(): { 
  contextLabel: string; 
  isGlobalAdmin: boolean; 
  hasTeam: boolean; 
  teamId?: string;
  teamRole?: string;
  teamSubdomain?: string;
  allMemberships: TeamMembershipInfo[];
} | null {
  if (typeof window === 'undefined') return null;
  
  const token = localStorage.getItem('token');
  if (!token) return null;
  
  const claims = decodeToken(token);
  if (!claims) return null;
  
  const isGlobalAdmin = claims.is_global_admin === 'true';
  const allMemberships = parseTeamMemberships(claims);
  
  // Determine current subdomain context
  const hostname = window.location.hostname;
  let currentSubdomain = '';
  
  if (hostname.includes('localhost')) {
    const parts = hostname.split('.');
    if (parts.length > 1) {
      currentSubdomain = parts[0];
    } else {
      currentSubdomain = 'localhost';
    }
  } else {
    const parts = hostname.split('.');
    if (parts.length > 2) {
      currentSubdomain = parts[0];
    }
  }

  // Normalize subdomain for context checking
  if (currentSubdomain === 'localhost' || currentSubdomain === '') {
    currentSubdomain = 'www';
  }
  
  // Find current team membership based on subdomain
  const currentMembership = currentSubdomain && currentSubdomain !== 'www' && currentSubdomain !== 'app' ? 
    allMemberships.find(m => m.teamSubdomain.toLowerCase() === currentSubdomain.toLowerCase()) : 
    null;
  
  const hasTeam = !!currentMembership;
  const teamId = currentMembership?.teamId;
  const teamRole = currentMembership?.teamRole;
  const teamSubdomain = currentMembership?.teamSubdomain;
  
  let contextLabel: string;
  
  if (currentSubdomain === 'app' && isGlobalAdmin) {
    // Global admin in app context
    contextLabel = 'Global Admin';
  } else if (hasTeam && teamId) {
    // Show the team subdomain (code) if available, otherwise fall back to generic label
    contextLabel = teamSubdomain || 'Team Context';
  } else if (isGlobalAdmin && currentSubdomain === 'www') {
    contextLabel = 'Global Admin';
  } else {
    contextLabel = 'None';
  }
  
  return {
    contextLabel,
    isGlobalAdmin,
    hasTeam,
    teamId,
    teamRole,
    teamSubdomain,
    allMemberships
  };
}

export function canAccessSubdomain(subdomain: string): boolean {
  if (typeof window === 'undefined') return false;
  
  const token = localStorage.getItem('token');
  if (!token || isTokenExpired(token)) return false;
  
  const claims = decodeToken(token);
  if (!claims) return false;
  
  const isGlobalAdmin = claims.is_global_admin === 'true';
  
  // Global admins can access app subdomain
  if (subdomain === 'app') {
    return isGlobalAdmin;
  }
  
  // Anyone can access www/marketing subdomain when authenticated
  if (subdomain === 'www' || subdomain === 'localhost') {
    return true;
  }
  
  // For team subdomains, check if user has membership for this subdomain
  const memberships = parseTeamMemberships(claims);
  const hasTeamAccess = memberships.some(m => m.teamSubdomain.toLowerCase() === subdomain.toLowerCase());
  
  return hasTeamAccess;
}

export function shouldRefreshTokenForSubdomain(): boolean {
  if (typeof window === 'undefined') return false;
  
  const token = localStorage.getItem('token');
  if (!token || isTokenExpired(token)) return false;
  
  // Always return false - we don't auto-refresh tokens anymore
  // Users must use tenant-switcher for context changes
  return false;
}

export function isTokenExpired(token: string): boolean {
  const claims = decodeToken(token);
  if (!claims) return true;
  
  const now = Math.floor(Date.now() / 1000);
  return claims.exp < now;
}

// SESSION FINGERPRINTING
export function generateFingerprint(): string {
  if (typeof window === 'undefined') {
    return btoa(JSON.stringify({ userAgent: "server-side" }));
  }
  
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
  if (typeof window === 'undefined') return true;
  
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

// For security violations - immediate logout with optional message
export function logoutForSecurityViolation(reason: string): void {
  console.warn(`Security violation logout: ${reason}`);
  
  // Stop all security mechanisms
  stopHeartbeat();
  disableFocusValidation();
  
  // Clear session data
  localStorage.removeItem('token');
  localStorage.removeItem('refreshToken');
  localStorage.removeItem('sessionFingerprint');
  
  // Store logout reason for display on login page
  let message = 'Your session has been terminated for security reasons.';
  
  if (reason === 'heartbeat-rejected' || reason === 'Heartbeat validation failed') {
    message = 'You have been logged out because you signed in on another device. Only one active session is allowed at a time.';
  } else if (reason === 'fingerprint-mismatch') {
    message = 'Your session has been terminated due to a security check failure.';
  } else if (reason === 'token-expired') {
    message = 'Your session has expired. Please log in again.';
  }
  
  localStorage.setItem('logoutMessage', message);
  
  // Force a hard reload to ensure all state is cleared
  window.location.replace('/login');
}

// For idle timeout and manual logout - allows modal to show
export function logout(): void {
  // Stop all security mechanisms
  stopHeartbeat();
  disableFocusValidation();
  
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