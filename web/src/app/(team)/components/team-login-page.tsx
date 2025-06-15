'use client';

import React, { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Image from 'next/image';
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
import { useAuth } from '@/contexts/auth-context';
import { getTeamThemeData } from '@/utils/team-theme';
import { SubdomainThemeDto } from '@/types/team';

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

interface TeamLoginPageProps {
  teamSubdomain: string;
}

export default function TeamLoginPage({
  teamSubdomain
}: TeamLoginPageProps) {
  const [loginForm, setLoginForm] = useState({
    email: '',
    password: ''
  });
  const [isLoading, setIsLoading] = useState(false);
  const [loginError, setLoginError] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [themeData, setThemeData] = useState<SubdomainThemeDto | null>(null);
  
  const router = useRouter();
  const { login, isAuthenticated } = useAuth();

  // Load theme data
  useEffect(() => {
    const loadTheme = async () => {
      try {
        const data = await getTeamThemeData(teamSubdomain);
        setThemeData(data);
        // Set document title with team name
        document.title = `${data.teamName || teamSubdomain} - Login`;
      } catch (error) {
        console.error('Failed to load theme data:', error);
        // Use default theme
        setThemeData({
          teamId: '',
          teamName: teamSubdomain.charAt(0).toUpperCase() + teamSubdomain.slice(1),
          subdomain: teamSubdomain,
          primaryColor: '#3B82F6',
          secondaryColor: '#F0FDF4',
          logoUrl: undefined
        });
        document.title = `${teamSubdomain} - Login`;
      }
    };

    if (teamSubdomain) {
      loadTheme();
    }
  }, [teamSubdomain]);

  useEffect(() => {
    // Check if user is already logged in
    if (isAuthenticated) {
      // User is already authenticated, redirect to team home
      router.push('/');
    }
  }, [isAuthenticated, router]);

  const handleLoginSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setLoginError('');

    try {
      const loginPayload = {
        email: loginForm.email,
        password: loginForm.password
      };

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

      // Use centralized login function
      login(authData.token, authData.refreshToken);

      // Navigate to team home
      router.push('/');
      
    } catch (error) {
      console.error('Login error:', error);
      setLoginError('Login failed. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleBackToHome = () => {
    router.push('/');
  };

  // Apply team theming with CSS custom properties
  const themeStyles = {
    '--team-primary': themeData?.primaryColor || '#10B981',
    '--team-secondary': themeData?.secondaryColor || '#D1FAE5',
    '--team-primary-hover': themeData?.primaryColor ? `${themeData.primaryColor}dd` : '#059669',
  } as React.CSSProperties;

  // Show loading state while theme data is loading
  if (!themeData) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return (
    <div 
      className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-indigo-50 flex flex-col justify-center py-12 sm:px-6 lg:px-8"
      style={themeStyles}
    >
      {/* Background decorative elements with team colors */}
      <div className="absolute inset-0 overflow-hidden pointer-events-none">
        <div 
          className="absolute top-20 left-10 w-72 h-72 rounded-full mix-blend-multiply filter blur-xl opacity-20 animate-pulse"
          style={{ backgroundColor: themeData?.primaryColor || '#3B82F6' }}
        ></div>
        <div 
          className="absolute top-40 right-10 w-72 h-72 rounded-full mix-blend-multiply filter blur-xl opacity-20 animate-pulse animation-delay-2000"
          style={{ backgroundColor: themeData?.secondaryColor || '#8B5CF6' }}
        ></div>
        <div 
          className="absolute -bottom-8 left-20 w-72 h-72 rounded-full mix-blend-multiply filter blur-xl opacity-20 animate-pulse animation-delay-4000"
          style={{ backgroundColor: themeData?.primaryColor ? `${themeData.primaryColor}80` : '#6366F1' }}
        ></div>
      </div>

      <div className="relative sm:mx-auto sm:w-full sm:max-w-md">
        <div className="flex justify-center">
          {themeData?.logoUrl ? (
            <div className="w-16 h-16 rounded-xl flex items-center justify-center shadow-lg overflow-hidden bg-white">
              <Image 
                src={themeData.logoUrl} 
                alt={`${themeData.teamName || teamSubdomain} logo`} 
                width={48}
                height={48}
                className="object-contain"
              />
            </div>
          ) : (
            <div 
              className="w-16 h-16 rounded-xl flex items-center justify-center shadow-lg"
              style={{ 
                background: `linear-gradient(to right, ${themeData?.primaryColor || '#3B82F6'}, ${themeData?.secondaryColor || '#8B5CF6'})` 
              }}
            >
              <span className="text-white font-bold text-2xl">
                {themeData?.teamName?.charAt(0).toUpperCase()}
              </span>
            </div>
          )}
        </div>
        <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
          {themeData?.teamName || teamSubdomain}
        </h2>
        <p className="mt-2 text-center text-sm text-gray-600">
          Sign in to access your team
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
                placeholder="athlete@example.com"
                className="w-full px-3 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:border-2 transition-colors"
                style={{
                  '--tw-ring-color': themeData?.primaryColor || '#3B82F6',
                  'focusBorderColor': themeData?.primaryColor || '#3B82F6'
                } as React.CSSProperties}
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
                  className="w-full px-3 py-3 pr-10 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:border-2 transition-colors"
                  style={{
                    '--tw-ring-color': themeData?.primaryColor || '#3B82F6',
                    'focusBorderColor': themeData?.primaryColor || '#3B82F6'
                  } as React.CSSProperties}
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
                <a 
                  href="#" 
                  className="font-medium hover:opacity-80 transition-opacity"
                  style={{ color: themeData?.primaryColor || '#3B82F6' }}
                >
                  Forgot your password?
                </a>
              </div>
            </div>
            
            <button 
              type="submit"
              disabled={isLoading}
              className="w-full flex justify-center py-3 px-4 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200 transform hover:scale-[1.02]"
              style={{
                background: `linear-gradient(to right, ${themeData?.primaryColor || '#3B82F6'}, ${themeData?.primaryColor ? `${themeData.primaryColor}dd` : '#2563EB'})`,
              }}
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
              ← Back to {themeData?.teamName || teamSubdomain}
            </button>
          </div>
        </div>

        <div className="mt-6 text-center space-y-2">
          <p className="text-xs text-gray-500">
            Not a member of {themeData?.teamName || teamSubdomain}?{' '}
            <a 
              href="/register" 
              className="font-medium hover:opacity-80 transition-opacity"
              style={{ color: themeData?.primaryColor || '#3B82F6' }}
            >
              Register to join
            </a>
          </p>
          <p className="text-xs text-gray-500">
            Looking for a different team?{' '}
            <a 
              href="/join-team" 
              className="font-medium hover:opacity-80 transition-opacity"
              style={{ color: themeData?.primaryColor || '#3B82F6' }}
            >
              Find your team
            </a>
          </p>
        </div>
      </div>
    </div>
  );
} 