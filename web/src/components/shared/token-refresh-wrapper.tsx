'use client';

import { useTokenRefreshFromHeaders } from '@/hooks/use-token-refresh';

interface TokenRefreshWrapperProps {
  needsRefresh?: string;
  children: React.ReactNode;
}

export default function TokenRefreshWrapper({ needsRefresh, children }: TokenRefreshWrapperProps) {
  // Handle token refresh based on middleware headers
  useTokenRefreshFromHeaders(needsRefresh);
  
  return <>{children}</>;
} 