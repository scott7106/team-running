'use client';

import { useEffect } from 'react';
import { useAuth } from '@/contexts/auth-context';

export function useAuthTokenRefresh() {
  // This hook no longer performs automatic token refresh
  // Users must use the tenant-switcher to change contexts
  
  useEffect(() => {
    // No-op - auto refresh is disabled
    // Users must use tenant-switcher for context changes
  }, []);
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