'use client';

import React, { createContext, useContext, useReducer, useEffect, useCallback } from 'react';
import { decodeToken, isTokenExpired, setSessionFingerprint, stopHeartbeat, disableFocusValidation, canAccessSubdomain, parseTeamMemberships, TeamMembershipInfo } from '@/utils/auth';

// Types
interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isGlobalAdmin: boolean;
  teamMemberships: TeamMembershipInfo[];
}

interface Tenant {
  teamId?: string;
  teamRole?: string;
  teamSubdomain?: string;
  contextLabel: string;
}

interface AuthState {
  user: User | null;
  tenant: Tenant | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
  subdomainAccessDenied: boolean;
}

type AuthAction =
  | { type: 'SET_LOADING'; payload: boolean }
  | { type: 'SET_ERROR'; payload: string | null }
  | { type: 'LOGIN_SUCCESS'; payload: { user: User; tenant: Tenant } }
  | { type: 'UPDATE_TENANT'; payload: Tenant }
  | { type: 'LOGOUT' }
  | { type: 'CLEAR_TENANT' }
  | { type: 'SUBDOMAIN_ACCESS_DENIED'; payload: { user: User } };

interface AuthContextType extends AuthState {
  login: (token: string, refreshToken: string) => void;
  logout: () => void;
  refreshContext: (subdomain: string) => Promise<boolean>;
  clearTenantContext: () => void;
  reloadAuthState: () => void;
}

// Initial state
const initialState: AuthState = {
  user: null,
  tenant: null,
  isAuthenticated: false,
  isLoading: true,
  error: null,
  subdomainAccessDenied: false,
};

// Reducer
function authReducer(state: AuthState, action: AuthAction): AuthState {
  switch (action.type) {
    case 'SET_LOADING':
      return { ...state, isLoading: action.payload };
    case 'SET_ERROR':
      return { ...state, error: action.payload };
    case 'LOGIN_SUCCESS':
      return {
        ...state,
        user: action.payload.user,
        tenant: action.payload.tenant,
        isAuthenticated: true,
        isLoading: false,
        error: null,
        subdomainAccessDenied: false,
      };
    case 'UPDATE_TENANT':
      return {
        ...state,
        tenant: action.payload,
        subdomainAccessDenied: false,
      };
    case 'CLEAR_TENANT':
      return {
        ...state,
        tenant: {
          contextLabel: state.user?.isGlobalAdmin ? 'Global Admin' : 'No Team',
        },
        subdomainAccessDenied: false,
      };
    case 'SUBDOMAIN_ACCESS_DENIED':
      return {
        ...state,
        user: action.payload.user,
        tenant: null,
        isAuthenticated: true,
        isLoading: false,
        error: null,
        subdomainAccessDenied: true,
      };
    case 'LOGOUT':
      return {
        ...initialState,
        isLoading: false,
      };
    default:
      return state;
  }
}

// Create context
const AuthContext = createContext<AuthContextType | undefined>(undefined);

// Helper functions
function getCurrentSubdomain(): string {
  if (typeof window === 'undefined') return 'www';
  
  const hostname = window.location.hostname;
  let subdomain = '';
  
  if (hostname.includes('localhost')) {
    const parts = hostname.split('.');
    if (parts.length > 1) {
      subdomain = parts[0];
    } else {
      subdomain = 'localhost';
    }
  } else {
    const parts = hostname.split('.');
    if (parts.length > 2) {
      subdomain = parts[0];
    }
  }

  // Normalize subdomain for context checking
  if (subdomain === 'localhost' || subdomain === '') {
    subdomain = 'www';
  }
  
  return subdomain;
}

function parseUserFromToken(token: string): User | null {
  const claims = decodeToken(token);
  if (!claims) return null;

  const isGlobalAdmin = claims.is_global_admin === 'true';
  const teamMemberships = parseTeamMemberships(claims);
  
  return {
    id: claims.sub,
    email: claims.email,
    firstName: claims.first_name || '',
    lastName: claims.last_name || '',
    isGlobalAdmin,
    teamMemberships,
  };
}

