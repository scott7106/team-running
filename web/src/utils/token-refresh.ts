'use client';

import { shouldRefreshTokenForSubdomain } from './auth';

interface RefreshResponse {
  token: string;
  refreshToken: string;
  email: string;
  firstName: string;
  lastName: string;
  teamId?: string;
  role?: string;
}

export async function refreshTokenForSubdomain(subdomain: string): Promise<boolean> {
  try {
    const token = localStorage.getItem('token');
    if (!token) {
      return false;
    }

    const response = await fetch('/api/authentication/refresh-context', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ subdomain }),
    });

    if (!response.ok) {
      console.error('Failed to refresh token for subdomain:', subdomain, response.status);
      return false;
    }

    const authData: RefreshResponse = await response.json();
    
    // Validate the new token before storing
    if (!authData.token || !authData.email) {
      console.error('Invalid token refresh response - missing required fields');
      return false;
    }
    
    // Update stored tokens
    localStorage.setItem('token', authData.token);
    localStorage.setItem('refreshToken', authData.refreshToken);
    
    console.log(`Token refreshed successfully for subdomain: ${subdomain}, user: ${authData.email}`);
    return true;
  } catch (error) {
    console.error('Error refreshing token for subdomain:', subdomain, error);
    return false;
  }
}

export function checkAndRefreshToken(): Promise<boolean> {
  if (typeof window === 'undefined') {
    return Promise.resolve(false);
  }

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

  if (shouldRefreshTokenForSubdomain(subdomain)) {
    return refreshTokenForSubdomain(subdomain);
  }

  return Promise.resolve(false);
} 