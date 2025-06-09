import { headers } from 'next/headers';
import SiteLoginPage from '../(www)/components/site-login-page';
import AdminLoginPage from '../(app)/components/admin-login-page';
import TeamLoginPage from '../(team)/components/team-login-page';

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