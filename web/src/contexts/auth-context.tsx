'use client';

import React, { createContext, useContext, useReducer, useEffect, useCallback } from 'react';
import { decodeToken, isTokenExpired, setSessionFingerprint, stopHeartbeat, disableFocusValidation } from '@/utils/auth';

// Types
interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isGlobalAdmin: boolean;
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
}

type AuthAction =
  | { type: 'SET_LOADING'; payload: boolean }
  | { type: 'SET_ERROR'; payload: string | null }
  | { type: 'LOGIN_SUCCESS'; payload: { user: User; tenant: Tenant } }
  | { type: 'UPDATE_TENANT'; payload: Tenant }
  | { type: 'LOGOUT' }
  | { type: 'CLEAR_TENANT' };

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
      };
    case 'UPDATE_TENANT':
      return {
        ...state,
        tenant: action.payload,
      };
    case 'CLEAR_TENANT':
      return {
        ...state,
        tenant: {
          contextLabel: state.user?.isGlobalAdmin ? 'Global Admin' : 'No Team',
        },
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
function parseTokenData(token: string): { user: User; tenant: Tenant } | null {
  const claims = decodeToken(token);
  if (!claims) return null;

  const user: User = {
    id: claims.sub,
    email: claims.email,
    firstName: claims.first_name || '',
    lastName: claims.last_name || '',
    isGlobalAdmin: claims.is_global_admin === 'true',
  };

  const hasTeam = !!claims.team_id;
  const teamSubdomain = claims.team_subdomain;

  let contextLabel: string;
  if (teamSubdomain === 'app' && user.isGlobalAdmin) {
    contextLabel = 'Global Admin';
  } else if (hasTeam && claims.team_id) {
    contextLabel = teamSubdomain || 'Team Context';
  } else if (user.isGlobalAdmin) {
    contextLabel = 'Global Admin';
  } else {
    contextLabel = 'No Team';
  }

  const tenant: Tenant = {
    teamId: claims.team_id,
    teamRole: claims.team_role,
    teamSubdomain: claims.team_subdomain,
    contextLabel,
  };

  return { user, tenant };
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

      const tokenData = parseTokenData(token);
      if (!tokenData) {
        // Invalid token
        localStorage.removeItem('token');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('sessionFingerprint');
        dispatch({ type: 'LOGOUT' });
        return;
      }

      dispatch({ type: 'LOGIN_SUCCESS', payload: tokenData });
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

      const tokenData = parseTokenData(token);
      if (!tokenData) {
        throw new Error('Invalid token data');
      }

      dispatch({ type: 'LOGIN_SUCCESS', payload: tokenData });
      
      // Emit custom event for cross-tab sync
      window.dispatchEvent(new CustomEvent('auth-login', { detail: tokenData }));
      
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
    
    // Emit custom event for cross-tab sync
    window.dispatchEvent(new CustomEvent('auth-logout'));
    
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
      const tokenData = parseTokenData(authData.token);
      if (!tokenData) {
        console.error('Failed to parse refreshed token');
        return false;
      }

      dispatch({ type: 'LOGIN_SUCCESS', payload: tokenData });
      
      // Emit event for cross-tab sync
      window.dispatchEvent(new CustomEvent('auth-context-refresh', { detail: tokenData }));
      
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

  // Initial load and event listeners
  useEffect(() => {
    loadAuthState();

    // Listen for storage changes (cross-tab sync)
    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === 'token') {
        loadAuthState();
      }
    };

    // Listen for custom auth events
    const handleAuthLogin = () => {
      console.log('[AuthContext] Auth login event received');
      loadAuthState();
    };

    const handleAuthLogout = () => {
      console.log('[AuthContext] Auth logout event received');
      dispatch({ type: 'LOGOUT' });
    };

    const handleContextRefresh = () => {
      console.log('[AuthContext] Context refresh event received');
      loadAuthState();
    };

    // Add event listeners
    window.addEventListener('storage', handleStorageChange);
    window.addEventListener('auth-login', handleAuthLogin as EventListener);
    window.addEventListener('auth-logout', handleAuthLogout);
    window.addEventListener('auth-context-refresh', handleContextRefresh as EventListener);

    return () => {
      window.removeEventListener('storage', handleStorageChange);
      window.removeEventListener('auth-login', handleAuthLogin as EventListener);
      window.removeEventListener('auth-logout', handleAuthLogout);
      window.removeEventListener('auth-context-refresh', handleContextRefresh as EventListener);
    };
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
  const { user, isAuthenticated, isLoading } = useAuth();
  return { user, isAuthenticated, isLoading };
}

// Hook to get tenant info
export function useTenant() {
  const { tenant, user } = useAuth();
  return { 
    tenant, 
    hasTeam: !!tenant?.teamId,
    isGlobalAdmin: user?.isGlobalAdmin || false,
  };
} 