function parseTokenDataWithSubdomainCheck(token: string): { 
  user: User; 
  tenant: Tenant | null; 
  hasSubdomainAccess: boolean;
  currentSubdomain: string;
} | null {
  const user = parseUserFromToken(token);
  if (!user) return null;

  // Determine current subdomain and team context
  const currentSubdomain = getCurrentSubdomain();
  
  // Check if user can access current subdomain
  const hasSubdomainAccess = canAccessSubdomain(currentSubdomain);
  
  if (!hasSubdomainAccess) {
    return { user, tenant: null, hasSubdomainAccess: false, currentSubdomain };
  }
  
  // Find current team membership based on subdomain
  const currentMembership = currentSubdomain && currentSubdomain !== 'www' && currentSubdomain !== 'app' ? 
    user.teamMemberships.find(m => m.teamSubdomain.toLowerCase() === currentSubdomain.toLowerCase()) : 
    null;

  let contextLabel: string;
  if (currentSubdomain === 'app' && user.isGlobalAdmin) {
    contextLabel = 'Global Admin';
  } else if (currentMembership) {
    contextLabel = currentMembership.teamSubdomain || 'Team Context';
  } else if (user.isGlobalAdmin && currentSubdomain === 'www') {
    contextLabel = 'Global Admin';
  } else {
    contextLabel = 'No Team';
  }

  const tenant: Tenant = {
    teamId: currentMembership?.teamId,
    teamRole: currentMembership?.teamRole,
    teamSubdomain: currentMembership?.teamSubdomain,
    contextLabel,
  };

  return { user, tenant, hasSubdomainAccess: true, currentSubdomain };
}



