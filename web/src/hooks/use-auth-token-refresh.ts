'use client';

import { useEffect } from 'react';
import { useAuth } from '@/contexts/auth-context';
import { shouldRefreshTokenForSubdomain } from '@/utils/auth';

export function useAuthTokenRefresh() {
  const { refreshContext } = useAuth();

  useEffect(() => {
    if (typeof window === 'undefined') return;

    const handleTokenRefresh = async () => {
      // Extract subdomain from current hostname
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

      // Check if token needs refresh for current subdomain
      if (shouldRefreshTokenForSubdomain(subdomain)) {
        console.log('[useAuthTokenRefresh] Refreshing context for subdomain:', subdomain);
        await refreshContext(subdomain);
      }
    };

    // Run on component mount with a small delay
    const timeoutId = setTimeout(handleTokenRefresh, 50);

    // Run when user navigates (focus returns to window)
    const handleFocus = () => {
      handleTokenRefresh();
    };

    window.addEventListener('focus', handleFocus);
    
    return () => {
      clearTimeout(timeoutId);
      window.removeEventListener('focus', handleFocus);
    };
  }, [refreshContext]);
}

// Hook for handling token refresh from middleware headers
export function useAuthTokenRefreshFromHeaders(needsRefresh?: string) {
  const { refreshContext } = useAuth();

  useEffect(() => {
    if (needsRefresh === 'true') {
      const handleRefresh = async () => {
        // Extract subdomain from current hostname
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

        console.log('[useAuthTokenRefreshFromHeaders] Refreshing context for subdomain:', subdomain);
        await refreshContext(subdomain);
      };

      handleRefresh();
    }
  }, [needsRefresh, refreshContext]);
} 