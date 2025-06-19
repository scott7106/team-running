import { headers } from 'next/headers';
import { Suspense } from 'react';
import SiteHomePage from './(www)/components/site-home-page';
import AdminHomePage from './(app)/components/admin-home-page';
import TeamHomePage from './(team)/components/team-home-page';
import TokenRefreshWrapper from '@/components/shared/token-refresh-wrapper';
import UrlTokenHandler from '@/components/shared/url-token-handler';
import UrlThemeHandler from '@/components/shared/url-theme-handler';

export async function generateMetadata() {
  const headersList = await headers();
  const context = headersList.get('x-context');
  
  switch(context) {
    case 'app':
      return {
        title: 'TeamStride - Global Administration - Dashboard',
        description: 'Manage teams and users across the entire TeamStride platform.'
      };
    case 'team':
      return {
        title: 'TeamStride - Team Dashboard',
        description: 'Manage your team with TeamStride.'
      };
    case 'www':
    default:
      return {
        title: 'TeamStride - Running Team Management Made Simple',
        description: 'Empower coaches to efficiently manage running teams with rosters, schedules, training plans, communications, and more. Mobile-first team management platform.'
      };
  }
}

export default async function RootPage() {
  const headersList = await headers();
  const context = headersList.get('x-context');
  
  console.log('[RootPage] Context detected:', context);
  
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
    <TokenRefreshWrapper>
      <Suspense fallback={null}>
        <UrlTokenHandler />
        <UrlThemeHandler />
      </Suspense>
      {pageContent}
    </TokenRefreshWrapper>
  );
} 