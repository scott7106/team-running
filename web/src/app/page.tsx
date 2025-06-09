import { headers } from 'next/headers';
import { Suspense } from 'react';
import SiteHomePage from './(www)/components/site-home-page';
import AdminHomePage from './(app)/components/admin-home-page';
import TeamHomePage from './(team)/components/team-home-page';
import TokenRefreshWrapper from '@/components/shared/token-refresh-wrapper';
import UrlTokenHandler from '@/components/shared/url-token-handler';
import UrlThemeHandler from '@/components/shared/url-theme-handler';

export default async function RootPage() {
  const headersList = await headers();
  const context = headersList.get('x-context');
  const needsRefresh = headersList.get('x-needs-token-refresh');
  
  console.log('[RootPage] Context detected:', context, 'Needs refresh:', needsRefresh);
  
  const pageContent = (() => {
    switch(context) {
      case 'app':
        return <AdminHomePage />;
      case 'team':
        return <TeamHomePage />;
      case 'www':
      default:
        return <SiteHomePage />;
    }
  })();

  return (
    <TokenRefreshWrapper needsRefresh={needsRefresh || undefined}>
      <Suspense fallback={null}>
        <UrlTokenHandler />
        <UrlThemeHandler />
      </Suspense>
      {pageContent}
    </TokenRefreshWrapper>
  );
} 