// Auth Provider Component
export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [state, dispatch] = useReducer(authReducer, initialState);

  // Load auth state from localStorage
  const loadAuthState = useCallback(() => {
    try {
      const token = localStorage.getItem('token');
      
      if (!token) {
        dispatch({ type: 'LOGOUT' });
        return;
      }

      if (isTokenExpired(token)) {
        // Clean up expired token
        localStorage.removeItem('token');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('sessionFingerprint');
        dispatch({ type: 'LOGOUT' });
        return;
      }

      const tokenData = parseTokenDataWithSubdomainCheck(token);
      if (!tokenData) {
        // Invalid token
        localStorage.removeItem('token');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('sessionFingerprint');
        dispatch({ type: 'LOGOUT' });
        return;
      }

      if (!tokenData.hasSubdomainAccess) {
        // User is authenticated but doesn't have access to current subdomain
        // Keep token for tenant switcher access
        dispatch({ type: 'SUBDOMAIN_ACCESS_DENIED', payload: { user: tokenData.user } });
        return;
      }

      dispatch({ type: 'LOGIN_SUCCESS', payload: { user: tokenData.user, tenant: tokenData.tenant! } });
    } catch (error) {
      console.error('Error loading auth state:', error);
      dispatch({ type: 'SET_ERROR', payload: 'Failed to load authentication state' });
      dispatch({ type: 'LOGOUT' });
    }
  }, []);

  // Login function
  const login = useCallback((token: string, refreshToken: string) => {
    try {
      localStorage.setItem('token', token);
      localStorage.setItem('refreshToken', refreshToken);
      setSessionFingerprint();

      const tokenData = parseTokenDataWithSubdomainCheck(token);
      if (!tokenData) {
        throw new Error('Invalid token data');
      }

      if (!tokenData.hasSubdomainAccess) {
        // User is authenticated but doesn't have access to current subdomain
        dispatch({ type: 'SUBDOMAIN_ACCESS_DENIED', payload: { user: tokenData.user } });
        console.log('[AuthContext] Login successful but subdomain access denied:', tokenData.user.email, 'subdomain:', tokenData.currentSubdomain);
        return;
      }

      dispatch({ type: 'LOGIN_SUCCESS', payload: { user: tokenData.user, tenant: tokenData.tenant! } });
      
      console.log('[AuthContext] Login successful:', tokenData.user.email);
    } catch (error) {
      console.error('Login error:', error);
      dispatch({ type: 'SET_ERROR', payload: 'Login failed' });
    }
  }, []);

  // Logout function
  const logout = useCallback(() => {
    // Stop security mechanisms
    stopHeartbeat();
    disableFocusValidation();
    
    // Clear storage
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('sessionFingerprint');
    
    // Update state
    dispatch({ type: 'LOGOUT' });
    
    console.log('[AuthContext] Logout completed');
    
    // Force reload to clear all state
    window.location.replace('/');
  }, []);

  // Refresh context for subdomain
  const refreshContext = useCallback(async (subdomain: string): Promise<boolean> => {
    try {
      const token = localStorage.getItem('token');
      if (!token) return false;

      const response = await fetch('/api/authentication/refresh-context', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ subdomain }),
      });

      if (!response.ok) {
        console.error('Failed to refresh context for subdomain:', subdomain);
        return false;
      }

      const authData = await response.json();
      
      if (!authData.token || !authData.email) {
        console.error('Invalid refresh response');
        return false;
      }

      // Update stored token
      localStorage.setItem('token', authData.token);
      localStorage.setItem('refreshToken', authData.refreshToken);

      // Parse new token and update state
      const tokenData = parseTokenDataWithSubdomainCheck(authData.token);
      if (!tokenData) {
        console.error('Failed to parse refreshed token');
        return false;
      }

      if (!tokenData.hasSubdomainAccess) {
        // User is authenticated but doesn't have access to subdomain after refresh
        dispatch({ type: 'SUBDOMAIN_ACCESS_DENIED', payload: { user: tokenData.user } });
        console.log('[AuthContext] Context refreshed but subdomain access denied:', authData.email, 'subdomain:', subdomain);
        return false;
      }

      dispatch({ type: 'LOGIN_SUCCESS', payload: { user: tokenData.user, tenant: tokenData.tenant! } });
      
      console.log('[AuthContext] Context refreshed for subdomain:', subdomain, 'user:', authData.email);
      return true;
    } catch (error) {
      console.error('Error refreshing context:', error);
      return false;
    }
  }, []);

  // Clear tenant context (for www subdomain)
  const clearTenantContext = useCallback(() => {
    if (!state.user) return;
    
    const tenant: Tenant = {
      contextLabel: state.user.isGlobalAdmin ? 'Global Admin' : 'No Team',
    };
    
    dispatch({ type: 'UPDATE_TENANT', payload: tenant });
  }, [state.user]);

  // Reload auth state (public method)
  const reloadAuthState = useCallback(() => {
    loadAuthState();
  }, [loadAuthState]);

  // Initial load
  useEffect(() => {
    loadAuthState();
  }, [loadAuthState]);

  const contextValue: AuthContextType = {
    ...state,
    login,
    logout,
    refreshContext,
    clearTenantContext,
    reloadAuthState,
  };

  return (
    <AuthContext.Provider value={contextValue}>
      {children}
    </AuthContext.Provider>
  );
}

// Hook to use auth context
export function useAuth(): AuthContextType {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}

// Hook to get user info
export function useUser() {
  const { user, isAuthenticated, isLoading, subdomainAccessDenied } = useAuth();
  return { user, isAuthenticated, isLoading, subdomainAccessDenied };
}

// Hook to get tenant info  
export function useTenant() {
  const { tenant, user, subdomainAccessDenied } = useAuth();
  return { 
    tenant, 
    isGlobalAdmin: user?.isGlobalAdmin || false,
    hasTeam: !!tenant?.teamId,
    teamMemberships: user?.teamMemberships || [],
    subdomainAccessDenied
  };
} 