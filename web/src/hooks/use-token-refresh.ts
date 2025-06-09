'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { checkAndRefreshToken } from '../utils/token-refresh';
import { isTokenExpired } from '../utils/auth';

export function useTokenRefresh() {
  const router = useRouter();

  useEffect(() => {
    const handleTokenRefresh = async () => {
      // Check if user has a token
      const token = localStorage.getItem('token');
      if (!token) {
        return;
      }

      // Check if token is expired - if so, redirect to login
      if (isTokenExpired(token)) {
        localStorage.removeItem('token');
        localStorage.removeItem('refreshToken');
        router.push('/login');
        return;
      }

      // Check if token needs refresh for current subdomain
      await checkAndRefreshToken();
    };

    // Run on component mount
    handleTokenRefresh();

    // Run when user navigates (focus returns to window)
    const handleFocus = () => {
      handleTokenRefresh();
    };

    window.addEventListener('focus', handleFocus);
    
    return () => {
      window.removeEventListener('focus', handleFocus);
    };
  }, [router]);
}

export function useTokenRefreshFromHeaders(needsRefresh?: string) {
  useEffect(() => {
    if (needsRefresh === 'true') {
      checkAndRefreshToken();
    }
  }, [needsRefresh]);
} 