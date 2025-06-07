'use client';

import { useState, useEffect, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { 
  faExclamationTriangle,
  faTimes,
  faEye,
  faEyeSlash
} from '@fortawesome/free-solid-svg-icons';
import { 
  faMicrosoft, 
  faGoogle 
} from '@fortawesome/free-brands-svg-icons';
import { isTokenExpired, onLoginSuccess } from '../../utils/auth';

interface AuthResponse {
  token: string;
  refreshToken: string;
  email: string;
  firstName: string;
  lastName: string;
  teamId?: string;
  role?: string;
  requiresEmailConfirmation: boolean;
}

interface TenantDto {
  teamId: string;
  teamName: string;
  subdomain: string;
  primaryColor: string;
  secondaryColor: string;
}

export default function LoginPage() {
  const [loginForm, setLoginForm] = useState({
    email: '',
    password: ''
  });
  const [isLoading, setIsLoading] = useState(false);
  const [loginError, setLoginError] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [availableTeams, setAvailableTeams] = useState<TenantDto[]>([]);
  const [showTeamSelector, setShowTeamSelector] = useState(false);
  const [showGlobalAdminSelector, setShowGlobalAdminSelector] = useState(false);
  
  const router = useRouter();

  // Get current subdomain info
  const getCurrentSubdomain = () => {
    if (typeof window === 'undefined') return null;
    
    const host = window.location.host;
    const hostParts = host.split('.');
    
    // Check if we have a subdomain (more than 2 parts or localhost with query param)
    if (host.includes('localhost') && new URLSearchParams(window.location.search).has('subdomain')) {
      return new URLSearchParams(window.location.search).get('subdomain');
    }
    
    if (hostParts.length > 2 && !host.startsWith('api.') && !host.startsWith('www.')) {
      return hostParts[0];
    }
    
    return null;
  };

  const handleAlreadyAuthenticatedUser = useCallback(async (token: string) => {
    try {
      // Create a mock auth response from the existing token
      const authData: AuthResponse = {
        token,
        refreshToken: localStorage.getItem('refreshToken') || '',
        email: '',
        firstName: '',
        lastName: '',
        requiresEmailConfirmation: false
      };

      const subdomain = getCurrentSubdomain();
      await handlePostLoginRouting(authData, subdomain);
    } catch (error) {
      console.error('Error routing already authenticated user:', error);
      // If routing fails, just go to home page as fallback
      router.push('/');
    }
  }, [router]); // eslint-disable-line react-hooks/exhaustive-deps

  // Set document title
  useEffect(() => {
    document.title = "TeamStride - Login";
  }, []);

  useEffect(() => {
    // Check if user is already logged in with a valid token
    const token = localStorage.getItem('token');
    if (token) {
      try {
        if (!isTokenExpired(token)) {
          // User is already authenticated, route them to appropriate page
          handleAlreadyAuthenticatedUser(token);
        } else {
          // Token is expired, remove it
          localStorage.removeItem('token');
          localStorage.removeItem('refreshToken');
        }
      } catch (error) {
        console.error('Error validating token:', error);
        // If validation fails, remove potentially invalid tokens
        localStorage.removeItem('token');
        localStorage.removeItem('refreshToken');
      }
    }
  }, [handleAlreadyAuthenticatedUser]);

  const handleLoginSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setLoginError('');

    try {
      const subdomain = getCurrentSubdomain();

      const loginPayload = {
        email: loginForm.email,
        password: loginForm.password
      };

      // Attempt login with ONLY email and password (no teamId)
      const loginResponse = await fetch('/api/authentication/login', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(loginPayload),
      });

      if (!loginResponse.ok) {
        const errorText = await loginResponse.text();
        
        let errorMessage = 'Invalid login credentials';
        try {
          const errorJson = JSON.parse(errorText);
          errorMessage = errorJson.detail || errorJson.message || errorMessage;
        } catch {
          // Could not parse error as JSON, use default message
        }
        
        setLoginError(errorMessage);
        return;
      }

      const authData: AuthResponse = await loginResponse.json();

      // Store tokens
      localStorage.setItem('token', authData.token);
      localStorage.setItem('refreshToken', authData.refreshToken);

      // Initialize session security
      onLoginSuccess();

      // Now handle the routing logic with the current subdomain context
      await handlePostLoginRouting(authData, subdomain);
      
    } catch (error) {
      console.error('Login error:', error);
      setLoginError('Login failed. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  const handlePostLoginRouting = async (authData: AuthResponse, currentSubdomain: string | null) => {
    try {
      // Check if user is global admin by making a test call to global admin endpoint
      let isGlobalAdmin = false;
      try {
        const adminResponse = await fetch('/api/admin/teams?pageSize=1', {
          headers: {
            'Authorization': `Bearer ${authData.token}`,
            'Content-Type': 'application/json',
          },
        });
        isGlobalAdmin = adminResponse.ok;
      } catch {
        // Not a global admin, continue with normal flow
      }

      // 1. If we have a team subdomain, verify access and route accordingly
      if (currentSubdomain && authData.teamId) {
        // User authenticated for specific team via subdomain
        routeToTeamPage(authData.teamId);
        return;
      }

      // 2. If no subdomain, check user permissions
      if (!currentSubdomain) {
        // Get user's team access
        const teamsResponse = await fetch('/api/tenant-switcher/tenants', {
          headers: {
            'Authorization': `Bearer ${authData.token}`,
            'Content-Type': 'application/json',
          },
        });

        let userTeams: TenantDto[] = [];
        if (teamsResponse.ok) {
          userTeams = await teamsResponse.json();
        }

        // 3. Handle global admin scenarios
        if (isGlobalAdmin) {
          if (userTeams.length === 0) {
            // Global admin with 0 teams - go directly to admin page
            routeToAdminPage();
            return;
          } else if (userTeams.length === 1) {
            // Global admin with 1 team - show choice between Host or Team
            setAvailableTeams(userTeams);
            setShowTeamSelector(false);
            setShowGlobalAdminSelector(true);
            return;
          } else {
            // Global admin with 2+ teams - show choice between Host or Teams
            setAvailableTeams(userTeams);
            setShowTeamSelector(false);
            setShowGlobalAdminSelector(true);
            return;
          }
        }

        // 4. Handle non-global admin scenarios
        if (userTeams.length === 0) {
          // User has no team access and is not global admin
          routeToNoTeamAccessPage();
          return;
        }

        if (userTeams.length === 1) {
          // User has access to only 1 team - route directly
          routeToTeamPage(userTeams[0].teamId, userTeams[0].subdomain);
          return;
        }

        // User has access to multiple teams - show team selector
        setAvailableTeams(userTeams);
        setShowGlobalAdminSelector(false);
        setShowTeamSelector(true);
        return;
      }

      // Fallback - something went wrong
      setLoginError('Unable to determine team access. Please try again.');
      
    } catch (error) {
      console.error('Post-login routing error:', error);
      setLoginError('Login succeeded but routing failed. Please try again.');
    }
  };

  const routeToAdminPage = () => {
    window.location.href = '/admin';
  };

  const routeToNoTeamAccessPage = () => {
    window.location.href = '/no-team-access';
  };

  const routeToTeamPage = (teamId: string, subdomain?: string) => {
    if (subdomain) {
      // Route to team subdomain
      const protocol = window.location.protocol;
      const hostname = window.location.hostname;
      const port = window.location.port ? `:${window.location.port}` : '';
      
      if (hostname.includes('localhost')) {
        // For development, use query parameter
        window.location.href = `${protocol}//${hostname}${port}/team?subdomain=${subdomain}`;
      } else {
        // For production, use subdomain
        const baseDomain = hostname.split('.').slice(-2).join('.');
        window.location.href = `${protocol}//${subdomain}.${baseDomain}${port}/team`;
      }
    } else {
      // Route to team page without subdomain
      router.push(`/team/${teamId}`);
    }
  };

  const handleTeamSelection = (team: TenantDto) => {
    routeToTeamPage(team.teamId, team.subdomain);
  };

  const handleGlobalAdminSelection = (choice: 'host' | TenantDto) => {
    if (choice === 'host') {
      routeToAdminPage();
    } else {
      routeToTeamPage(choice.teamId, choice.subdomain);
    }
  };

  const handleBackToHome = () => {
    router.push('/');
  };

  // Show team selection if needed
  if (showTeamSelector) {
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
                  onClick={() => handleTeamSelection(team)}
                  className="w-full text-left p-4 border border-gray-200 rounded-lg hover:bg-blue-50 hover:border-blue-300 transition-all duration-200 hover:shadow-md"
                >
                  <div className="font-medium text-gray-900">{team.teamName}</div>
                  <div className="text-sm text-gray-500">{team.subdomain}.teamstride.com</div>
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

  // Show global admin selection if needed
  if (showGlobalAdminSelector) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-indigo-50 flex flex-col justify-center py-12 sm:px-6 lg:px-8">
        <div className="sm:mx-auto sm:w-full sm:max-w-md">
          <div className="flex justify-center">
            <div className="w-12 h-12 bg-blue-600 rounded-lg flex items-center justify-center">
              <span className="text-white font-bold text-xl">T</span>
            </div>
          </div>
          <h2 className="mt-4 text-center text-3xl font-extrabold text-gray-900">
            Admin Access
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            Choose where you&apos;d like to go
          </p>
        </div>

        <div className="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
          <div className="bg-white py-8 px-4 shadow-xl sm:rounded-xl sm:px-10 border border-gray-100">
            <div className="space-y-3 max-h-60 overflow-y-auto">
              <button
                onClick={() => handleGlobalAdminSelection('host')}
                className="w-full text-left p-4 border border-gray-200 rounded-lg hover:bg-blue-50 hover:border-blue-300 transition-all duration-200 hover:shadow-md"
              >
                <div className="font-medium text-gray-900">Global Administration</div>
                <div className="text-sm text-gray-500">Manage all teams and users</div>
              </button>
              {availableTeams.map((team) => (
                <button
                  key={team.teamId}
                  onClick={() => handleGlobalAdminSelection(team)}
                  className="w-full text-left p-4 border border-gray-200 rounded-lg hover:bg-blue-50 hover:border-blue-300 transition-all duration-200 hover:shadow-md"
                >
                  <div className="font-medium text-gray-900">{team.teamName}</div>
                  <div className="text-sm text-gray-500">{team.subdomain}.teamstride.com</div>
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

  // Main login form
  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-indigo-50 flex flex-col justify-center py-12 sm:px-6 lg:px-8">
      {/* Background decorative elements */}
      <div className="absolute inset-0 overflow-hidden pointer-events-none">
        <div className="absolute top-20 left-10 w-72 h-72 bg-blue-200 rounded-full mix-blend-multiply filter blur-xl opacity-20 animate-pulse"></div>
        <div className="absolute top-40 right-10 w-72 h-72 bg-purple-200 rounded-full mix-blend-multiply filter blur-xl opacity-20 animate-pulse animation-delay-2000"></div>
        <div className="absolute -bottom-8 left-20 w-72 h-72 bg-indigo-200 rounded-full mix-blend-multiply filter blur-xl opacity-20 animate-pulse animation-delay-4000"></div>
      </div>

      <div className="relative sm:mx-auto sm:w-full sm:max-w-md">
        <div className="flex justify-center">
          <div className="w-16 h-16 bg-gradient-to-r from-blue-600 to-indigo-600 rounded-xl flex items-center justify-center shadow-lg">
            <span className="text-white font-bold text-2xl">T</span>
          </div>
        </div>
        <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
          Welcome to TeamStride
        </h2>
        <p className="mt-2 text-center text-sm text-gray-600">
          Sign in to your account to continue
        </p>
      </div>

      <div className="relative mt-8 sm:mx-auto sm:w-full sm:max-w-md">
        <div className="bg-white py-8 px-4 shadow-xl sm:rounded-xl sm:px-10 border border-gray-100">
          {/* Error Alert */}
          {loginError && (
            <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg">
              <div className="flex items-start">
                <FontAwesomeIcon icon={faExclamationTriangle} className="text-red-500 mt-0.5 mr-3 flex-shrink-0" />
                <div className="flex-1">
                  <p className="text-sm text-red-700">{loginError}</p>
                </div>
                <button
                  onClick={() => setLoginError('')}
                  className="text-red-400 hover:text-red-600 ml-2 flex-shrink-0"
                >
                  <FontAwesomeIcon icon={faTimes} className="w-4 h-4" />
                </button>
              </div>
            </div>
          )}
          
          <form onSubmit={handleLoginSubmit} className="space-y-6">
            <div>
              <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
                Email address
              </label>
              <input
                id="email"
                name="email"
                type="email"
                required
                autoComplete="email"
                value={loginForm.email}
                onChange={(e) => setLoginForm(prev => ({ ...prev, email: e.target.value }))}
                placeholder="coach@example.com"
                className="w-full px-3 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-colors"
                disabled={isLoading}
              />
            </div>
            
            <div>
              <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-1">
                Password
              </label>
              <div className="relative">
                <input
                  id="password"
                  name="password"
                  type={showPassword ? 'text' : 'password'}
                  required
                  autoComplete="current-password"
                  value={loginForm.password}
                  onChange={(e) => setLoginForm(prev => ({ ...prev, password: e.target.value }))}
                  placeholder="••••••••"
                  className="w-full px-3 py-3 pr-10 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-colors"
                  disabled={isLoading}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-400 hover:text-gray-600"
                  disabled={isLoading}
                >
                  <FontAwesomeIcon icon={showPassword ? faEyeSlash : faEye} className="w-5 h-5" />
                </button>
              </div>
            </div>

            <div className="flex items-center justify-between">
              <div className="text-sm">
                <a href="#" className="font-medium text-blue-600 hover:text-blue-500 transition-colors">
                  Forgot your password?
                </a>
              </div>
            </div>
            
            <button 
              type="submit"
              disabled={isLoading}
              className="w-full flex justify-center py-3 px-4 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-700 hover:to-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200 transform hover:scale-[1.02]"
            >
              {isLoading ? (
                <div className="flex items-center">
                  <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                  Signing in...
                </div>
              ) : (
                'Sign in'
              )}
            </button>
          </form>
          
          <div className="mt-6">
            <div className="relative">
              <div className="absolute inset-0 flex items-center">
                <div className="w-full border-t border-gray-300" />
              </div>
              <div className="relative flex justify-center text-sm">
                <span className="px-2 bg-white text-gray-500">Or continue with</span>
              </div>
            </div>
            
            <div className="mt-6 grid grid-cols-2 gap-3">
              <button 
                type="button"
                disabled={isLoading}
                className="w-full inline-flex justify-center py-3 px-4 border border-gray-300 rounded-lg shadow-sm bg-white text-sm font-medium text-gray-500 hover:bg-gray-50 hover:border-gray-400 disabled:opacity-50 transition-all duration-200"
              >
                <FontAwesomeIcon icon={faMicrosoft} className="w-5 h-5 text-blue-600" />
                <span className="ml-2">Microsoft</span>
              </button>
              <button 
                type="button"
                disabled={isLoading}
                className="w-full inline-flex justify-center py-3 px-4 border border-gray-300 rounded-lg shadow-sm bg-white text-sm font-medium text-gray-500 hover:bg-gray-50 hover:border-gray-400 disabled:opacity-50 transition-all duration-200"
              >
                <FontAwesomeIcon icon={faGoogle} className="w-5 h-5 text-red-500" />
                <span className="ml-2">Google</span>
              </button>
            </div>
          </div>

          <div className="mt-6 text-center">
            <button
              onClick={handleBackToHome}
              className="text-sm text-gray-600 hover:text-gray-800 font-medium transition-colors"
            >
              ← Back to Home
            </button>
          </div>
        </div>

        <div className="mt-6 text-center">
          <p className="text-xs text-gray-500">
            Don&apos;t have an account?{' '}
            <a href="#" className="text-blue-600 hover:text-blue-500 font-medium">
              Contact your team administrator
            </a>
          </p>
        </div>
      </div>
    </div>
  );
} 