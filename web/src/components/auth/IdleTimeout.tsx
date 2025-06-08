'use client';

import { useEffect, useState, useCallback, useMemo } from 'react';
import { useIdleTimeout } from '@/utils/useIdleTimeout';
import { logout } from '@/utils/auth';
import IdleTimeoutModal from './IdleTimeoutModal';

const TIMEOUT_DURATION = 5 * 60 * 1000; // 5 minutes in milliseconds
const WARNING_DURATION = 1 * 60 * 1000; // 1 minute warning in milliseconds

export default function IdleTimeout() {
  const [isAuthenticated, setIsAuthenticated] = useState(() => {
    // Initialize with current auth state to prevent unnecessary re-renders
    if (typeof window !== 'undefined') {
      return !!localStorage.getItem('token');
    }
    return false;
  });

  // Create stable callbacks that don't change on re-renders
  const handleIdle = useCallback(() => {
    // User has been idle for the full timeout period - logout
    logout();
  }, []);

  const handleWarning = useCallback(() => {
    // Showing idle timeout warning
  }, []);

  const handleActive = useCallback(() => {
    // User became active again
  }, []);

  // Create stable config object
  const config = useMemo(() => ({
    timeout: TIMEOUT_DURATION,
    warningTime: WARNING_DURATION,
    onIdle: handleIdle,
    onWarning: handleWarning,
    onActive: handleActive
  }), [handleIdle, handleWarning, handleActive]); // Include callbacks in dependencies

  const {
    showWarning,
    remainingTime,
    extend
  } = useIdleTimeout(config);

  useEffect(() => {
    // Check authentication status on mount and when localStorage changes
    const checkAuth = () => {
      const token = localStorage.getItem('token');
      const isAuth = !!token;
      setIsAuthenticated(prev => {
        if (prev !== isAuth) {
          return isAuth;
        }
        return prev;
      });
    };

    checkAuth();

    // Listen for storage changes (e.g., when user logs out in another tab)
    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === 'token') {
        checkAuth();
      }
    };

    window.addEventListener('storage', handleStorageChange);

    return () => {
      window.removeEventListener('storage', handleStorageChange);
    };
  }, []);

  const handleContinueSession = useCallback(() => {
    extend();
  }, [extend]);

  const handleLogoutNow = useCallback(() => {
    logout();
  }, []);

  // Focus validation is now handled by SessionSecurityProvider

  // Only render for authenticated users
  if (!isAuthenticated) {
    return null;
  }

  return (
    <IdleTimeoutModal
      isOpen={showWarning}
      remainingTime={remainingTime}
      onContinue={handleContinueSession}
      onLogout={handleLogoutNow}
    />
  );
} 