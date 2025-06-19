import { headers } from 'next/headers';
import SiteLoginPage from '../(www)/components/site-login-page';
import AdminLoginPage from '../(app)/components/admin-login-page';
import TeamLoginPage from '../(team)/components/team-login-page';

export async function generateMetadata() {
  const headersList = await headers();
  const context = headersList.get('x-context');
  const teamSubdomain = headersList.get('x-team-subdomain') || '';
  
  switch(context) {
    case 'app':
      return {
        title: 'TeamStride Admin - Login',
        description: 'Sign in to access the TeamStride administration dashboard.'
      };
    case 'team':
      return {
        title: `${teamSubdomain} - Login`,
        description: `Sign in to access your ${teamSubdomain} team account.`
      };
    case 'www':
    default:
      return {
        title: 'TeamStride - Login',
        description: 'Sign in to your TeamStride account.'
      };
  }
}

export default async function LoginPage() {
  const headersList = await headers();
  const context = headersList.get('x-context');
  
  console.log('[LoginPage] Context detected:', context);
  
  switch(context) {
    case 'app':
      return <AdminLoginPage />;
    case 'team':
      // Team login will fetch theme data client-side
      const teamSubdomain = headersList.get('x-team-subdomain') || '';
      
      return (
        <TeamLoginPage 
          teamSubdomain={teamSubdomain}
        />
      );
    case 'www':
    default:
      return <SiteLoginPage />;
  }
} 