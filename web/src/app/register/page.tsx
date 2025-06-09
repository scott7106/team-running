import { headers } from 'next/headers';
import SiteRegisterPage from '../(www)/components/site-register-page';
import TeamRegisterPage from '../(team)/components/team-register-page';

export default async function RegisterPage() {
  const headersList = await headers();
  const context = headersList.get('x-context');
  
  console.log('[RegisterPage] Context detected:', context);
  
  switch(context) {
    case 'app':
      // Admin registration could redirect to team creation or show different form
      return <SiteRegisterPage />;
    case 'team':
      // Team registration will fetch theme data client-side
      const teamSubdomain = headersList.get('x-team-subdomain') || '';
      
      return (
        <TeamRegisterPage 
          teamSubdomain={teamSubdomain}
        />
      );
    case 'www':
    default:
      return <SiteRegisterPage />;
  }
} 