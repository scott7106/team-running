'use client';

import { useEffect } from 'react';
import { useSearchParams, useRouter } from 'next/navigation';
import { useAuth } from '@/contexts/auth-context';

/**
 * URL Token Handler Component
 * 
 * This component handles cross-subdomain authentication by processing JWT tokens 
 * passed as URL parameters during tenant switching. It implements a "silent login"
 * flow after the user has been silently logged out from the source subdomain.
 * 
 * Flow:
 * 1. User selects tenant context in tenant-switcher
 * 2. Tenant-switcher performs silent logout (clears current subdomain's localStorage)
 * 3. Tenant-switcher gets new JWT token for target subdomain and redirects with tokens as URL params
 * 4. This component detects tokens in URL parameters
 * 5. Stores new tokens and establishes new authentication state (silent login)
 * 6. Cleans up URL to remove token parameters from browser history
 * 
 * Security considerations:
 * - Tokens are immediately removed from URL after processing
 * - Source subdomain authentication state was already cleared by tenant-switcher
 * - Each subdomain maintains isolated authentication context
 * 
 * @returns null - This is a utility component that doesn't render anything
 */
export default function UrlTokenHandler() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const { login, reloadAuthState } = useAuth();

  useEffect(() => {
    const token = searchParams.get('token');
    const refreshToken = searchParams.get('refreshToken');

    if (token && refreshToken) {
      console.log('[UrlTokenHandler] Cross-subdomain tokens detected - performing silent login');
      
      // Perform the cross-subdomain authentication flow
      const handleCrossSubdomainAuth = async () => {
        try {
          // Step 1: Clean up URL by removing token parameters first
          const url = new URL(window.location.href);
          url.searchParams.delete('token');
          url.searchParams.delete('refreshToken');
          
          // Replace the current URL without the token parameters
          // This prevents the tokens from being visible in the browser history
          window.history.replaceState(null, '', url.toString());
          
          // Step 2: Store new tokens in localStorage
          localStorage.setItem('token', token);
          localStorage.setItem('refreshToken', refreshToken);

          // Step 3: Small delay to ensure URL cleanup is complete
          await new Promise(resolve => setTimeout(resolve, 50));

          // Step 4: Silent login - use the AuthContext login method to establish new auth state
          login(token, refreshToken);
          console.log('[UrlTokenHandler] Silent login completed successfully');
          
        } catch (error) {
          console.error('[UrlTokenHandler] Error during cross-subdomain authentication:', error);
          // If login fails, force a reload of auth state to ensure consistent state
          reloadAuthState();
        }
      };

      // Execute the cross-subdomain authentication flow
      handleCrossSubdomainAuth();
    }
  }, [searchParams, router, login, reloadAuthState]);

  return null; // This component doesn't render anything
} 