'use client';

import { useAuthTokenRefreshFromHeaders } from '@/hooks/use-auth-token-refresh';

interface TokenRefreshWrapperProps {
  needsRefresh?: string;
  children: React.ReactNode;
}

export default function TokenRefreshWrapper({ needsRefresh, children }: TokenRefreshWrapperProps) {
  // Handle token refresh based on middleware headers using AuthContext
  useAuthTokenRefreshFromHeaders(needsRefresh);
  
  return <>{children}</>;
} 