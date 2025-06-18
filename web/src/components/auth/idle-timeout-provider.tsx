'use client';

import { ReactNode } from 'react';
import { usePathname } from 'next/navigation';
import IdleTimeout from './idle-timeout';

interface IdleTimeoutProviderProps {
  children: ReactNode;
}

// Pages that don't require idle timeout (public pages)
const PUBLIC_PAGES = [
  '/', 
  '/login',
  '/register',           // Site registration page
  '/join-team',          // Team joining page
  '/forgot-password',    // Password reset page
  '/reset-password'      // Password reset confirmation page
];

export default function IdleTimeoutProvider({ children }: IdleTimeoutProviderProps) {
  const pathname = usePathname();

  // Check if current page requires authentication
  // Also check for team-specific public pages (like team registration)
  const requiresAuth = !PUBLIC_PAGES.includes(pathname) && 
                       !pathname.endsWith('/register') && // Team-specific registration pages
                       !pathname.endsWith('/login');      // Team-specific login pages
  
  // console.log('IdleTimeoutProvider - pathname:', pathname, 'requiresAuth:', requiresAuth);

  return (
    <>
      {children}
      {requiresAuth && <IdleTimeout />}
    </>
  );
} 