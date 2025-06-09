'use client';

import { ReactNode } from 'react';
import { usePathname } from 'next/navigation';
import IdleTimeout from './idle-timeout';

interface IdleTimeoutProviderProps {
  children: ReactNode;
}

// Pages that don't require idle timeout (public pages)
const PUBLIC_PAGES = ['/', '/login'];

export default function IdleTimeoutProvider({ children }: IdleTimeoutProviderProps) {
  const pathname = usePathname();

  // Check if current page requires authentication
  const requiresAuth = !PUBLIC_PAGES.includes(pathname);
  
  // console.log('IdleTimeoutProvider - pathname:', pathname, 'requiresAuth:', requiresAuth);

  return (
    <>
      {children}
      {requiresAuth && <IdleTimeout />}
    </>
  );
} 