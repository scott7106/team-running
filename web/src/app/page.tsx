'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { 
  faUsers, 
  faCalendarDays, 
  faRunning, 
  faComments, 
  faTrophy, 
  faClock,
  faExclamationTriangle,
  faTimes
} from '@fortawesome/free-solid-svg-icons';
import { 
  faMicrosoft, 
  faGoogle 
} from '@fortawesome/free-brands-svg-icons';

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

export default function Home() {
  const [showLogin, setShowLogin] = useState(false);
  const [showTeamSelector, setShowTeamSelector] = useState(false);
  const [loginForm, setLoginForm] = useState({
    email: '',
    password: ''
  });
  const [isLoading, setIsLoading] = useState(false);
  const [loginError, setLoginError] = useState('');
  const [availableTeams, setAvailableTeams] = useState<TenantDto[]>([]);
  
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

  const handleLoginSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setLoginError('');

    try {
      const subdomain = getCurrentSubdomain();
      let teamId: string | undefined;

      // If we have a subdomain, try to get the team by subdomain
      if (subdomain) {
        try {
          const teamResponse = await fetch(`/api/teams/subdomain/${subdomain}`, {
            headers: {
              'Content-Type': 'application/json',
            },
          });
          
          if (teamResponse.ok) {
            const teamData = await teamResponse.json();
            teamId = teamData.id;
          }
        } catch (error) {
          console.warn('Could not fetch team by subdomain:', error);
        }
      }

      // Attempt login
      const loginResponse = await fetch('/api/authentication/login', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          email: loginForm.email,
          password: loginForm.password,
          teamId: teamId || null
        }),
      });

      if (!loginResponse.ok) {
        setLoginError('Invalid login credentials');
        return;
      }

      const authData: AuthResponse = await loginResponse.json();

      // Store tokens
      localStorage.setItem('token', authData.token);
      localStorage.setItem('refreshToken', authData.refreshToken);

      // Now handle the routing logic
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
        // 3. If user is global admin, route to admin page
        if (isGlobalAdmin) {
          routeToAdminPage();
          return;
        }

        // 4. Check number of teams user has access to
        const teamsResponse = await fetch('/api/tenant-switcher/tenants', {
          headers: {
            'Authorization': `Bearer ${authData.token}`,
            'Content-Type': 'application/json',
          },
        });

        if (teamsResponse.ok) {
          const teams: TenantDto[] = await teamsResponse.json();

          if (teams.length === 0) {
            setLoginError('You do not have access to any teams. Please contact your administrator.');
            return;
          }

          if (teams.length === 1) {
            // 4. User has access to only 1 team - route directly
            routeToTeamPage(teams[0].teamId, teams[0].subdomain);
            return;
          }

          // 5. User has access to multiple teams - show team selector
          setAvailableTeams(teams);
          setShowLogin(false);
          setShowTeamSelector(true);
          return;
        }
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

  const resetLoginState = () => {
    setShowLogin(false);
    setShowTeamSelector(false);
    setLoginForm({ email: '', password: '' });
    setLoginError('');
    setAvailableTeams([]);
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
      {/* Header */}
      <header className="bg-white shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center py-4">
            <div className="flex items-center">
              <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center">
                <span className="text-white font-bold text-lg">T</span>
              </div>
              <span className="ml-2 text-xl font-bold text-gray-900">TeamStride</span>
            </div>
            <button
              onClick={() => setShowLogin(!showLogin)}
              className="bg-blue-600 text-white px-4 py-2 rounded-lg font-medium hover:bg-blue-700 transition-colors"
            >
              Login
            </button>
          </div>
        </div>
      </header>

      {/* Login Modal */}
      {showLogin && (
        <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-lg p-6 w-full max-w-md">
            <div className="flex justify-between items-center mb-4">
              <h2 className="text-xl font-bold text-gray-900">Login to TeamStride</h2>
              <button
                onClick={resetLoginState}
                className="text-gray-400 hover:text-gray-600"
              >
                <FontAwesomeIcon icon={faTimes} />
              </button>
            </div>

            {/* Error Alert */}
            {loginError && (
              <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg flex items-start">
                <FontAwesomeIcon icon={faExclamationTriangle} className="text-red-500 mt-0.5 mr-2" />
                <div className="flex-1">
                  <p className="text-sm text-red-700">{loginError}</p>
                </div>
                <button
                  onClick={() => setLoginError('')}
                  className="text-red-400 hover:text-red-600 ml-2"
                >
                  <FontAwesomeIcon icon={faTimes} className="w-3 h-3" />
                </button>
              </div>
            )}
            
            <form onSubmit={handleLoginSubmit} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Email
                </label>
                <input
                  type="email"
                  required
                  value={loginForm.email}
                  onChange={(e) => setLoginForm(prev => ({ ...prev, email: e.target.value }))}
                  placeholder="coach@example.com"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 placeholder:text-gray-300 text-gray-900"
                  disabled={isLoading}
                />
              </div>
              
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Password
                </label>
                <input
                  type="password"
                  required
                  value={loginForm.password}
                  onChange={(e) => setLoginForm(prev => ({ ...prev, password: e.target.value }))}
                  placeholder="••••••••"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 placeholder:text-gray-300 text-gray-900"
                  disabled={isLoading}
                />
              </div>
              
              <button 
                type="submit"
                disabled={isLoading}
                className="w-full bg-blue-600 text-white py-2 rounded-lg font-medium hover:bg-blue-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {isLoading ? 'Signing In...' : 'Sign In'}
              </button>
            </form>
            
            <div className="text-center text-sm text-gray-600 mt-4">
              Or sign in with
            </div>
            
            <div className="grid grid-cols-2 gap-3 mt-3">
              <button 
                type="button"
                disabled={isLoading}
                className="flex items-center justify-center px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors text-gray-700 disabled:opacity-50"
              >
                <FontAwesomeIcon icon={faMicrosoft} className="w-4 h-4 mr-2 text-blue-600" />
                <span className="text-sm font-medium">Microsoft</span>
              </button>
              <button 
                type="button"
                disabled={isLoading}
                className="flex items-center justify-center px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors text-gray-700 disabled:opacity-50"
              >
                <FontAwesomeIcon icon={faGoogle} className="w-4 h-4 mr-2 text-red-500" />
                <span className="text-sm font-medium">Google</span>
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Team Selector Modal */}
      {showTeamSelector && (
        <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-lg p-6 w-full max-w-md">
            <div className="flex justify-between items-center mb-4">
              <h2 className="text-xl font-bold text-gray-900">Select Team</h2>
              <button
                onClick={resetLoginState}
                className="text-gray-400 hover:text-gray-600"
              >
                <FontAwesomeIcon icon={faTimes} />
              </button>
            </div>
            
            <p className="text-gray-600 mb-4">
              You have access to multiple teams. Please select which team you&apos;d like to access:
            </p>
            
            <div className="space-y-2 max-h-60 overflow-y-auto">
              {availableTeams.map((team) => (
                <button
                  key={team.teamId}
                  onClick={() => handleTeamSelection(team)}
                  className="w-full text-left p-3 border border-gray-200 rounded-lg hover:bg-gray-50 hover:border-gray-300 transition-colors"
                >
                  <div className="font-medium text-gray-900">{team.teamName}</div>
                  <div className="text-sm text-gray-500">{team.subdomain}.teamstride.com</div>
                </button>
              ))}
            </div>
          </div>
        </div>
      )}

      {/* Hero Section */}
      <section className="relative overflow-hidden">
        {/* Background Elements */}
        <div className="absolute inset-0 bg-gradient-to-br from-blue-50 via-indigo-50 to-purple-50"></div>
        <div className="absolute top-0 left-0 w-full h-full">
          <div className="absolute top-20 left-10 w-72 h-72 bg-blue-200 rounded-full mix-blend-multiply filter blur-xl opacity-30 animate-pulse"></div>
          <div className="absolute top-40 right-10 w-72 h-72 bg-purple-200 rounded-full mix-blend-multiply filter blur-xl opacity-30 animate-pulse animation-delay-2000"></div>
          <div className="absolute -bottom-8 left-20 w-72 h-72 bg-indigo-200 rounded-full mix-blend-multiply filter blur-xl opacity-30 animate-pulse animation-delay-4000"></div>
        </div>
        
        {/* Content */}
        <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-20 sm:py-32">
          <div className="text-center">
            {/* Main Headline */}
            <h1 className="text-5xl sm:text-6xl lg:text-7xl font-extrabold text-gray-900 mb-6 leading-tight">
              <span className="block animate-slide-up">Running Team</span>
              <span className="block bg-gradient-to-r from-blue-600 via-purple-600 to-indigo-600 bg-clip-text text-transparent animate-slide-up animation-delay-200">
                Management
              </span>
              <span className="block text-4xl sm:text-5xl lg:text-6xl mt-2 animate-slide-up animation-delay-400">
                Made Simple
              </span>
            </h1>
            
            {/* Subtitle */}
            <p className="text-xl sm:text-2xl text-gray-600 mb-10 max-w-4xl mx-auto leading-relaxed animate-fade-in animation-delay-600">
              Empower coaches to efficiently manage rosters, schedules, training plans, 
              communications, and more. Built mobile-first for coaches on the go.
            </p>
            
            {/* CTA Buttons */}
            <div className="flex flex-col sm:flex-row gap-4 justify-center mb-12 animate-fade-in animation-delay-800">
              <button className="group bg-gradient-to-r from-blue-600 to-indigo-600 text-white px-10 py-5 rounded-xl text-lg font-bold hover:from-blue-700 hover:to-indigo-700 transition-all duration-300 transform hover:scale-105 hover:shadow-xl">
                <span className="flex items-center justify-center">
                  Start Your Free Team
                  <svg className="w-5 h-5 ml-2 group-hover:translate-x-1 transition-transform" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 7l5 5m0 0l-5 5m5-5H6" />
                  </svg>
                </span>
              </button>
              <button className="group border-2 border-gray-300 text-gray-700 px-10 py-5 rounded-xl text-lg font-bold hover:border-blue-500 hover:text-blue-600 transition-all duration-300 transform hover:scale-105 hover:shadow-lg bg-white/80 backdrop-blur-sm">
                <span className="flex items-center justify-center">
                  <svg className="w-5 h-5 mr-2 group-hover:scale-110 transition-transform" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M14.828 14.828a4 4 0 01-5.656 0M9 10h1m4 0h1m-6 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  Watch Demo
                </span>
              </button>
            </div>
            
            {/* Social Proof */}
            <div className="flex flex-col sm:flex-row items-center justify-center gap-8 text-sm text-gray-500 animate-fade-in animation-delay-1000">
              <div className="flex items-center">
                <div className="flex -space-x-2 mr-3">
                  <div className="w-8 h-8 bg-gradient-to-r from-blue-400 to-blue-600 rounded-full border-2 border-white"></div>
                  <div className="w-8 h-8 bg-gradient-to-r from-purple-400 to-purple-600 rounded-full border-2 border-white"></div>
                  <div className="w-8 h-8 bg-gradient-to-r from-indigo-400 to-indigo-600 rounded-full border-2 border-white"></div>
                  <div className="w-8 h-8 bg-gradient-to-r from-green-400 to-green-600 rounded-full border-2 border-white"></div>
                </div>
                <span>Join 2,500+ coaches</span>
              </div>
              <div className="flex items-center">
                <span className="text-yellow-400 mr-1">★★★★★</span>
                <span>4.9/5 from 200+ reviews</span>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section className="bg-white py-16">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-12">
            <h2 className="text-3xl sm:text-4xl font-bold text-gray-900 mb-4">
              Everything You Need to Manage Your Team
            </h2>
            <p className="text-lg text-gray-600">
              From roster management to race results, TeamStride has you covered
            </p>
          </div>
          
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {/* Roster Management Card */}
            <div className="bg-white rounded-lg border border-gray-200 shadow-sm hover:shadow-md transition-shadow duration-300 p-6 text-center">
              <div className="w-12 h-12 bg-blue-100 rounded-lg flex items-center justify-center mx-auto mb-4">
                <FontAwesomeIcon icon={faUsers} className="text-blue-600 text-xl" />
              </div>
              <h3 className="text-xl font-semibold text-gray-900 mb-3">Roster Management</h3>
              <p className="text-gray-600 leading-relaxed">Add, edit, and organize athlete profiles with role assignments and contact information.</p>
            </div>
            
            {/* Smart Scheduling Card */}
            <div className="bg-white rounded-lg border border-gray-200 shadow-sm hover:shadow-md transition-shadow duration-300 p-6 text-center">
              <div className="w-12 h-12 bg-blue-100 rounded-lg flex items-center justify-center mx-auto mb-4">
                <FontAwesomeIcon icon={faCalendarDays} className="text-blue-600 text-xl" />
              </div>
              <h3 className="text-xl font-semibold text-gray-900 mb-3">Smart Scheduling</h3>
              <p className="text-gray-600 leading-relaxed">Manage practices and races with conflict detection and automated notifications.</p>
            </div>
            
            {/* Training Plans Card */}
            <div className="bg-white rounded-lg border border-gray-200 shadow-sm hover:shadow-md transition-shadow duration-300 p-6 text-center">
              <div className="w-12 h-12 bg-blue-100 rounded-lg flex items-center justify-center mx-auto mb-4">
                <FontAwesomeIcon icon={faRunning} className="text-blue-600 text-xl" />
              </div>
              <h3 className="text-xl font-semibold text-gray-900 mb-3">Training Plans</h3>
              <p className="text-gray-600 leading-relaxed">Create team-wide or individual training plans with goal tracking and progress monitoring.</p>
            </div>
            
            {/* Team Communication Card */}
            <div className="bg-white rounded-lg border border-gray-200 shadow-sm hover:shadow-md transition-shadow duration-300 p-6 text-center">
              <div className="w-12 h-12 bg-blue-100 rounded-lg flex items-center justify-center mx-auto mb-4">
                <FontAwesomeIcon icon={faComments} className="text-blue-600 text-xl" />
              </div>
              <h3 className="text-xl font-semibold text-gray-900 mb-3">Team Communication</h3>
              <p className="text-gray-600 leading-relaxed">Send messages via email and SMS to teams, groups, or individuals with read receipts.</p>
            </div>
            
            {/* Race Results Card */}
            <div className="bg-white rounded-lg border border-gray-200 shadow-sm hover:shadow-md transition-shadow duration-300 p-6 text-center">
              <div className="w-12 h-12 bg-blue-100 rounded-lg flex items-center justify-center mx-auto mb-4">
                <FontAwesomeIcon icon={faTrophy} className="text-blue-600 text-xl" />
              </div>
              <h3 className="text-xl font-semibold text-gray-900 mb-3">Race Results</h3>
              <p className="text-gray-600 leading-relaxed">Track race results, personal records, and team statistics with MileSplit integration.</p>
            </div>
            
            {/* Garmin Integration Card */}
            <div className="bg-white rounded-lg border border-gray-200 shadow-sm hover:shadow-md transition-shadow duration-300 p-6 text-center">
              <div className="w-12 h-12 bg-blue-100 rounded-lg flex items-center justify-center mx-auto mb-4">
                <FontAwesomeIcon icon={faClock} className="text-blue-600 text-xl" />
              </div>
              <h3 className="text-xl font-semibold text-gray-900 mb-3">Garmin Integration</h3>
              <p className="text-gray-600 leading-relaxed">Automatically sync workout data including mileage, pace, and heart rate from Garmin devices.</p>
            </div>
          </div>
        </div>
      </section>

      {/* Pricing Section */}
      <section className="py-16 bg-gray-50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-12">
            <h2 className="text-3xl sm:text-4xl font-bold text-gray-900 mb-4">
              Choose Your Plan
            </h2>
            <p className="text-lg text-gray-600">
              Start free and upgrade as your team grows
            </p>
          </div>
          
          <div className="grid grid-cols-1 md:grid-cols-3 gap-8 max-w-5xl mx-auto">
            {/* Free Plan */}
            <div className="bg-white rounded-lg shadow-lg p-8 border-2 border-gray-200 flex flex-col">
              <div className="text-center">
                <h3 className="text-2xl font-bold text-gray-900 mb-2">Free</h3>
                <div className="text-4xl font-bold text-gray-900 mb-4">$0<span className="text-lg text-gray-600">/month</span></div>
                <p className="text-gray-600 mb-6">Perfect for small teams getting started</p>
              </div>
              
              <ul className="space-y-3 mb-8 flex-grow">
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Up to 7 athletes</span>
                </li>
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Basic team setup</span>
                </li>
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Manual data entry</span>
                </li>
                <li className="flex items-center">
                  <span className="text-yellow-500 mr-3">⚠</span>
                  <span className="text-gray-700">Demo messaging only</span>
                </li>
              </ul>
              
              <button className="w-full bg-gray-600 text-white py-3 rounded-lg font-semibold hover:bg-gray-700 transition-colors mt-auto">
                Get Started Free
              </button>
            </div>
            
            {/* Standard Plan */}
            <div className="bg-white rounded-lg shadow-lg p-8 border-2 border-gray-200 flex flex-col">
              <div className="text-center">
                <h3 className="text-2xl font-bold text-gray-900 mb-2">Standard</h3>
                <div className="text-4xl font-bold text-gray-900 mb-4">$39<span className="text-lg text-gray-600">/year</span></div>
                <p className="text-gray-600 mb-6">Great for growing teams</p>
              </div>
              
              <ul className="space-y-3 mb-8 flex-grow">
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Up to 30 athletes</span>
                </li>
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Manual data entry</span>
                </li>
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Full messaging</span>
                </li>
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Basic reporting</span>
                </li>
              </ul>
              
              <button className="w-full bg-blue-600 text-white py-3 rounded-lg font-semibold hover:bg-blue-700 transition-colors mt-auto">
                Start Standard Plan
              </button>
            </div>
            
            {/* Premium Plan */}
            <div className="bg-white rounded-lg shadow-lg p-8 border-2 border-blue-500 relative flex flex-col">
              <div className="absolute -top-4 left-1/2 transform -translate-x-1/2">
                <span className="bg-blue-500 text-white px-4 py-1 rounded-full text-sm font-semibold">
                  Most Popular
                </span>
              </div>
              
              <div className="text-center">
                <h3 className="text-2xl font-bold text-gray-900 mb-2">Premium</h3>
                <div className="text-4xl font-bold text-gray-900 mb-4">$79<span className="text-lg text-gray-600">/year</span></div>
                <p className="text-gray-600 mb-6">Everything you need for serious teams</p>
              </div>
              
              <ul className="space-y-3 mb-8 flex-grow">
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Unlimited athletes</span>
                </li>
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Garmin & MileSplit sync</span>
                </li>
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Import/export data</span>
                </li>
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Advanced reporting</span>
                </li>
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Group training plans</span>
                </li>
              </ul>
              
              <button className="w-full bg-blue-600 text-white py-3 rounded-lg font-semibold hover:bg-blue-700 transition-colors mt-auto">
                Start Premium Plan
              </button>
            </div>
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="bg-blue-600 py-16">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h2 className="text-3xl sm:text-4xl font-bold text-white mb-4">
            Ready to Transform Your Team Management?
          </h2>
          <p className="text-xl text-blue-100 mb-8 max-w-2xl mx-auto">
            Join hundreds of coaches who have simplified their team management with TeamStride. 
            Start your free trial today - no credit card required.
          </p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <button className="bg-white text-blue-600 px-8 py-4 rounded-lg text-lg font-semibold hover:bg-gray-100 transition-colors">
              Start Your Free Team
            </button>
            <button className="border-2 border-white text-white px-8 py-4 rounded-lg text-lg font-semibold hover:bg-blue-700 transition-colors">
              Schedule a Demo
            </button>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="bg-gray-900 text-white py-12">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-1 md:grid-cols-4 gap-8">
            <div>
              <div className="flex items-center mb-4">
                <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center">
                  <span className="text-white font-bold text-lg">T</span>
                </div>
                <span className="ml-2 text-xl font-bold">TeamStride</span>
              </div>
              <p className="text-gray-400">
                Empowering coaches to manage running teams efficiently with modern, mobile-first tools.
              </p>
            </div>
            
            <div>
              <h4 className="text-lg font-semibold mb-4">Product</h4>
              <ul className="space-y-2 text-gray-400">
                <li><a href="#" className="hover:text-white transition-colors">Features</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Pricing</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Integrations</a></li>
                <li><a href="#" className="hover:text-white transition-colors">API</a></li>
              </ul>
            </div>
            
            <div>
              <h4 className="text-lg font-semibold mb-4">Support</h4>
              <ul className="space-y-2 text-gray-400">
                <li><a href="#" className="hover:text-white transition-colors">Help Center</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Contact Us</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Status</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Community</a></li>
              </ul>
            </div>
            
            <div>
              <h4 className="text-lg font-semibold mb-4">Legal</h4>
              <ul className="space-y-2 text-gray-400">
                <li><a href="#" className="hover:text-white transition-colors">Privacy Policy</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Terms of Service</a></li>
                <li><a href="#" className="hover:text-white transition-colors">COPPA Compliance</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Security</a></li>
              </ul>
            </div>
          </div>
          
          <div className="border-t border-gray-800 mt-8 pt-8 text-center text-gray-400">
            <p>&copy; 2024 TeamStride. All rights reserved.</p>
          </div>
        </div>
      </footer>
    </div>
  );
}
