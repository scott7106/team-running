'use client';

import { useState, useEffect, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faExclamationTriangle, faCrown, faUsers } from '@fortawesome/free-solid-svg-icons';
import { useUser, useTenant } from '@/contexts/auth-context';
import { SubdomainThemeDto } from '@/types/team';

interface TeamOption {
  teamId: string;
  teamName: string;
  subdomain: string;
  teamRole: string;
  memberType: string;
  themeData?: SubdomainThemeDto;
}

export default function TenantSwitcherPage() {
  const router = useRouter();
  const [availableTeams, setAvailableTeams] = useState<TeamOption[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [showAdminChoice, setShowAdminChoice] = useState(false);
  const [isSwitching, setIsSwitching] = useState(false);
  
  // Use centralized auth state
  const { user, isAuthenticated } = useUser();
  const { isGlobalAdmin } = useTenant();

  const routeToAdminPage = useCallback(async () => {
    if (isSwitching) return;
    
    try {
      setIsSwitching(true);
      setError('');
      
      // Step 1: Get current token before clearing localStorage
      const currentToken = localStorage.getItem('token');
      if (!currentToken) {
        setError('No authentication token found. Please log in again.');
        setIsSwitching(false);
        return;
      }

      // Step 2: Get a new JWT token for global admin context using the current token
      const response = await fetch('/api/authentication/refresh-context', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${currentToken}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ subdomain: 'app' }),
      });

      if (!response.ok) {
        throw new Error('Failed to refresh token for admin context');
      }

      const authData = await response.json();
      const token = authData.token;
      const refreshToken = authData.refreshToken;
      
      // Step 3: Clear old authentication state only after successful refresh
      console.log('[TenantSwitcher] Clearing old tokens after successful refresh');
      localStorage.removeItem('token');
      localStorage.removeItem('refreshToken');
      localStorage.removeItem('sessionFingerprint');
      
      // Step 4: Route to admin subdomain with token as URL parameter
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
    } catch (error) {
      console.error('Error refreshing token for admin context:', error);
      setError('Failed to access admin area. Please try again.');
      setIsSwitching(false);
    }
  }, [isSwitching]);

  const switchToTeamContext = useCallback(async (team: TeamOption) => {
    console.log('[TenantSwitcher] switchToTeamContext called for:', team.subdomain);
    if (isSwitching) {
      console.log('[TenantSwitcher] Already switching, ignoring call');
      return;
    }
    
    try {
      setIsSwitching(true);
      setError('');
      
      // Step 1: Get current token before clearing localStorage
      const currentToken = localStorage.getItem('token');
      if (!currentToken) {
        setError('No authentication token found. Please log in again.');
        setIsSwitching(false);
        return;
      }

      // Step 2: Get a new JWT token for team context using the current token
      console.log('[TenantSwitcher] Getting new token for team context:', team.subdomain);
      const response = await fetch('/api/authentication/refresh-context', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${currentToken}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ subdomain: team.subdomain }),
      });

      if (!response.ok) {
        throw new Error('Failed to switch team context');
      }

      const authData = await response.json();
      const token = authData.token;
      const refreshToken = authData.refreshToken;
      
      // Step 3: Clear old authentication state only after successful refresh
      console.log('[TenantSwitcher] Clearing old tokens after successful refresh');
      localStorage.removeItem('token');
      localStorage.removeItem('refreshToken');
      localStorage.removeItem('sessionFingerprint');
      
      // Step 4: Route to team subdomain with token as URL parameters
      const protocol = window.location.protocol;
      const hostname = window.location.hostname;
      const port = window.location.port ? `:${window.location.port}` : '';
      
      const urlParams = new URLSearchParams({
        token: token,
        refreshToken: refreshToken
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
      setIsSwitching(false);
    }
  }, [isSwitching]);

  const fetchTeamThemeData = useCallback(async (teamIds: string[]) => {
    if (!teamIds.length) return [];
    
    try {
      const response = await fetch(`/api/tenant-switcher/themes?teamIds=${teamIds.join(',')}`);
      if (!response.ok) {
        console.error('Failed to fetch theme data:', response.statusText);
        return [];
      }
      
      const themeData: SubdomainThemeDto[] = await response.json();
      return themeData;
    } catch (error) {
      console.error('Error fetching theme data:', error);
      return [];
    }
  }, []);

  const loadTeamsFromToken = useCallback(async () => {
    try {
      setIsLoading(true);
      setError('');

      if (!user) {
        setError('User information not available');
        return;
      }

      // Convert team memberships to team options
      const teamOptions: TeamOption[] = user.teamMemberships.map(membership => ({
        teamId: membership.teamId,
        teamName: membership.teamSubdomain, // Placeholder until we get theme data
        subdomain: membership.teamSubdomain,
        teamRole: membership.teamRole,
        memberType: membership.memberType
      }));

      // Fetch theme data for all teams
      const teamIds = teamOptions.map(team => team.teamId);
      const themeDataArray = await fetchTeamThemeData(teamIds);
      
      // Merge theme data with team options
      const teamsWithThemeData = teamOptions.map(team => {
        const themeData = themeDataArray.find(theme => theme.teamId === team.teamId);
        return {
          ...team,
          teamName: themeData?.teamName || team.teamName,
          themeData
        };
      });

      setAvailableTeams(teamsWithThemeData);

      // Determine routing based on admin status and team count
      if (isGlobalAdmin && teamsWithThemeData.length > 0) {
        // Global admin with teams - show choice between admin and teams
        setShowAdminChoice(true);
      } else if (isGlobalAdmin && teamsWithThemeData.length === 0) {
        // Global admin with no teams - go directly to admin
        routeToAdminPage();
        return;
      } else if (teamsWithThemeData.length === 1) {
        // Non-admin with single team - go directly to team
        switchToTeamContext(teamsWithThemeData[0]);
        return;
      } else if (teamsWithThemeData.length === 0) {
        // No admin, no teams - show error
        setError('You do not have access to any teams. Please contact your administrator.');
      }
      // If multiple teams and not admin, show team selection (handled by render)

    } catch (error) {
      console.error('Error loading teams from token:', error);
      setError('Failed to load team information. Please try again.');
    } finally {
      setIsLoading(false);
    }
  }, [user, isGlobalAdmin, routeToAdminPage, switchToTeamContext, fetchTeamThemeData]);

  useEffect(() => {
    if (!isAuthenticated) {
      router.push('/login');
      return;
    }
    
    if (user) {
      loadTeamsFromToken();
    }
  }, [isAuthenticated, user, router, loadTeamsFromToken]);

  const handleAdminChoice = () => {
    routeToAdminPage();
  };

  const handleTeamChoice = (team: TeamOption) => {
    switchToTeamContext(team);
  };

  const handleBackToHome = () => {
    router.push('/');
  };

  // Helper function to get team button style
  const getTeamButtonStyle = (team: TeamOption) => {
    if (!team.themeData) {
      return {
        backgroundColor: '#f8fafc',
        borderColor: '#e2e8f0',
        color: '#1f2937'
      };
    }

    return {
      backgroundColor: team.themeData.secondaryColor,
      borderColor: team.themeData.primaryColor,
      color: '#1f2937',
      borderWidth: '2px'
    };
  };

  const getTeamButtonHoverStyle = (team: TeamOption) => {
    if (!team.themeData) {
      return {
        backgroundColor: '#f1f5f9',
        borderColor: '#cbd5e1'
      };
    }

    return {
      backgroundColor: team.themeData.primaryColor + '10', // 10% opacity
      borderColor: team.themeData.primaryColor,
      transform: 'scale(1.02)'
    };
  };

  if (isLoading || isSwitching) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-indigo-50 flex flex-col pt-16 sm:pt-20 lg:pt-24 py-12 sm:px-6 lg:px-8">
        <div className="sm:mx-auto sm:w-full sm:max-w-md">
          <div className="flex justify-center">
            <div className="w-12 h-12 bg-blue-600 rounded-lg flex items-center justify-center">
              <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-white"></div>
            </div>
          </div>
          <h2 className="mt-4 text-center text-3xl font-extrabold text-gray-900">
            {isSwitching ? 'Switching...' : 'Loading...'}
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            {isSwitching ? 'Switching to your selected context' : 'Checking your team access'}
          </p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-indigo-50 flex flex-col pt-16 sm:pt-20 lg:pt-24 py-12 sm:px-6 lg:px-8">
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
      <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-indigo-50 pt-12 sm:pt-16 lg:pt-20 pb-12 sm:px-6 lg:px-8">
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
            <div className="space-y-4">
              {/* Global Admin Option */}
              <button
                onClick={handleAdminChoice}
                className="w-full text-left p-4 rounded-lg transition-all duration-200 hover:shadow-lg transform hover:scale-[1.02] bg-gradient-to-r from-purple-500 to-indigo-600 text-white border-2 border-purple-500 hover:from-purple-600 hover:to-indigo-700"
              >
                <div className="flex items-center">
                  <FontAwesomeIcon icon={faCrown} className="w-5 h-5 mr-3 text-yellow-300" />
                  <div className="flex-1">
                    <div className="font-semibold text-lg">Global Administration</div>
                    <div className="text-sm text-purple-100">app.teamstride.net</div>
                  </div>
                </div>
              </button>
              
              {/* Divider with "Or" */}
              {availableTeams.length > 0 && (
                <div className="relative py-4">
                  <div className="absolute inset-0 flex items-center">
                    <div className="w-full border-t border-gray-200"></div>
                  </div>
                  <div className="relative flex justify-center text-sm">
                    <span className="px-3 bg-white text-gray-500 font-medium">Or</span>
                  </div>
                </div>
              )}
              
              {/* Team Options */}
              {availableTeams.map((team) => (
                <button
                  key={team.teamId}
                  onClick={() => handleTeamChoice(team)}
                  className="w-full text-left p-4 rounded-lg transition-all duration-200 hover:shadow-lg"
                  style={getTeamButtonStyle(team)}
                  onMouseEnter={(e) => {
                    const hoverStyles = getTeamButtonHoverStyle(team);
                    Object.assign(e.currentTarget.style, hoverStyles);
                  }}
                  onMouseLeave={(e) => {
                    const normalStyles = getTeamButtonStyle(team);
                    Object.assign(e.currentTarget.style, normalStyles);
                    e.currentTarget.style.transform = 'scale(1)';
                  }}
                >
                  <div className="flex items-center">
                    <FontAwesomeIcon icon={faUsers} className="w-5 h-5 mr-3 opacity-70" />
                    <div className="flex-1">
                      <div className="font-semibold text-lg">{team.teamName}</div>
                      <div className="text-sm opacity-75 mt-1">
                        {team.subdomain}.teamstride.net
                      </div>
                    </div>
                  </div>
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
    <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-indigo-50 pt-12 sm:pt-16 lg:pt-20 pb-12 sm:px-6 lg:px-8">
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
          <div className="space-y-4">
            {availableTeams.map((team) => (
              <button
                key={team.teamId}
                onClick={() => handleTeamChoice(team)}
                className="w-full text-left p-4 rounded-lg transition-all duration-200 hover:shadow-lg"
                style={getTeamButtonStyle(team)}
                onMouseEnter={(e) => {
                  const hoverStyles = getTeamButtonHoverStyle(team);
                  Object.assign(e.currentTarget.style, hoverStyles);
                }}
                onMouseLeave={(e) => {
                  const normalStyles = getTeamButtonStyle(team);
                  Object.assign(e.currentTarget.style, normalStyles);
                  e.currentTarget.style.transform = 'scale(1)';
                }}
              >
                <div className="flex items-center">
                  <FontAwesomeIcon icon={faUsers} className="w-5 h-5 mr-3 opacity-70" />
                  <div className="flex-1">
                    <div className="font-semibold text-lg">{team.teamName}</div>
                    <div className="text-sm opacity-75 mt-1">
                      {team.subdomain}.teamstride.net
                    </div>
                  </div>
                </div>
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