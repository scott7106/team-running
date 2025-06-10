'use client';

import { useAuthTokenRefreshFromHeaders } from '@/hooks/use-auth-token-refresh';

interface TokenRefreshWrapperProps {
  needsRefresh?: string;
  children: React.ReactNode;
}

export default function TokenRefreshWrapper({ needsRefresh, children }: TokenRefreshWrapperProps) {
  // Handle token refresh based on middleware headers using AuthContext
  // Note: needsRefresh is no longer sent by middleware but kept for compatibility
  useAuthTokenRefreshFromHeaders(needsRefresh);
  
  return <>{children}</>;
} 