'use client';

import { useEffect } from 'react';
import { useSearchParams, useRouter } from 'next/navigation';

export default function UrlTokenHandler() {
  const searchParams = useSearchParams();
  const router = useRouter();

  useEffect(() => {
    const token = searchParams.get('token');
    const refreshToken = searchParams.get('refreshToken');

    if (token && refreshToken) {
      // Store tokens in localStorage
      localStorage.setItem('token', token);
      localStorage.setItem('refreshToken', refreshToken);

      // Clean up URL by removing token parameters
      const url = new URL(window.location.href);
      url.searchParams.delete('token');
      url.searchParams.delete('refreshToken');
      
      // Replace the current URL without the token parameters
      // This prevents the tokens from being visible in the browser history
      window.history.replaceState(null, '', url.toString());
      
      console.log('[UrlTokenHandler] Tokens received and stored from URL parameters');
    }
  }, [searchParams, router]);

  return null; // This component doesn't render anything
} 