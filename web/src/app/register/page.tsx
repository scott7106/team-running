import { headers } from 'next/headers';
import SiteRegisterPage from '../(www)/components/site-register-page';
import TeamRegisterPage from '../(team)/components/team-register-page';

export async function generateMetadata() {
  const headersList = await headers();
  const context = headersList.get('x-context');
  const teamSubdomain = headersList.get('x-team-subdomain') || '';
  
  switch(context) {
    case 'app':
      return {
        title: 'TeamStride - Register',
        description: 'Create your TeamStride account to get started.'
      };
    case 'team':
      return {
        title: `Join ${teamSubdomain} - Registration`,
        description: `Register to join the ${teamSubdomain} team.`
      };
    case 'www':
    default:
      return {
        title: 'TeamStride - Register',
        description: 'Create your TeamStride account to get started.'
      };
  }
}

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