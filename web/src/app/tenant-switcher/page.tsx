'use client';

import { useState, useEffect, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faExclamationTriangle } from '@fortawesome/free-solid-svg-icons';
import { useUser, useTenant } from '@/contexts/auth-context';

interface TenantDto {
  teamId: string;
  teamName: string;
  subdomain: string;
  primaryColor: string;
  secondaryColor: string;
}

export default function TenantSwitcherPage() {
  const router = useRouter();
  const [availableTeams, setAvailableTeams] = useState<TenantDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [showAdminChoice, setShowAdminChoice] = useState(false);
  
  // Use centralized auth state
  const { isAuthenticated } = useUser();
  const { isGlobalAdmin } = useTenant();

  const routeToAdminPage = useCallback(async () => {
    try {
      // Get a new JWT token for global admin context
      const response = await fetch('/api/authentication/refresh-context', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ subdomain: 'app' }),
      });

      if (response.ok) {
        const authData = await response.json();
        const token = authData.token;
        const refreshToken = authData.refreshToken;
        
        // Route to admin subdomain with token as URL parameter
        const protocol = window.location.protocol;
        const hostname = window.location.hostname;
        const port = window.location.port ? `:${window.location.port}` : '';
        
        if (hostname.includes('localhost')) {
          // For development
          window.location.href = `${protocol}//app.localhost${port}/?token=${encodeURIComponent(token)}&refreshToken=${encodeURIComponent(refreshToken)}`;
        } else {
          // For production
          const baseDomain = hostname.split('.').slice(-2).join('.');
          window.location.href = `${protocol}//app.${baseDomain}${port}/?token=${encodeURIComponent(token)}&refreshToken=${encodeURIComponent(refreshToken)}`;
        }
      } else {
        throw new Error('Failed to refresh token for admin context');
      }
    } catch (error) {
      console.error('Error refreshing token for admin context:', error);
      setError('Failed to access admin area. Please try again.');
    }
  }, []);



  const switchToTeamContext = useCallback(async (team: TenantDto) => {
    try {
      // Call the tenant switcher API to get a new JWT with team context
      const response = await fetch(`/api/tenant-switcher/switch/${team.teamId}`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        throw new Error('Failed to switch team context');
      }

      const authData = await response.json();
      const token = authData.token;
      const refreshToken = authData.refreshToken;
      
      // Route to team subdomain with token and theme data as URL parameters
      const protocol = window.location.protocol;
      const hostname = window.location.hostname;
      const port = window.location.port ? `:${window.location.port}` : '';
      
      const urlParams = new URLSearchParams({
        token: token,
        refreshToken: refreshToken,
        primaryColor: team.primaryColor,
        secondaryColor: team.secondaryColor
      });
      
      if (hostname.includes('localhost')) {
        // For development
        window.location.href = `${protocol}//${team.subdomain}.localhost${port}/?${urlParams.toString()}`;
      } else {
        // For production
        const baseDomain = hostname.split('.').slice(-2).join('.');
        window.location.href = `${protocol}//${team.subdomain}.${baseDomain}${port}/?${urlParams.toString()}`;
      }
      
    } catch (error) {
      console.error('Error switching team context:', error);
      setError('Failed to switch to team context. Please try again.');
    }
  }, []);

  const loadTeamsAndAdminStatus = useCallback(async (token: string) => {
    try {
      setIsLoading(true);
      setError('');

      // Check if user is global admin from centralized auth context
      const isAdmin = isGlobalAdmin;

      // Get user's teams
      const teamsResponse = await fetch('/api/tenant-switcher/tenants', {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
      });

      if (!teamsResponse.ok) {
        throw new Error('Failed to load teams');
      }

      const userTeams: TenantDto[] = await teamsResponse.json();
      setAvailableTeams(userTeams);

      // Determine routing based on admin status and team count
      if (isAdmin && userTeams.length > 0) {
        // Global admin with teams - show choice between admin and teams
        setShowAdminChoice(true);
      } else if (isAdmin && userTeams.length === 0) {
        // Global admin with no teams - go directly to admin
        routeToAdminPage();
        return;
      } else if (userTeams.length === 1) {
        // Non-admin with single team - go directly to team
        switchToTeamContext(userTeams[0]);
        return;
      } else if (userTeams.length === 0) {
        // No admin, no teams - show error
        setError('You do not have access to any teams. Please contact your administrator.');
      }
      // If multiple teams and not admin, show team selection (handled by render)

    } catch (error) {
      console.error('Error loading teams:', error);
      setError('Failed to load team information. Please try again.');
    } finally {
      setIsLoading(false);
    }
  }, [isGlobalAdmin, routeToAdminPage, switchToTeamContext]);

  useEffect(() => {
    if (!isAuthenticated) {
      router.push('/login');
      return;
    }
    
    const token = localStorage.getItem('token');
    if (token) {
      loadTeamsAndAdminStatus(token);
    }
  }, [isAuthenticated, router, loadTeamsAndAdminStatus]);

  const handleAdminChoice = () => {
    routeToAdminPage();
  };

  const handleTeamChoice = (team: TenantDto) => {
    switchToTeamContext(team);
  };

  const handleBackToHome = () => {
    router.push('/');
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-indigo-50 flex flex-col justify-center py-12 sm:px-6 lg:px-8">
        <div className="sm:mx-auto sm:w-full sm:max-w-md">
          <div className="flex justify-center">
            <div className="w-12 h-12 bg-blue-600 rounded-lg flex items-center justify-center">
              <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-white"></div>
            </div>
          </div>
          <h2 className="mt-4 text-center text-3xl font-extrabold text-gray-900">
            Loading...
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            Checking your team access
          </p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-indigo-50 flex flex-col justify-center py-12 sm:px-6 lg:px-8">
        <div className="sm:mx-auto sm:w-full sm:max-w-md">
          <div className="flex justify-center">
            <div className="w-12 h-12 bg-red-600 rounded-lg flex items-center justify-center">
              <FontAwesomeIcon icon={faExclamationTriangle} className="text-white w-6 h-6" />
            </div>
          </div>
          <h2 className="mt-4 text-center text-3xl font-extrabold text-gray-900">
            Access Error
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">{error}</p>
          
          <div className="mt-6 text-center">
            <button
              onClick={handleBackToHome}
              className="text-blue-600 hover:text-blue-500 font-medium"
            >
              ← Back to Home
            </button>
          </div>
        </div>
      </div>
    );
  }

  if (showAdminChoice) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-indigo-50 flex flex-col justify-center py-12 sm:px-6 lg:px-8">
        <div className="sm:mx-auto sm:w-full sm:max-w-md">
          <div className="flex justify-center">
            <div className="w-12 h-12 bg-blue-600 rounded-lg flex items-center justify-center">
              <span className="text-white font-bold text-xl">T</span>
            </div>
          </div>
          <h2 className="mt-4 text-center text-3xl font-extrabold text-gray-900">
            Choose Your Access
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            You have global admin privileges
          </p>
        </div>

        <div className="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
          <div className="bg-white py-8 px-4 shadow-xl sm:rounded-xl sm:px-10 border border-gray-100">
            <div className="space-y-3 max-h-60 overflow-y-auto">
              <button
                onClick={handleAdminChoice}
                className="w-full text-left p-4 border border-gray-200 rounded-lg hover:bg-blue-50 hover:border-blue-300 transition-all duration-200 hover:shadow-md"
              >
                <div className="font-medium text-gray-900">Global Administration</div>
                <div className="text-sm text-gray-500">app.teamstride.net</div>
              </button>
              
              {availableTeams.map((team) => (
                <button
                  key={team.teamId}
                  onClick={() => handleTeamChoice(team)}
                  className="w-full text-left p-4 border border-gray-200 rounded-lg hover:bg-blue-50 hover:border-blue-300 transition-all duration-200 hover:shadow-md"
                >
                  <div className="font-medium text-gray-900">{team.teamName}</div>
                  <div className="text-sm text-gray-500">{team.subdomain}.teamstride.net</div>
                </button>
              ))}
            </div>
            
            <div className="mt-6 pt-6 border-t border-gray-200">
              <button
                onClick={handleBackToHome}
                className="w-full text-center text-sm text-gray-600 hover:text-gray-800"
              >
                ← Back to Home
              </button>
            </div>
          </div>
        </div>
      </div>
    );
  }

  // Show team selection for multiple teams
  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-indigo-50 flex flex-col justify-center py-12 sm:px-6 lg:px-8">
      <div className="sm:mx-auto sm:w-full sm:max-w-md">
        <div className="flex justify-center">
          <div className="w-12 h-12 bg-blue-600 rounded-lg flex items-center justify-center">
            <span className="text-white font-bold text-xl">T</span>
          </div>
        </div>
        <h2 className="mt-4 text-center text-3xl font-extrabold text-gray-900">
          Select Your Team
        </h2>
        <p className="mt-2 text-center text-sm text-gray-600">
          You have access to multiple teams
        </p>
      </div>

      <div className="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
        <div className="bg-white py-8 px-4 shadow-xl sm:rounded-xl sm:px-10 border border-gray-100">
          <div className="space-y-3 max-h-60 overflow-y-auto">
            {availableTeams.map((team) => (
              <button
                key={team.teamId}
                onClick={() => handleTeamChoice(team)}
                className="w-full text-left p-4 border border-gray-200 rounded-lg hover:bg-blue-50 hover:border-blue-300 transition-all duration-200 hover:shadow-md"
              >
                <div className="font-medium text-gray-900">{team.teamName}</div>
                <div className="text-sm text-gray-500">{team.subdomain}.teamstride.net</div>
              </button>
            ))}
          </div>
          
          <div className="mt-6 pt-6 border-t border-gray-200">
            <button
              onClick={handleBackToHome}
              className="w-full text-center text-sm text-gray-600 hover:text-gray-800"
            >
              ← Back to Home
            </button>
          </div>
        </div>
      </div>
    </div>
  );
} 