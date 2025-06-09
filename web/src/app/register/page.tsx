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
      // Extract team data from headers for team registration
      const teamId = headersList.get('x-team-id') || '';
      const teamName = headersList.get('x-team-name') || 'Team';
      const primaryColor = headersList.get('x-team-primary-color') || '#10B981';
      const secondaryColor = headersList.get('x-team-secondary-color') || '#D1FAE5';
      const logoUrl = headersList.get('x-team-logo-url') || undefined;
      
      return (
        <TeamRegisterPage 
          teamId={teamId}
          teamName={teamName}
          primaryColor={primaryColor}
          secondaryColor={secondaryColor}
          logoUrl={logoUrl}
        />
      );
    case 'www':
    default:
      return <SiteRegisterPage />;
  }
} 