'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Image from 'next/image';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { 
  faExclamationTriangle,
  faTimes,
  faEye,
  faEyeSlash
} from '@fortawesome/free-solid-svg-icons';
import { getTeamThemeData } from '@/utils/team-theme';
import { SubdomainThemeDto } from '@/types/team';

interface TeamRegisterPageProps {
  teamSubdomain: string;
}

export default function TeamRegisterPage({ 
  teamSubdomain
}: TeamRegisterPageProps) {
  const [registrationForm, setRegistrationForm] = useState({
    parentName: '',
    athleteName: '',
    email: '',
    teamPasscode: ''
  });
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const [showPasscode, setShowPasscode] = useState(false);
  const [themeData, setThemeData] = useState<SubdomainThemeDto | null>(null);
  const router = useRouter();

  // Load theme data
  useEffect(() => {
    const loadTheme = async () => {
      try {
        const data = await getTeamThemeData(teamSubdomain);
        setThemeData(data);
        // Set document title with team name
        document.title = `Join ${data.teamName || teamSubdomain} - Registration`;
      } catch (error) {
        console.error('Failed to load theme data:', error);
        // Use default theme
        setThemeData({
          teamId: '',
          teamName: teamSubdomain.charAt(0).toUpperCase() + teamSubdomain.slice(1),
          subdomain: teamSubdomain,
          primaryColor: '#10B981',
          secondaryColor: '#F0FDF4',
          logoUrl: undefined
        });
        document.title = `Join ${teamSubdomain} - Registration`;
      }
    };

    if (teamSubdomain) {
      loadTheme();
    }
  }, [teamSubdomain]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError('');

    try {
      // TODO: Implement team registration API call
      console.log('Registration form data:', { ...registrationForm, teamId: themeData?.teamId });
      
      // For now, just show success message
      alert('Registration request submitted! The team will review your request and contact you soon.');
      
      // Reset form
      setRegistrationForm({
        parentName: '',
        athleteName: '',
        email: '',
        teamPasscode: ''
      });
      
    } catch (error) {
      console.error('Registration error:', error);
      setError('Registration failed. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleBackToTeam = () => {
    router.push('/');
  };

  // Show loading state while theme data is loading
  if (!themeData) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  // Apply team theming
  const themeStyles = {
    '--team-primary': themeData?.primaryColor || '#10B981',
    '--team-secondary': themeData?.secondaryColor || '#D1FAE5',
    '--team-primary-bg': themeData?.secondaryColor || '#F0FDF4',
  } as React.CSSProperties;

  return (
    <div 
      className="min-h-screen flex flex-col justify-center py-12 sm:px-6 lg:px-8"
      style={{ 
        ...themeStyles,
        background: `linear-gradient(to br, ${themeData?.secondaryColor || '#F0FDF4'}, white, ${themeData?.primaryColor ? `${themeData.primaryColor}10` : '#E5F7F0'})`
      }}
    >
      <div className="sm:mx-auto sm:w-full sm:max-w-md">
        <div className="flex justify-center">
          {themeData?.logoUrl ? (
            <div className="w-16 h-16 rounded-xl flex items-center justify-center shadow-lg overflow-hidden bg-white">
              <Image 
                src={themeData.logoUrl} 
                alt={`${themeData.teamName || themeData.subdomain} logo`} 
                width={48}
                height={48}
                className="object-contain"
              />
            </div>
          ) : (
            <div 
              className="w-16 h-16 rounded-xl flex items-center justify-center shadow-lg"
              style={{ 
                background: `linear-gradient(to right, ${themeData?.primaryColor || '#10B981'}, ${themeData?.secondaryColor || '#059669'})` 
              }}
            >
              <span className="text-white font-bold text-2xl">
                {themeData?.teamName?.charAt(0).toUpperCase() || themeData?.subdomain.charAt(0).toUpperCase()}
              </span>
            </div>
          )}
        </div>
        <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
          Join {themeData?.teamName || themeData?.subdomain}
        </h2>
        <p className="mt-2 text-center text-sm text-gray-600">
          Request access to become a team member
        </p>
      </div>

      <div className="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
        <div className="bg-white py-8 px-4 shadow-xl sm:rounded-xl sm:px-10 border border-gray-100">
          {/* Error Alert */}
          {error && (
            <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg">
              <div className="flex items-start">
                <FontAwesomeIcon icon={faExclamationTriangle} className="text-red-500 mt-0.5 mr-3 flex-shrink-0" />
                <div className="flex-1">
                  <p className="text-sm text-red-700">{error}</p>
                </div>
                <button
                  onClick={() => setError('')}
                  className="text-red-400 hover:text-red-600 ml-2 flex-shrink-0"
                >
                  <FontAwesomeIcon icon={faTimes} className="w-4 h-4" />
                </button>
              </div>
            </div>
          )}
          
          <form onSubmit={handleSubmit} className="space-y-6">
            <div>
              <label htmlFor="parentName" className="block text-sm font-medium text-gray-700 mb-1">
                Parent/Guardian Name
              </label>
              <input
                id="parentName"
                name="parentName"
                type="text"
                required
                value={registrationForm.parentName}
                onChange={(e) => setRegistrationForm(prev => ({ ...prev, parentName: e.target.value }))}
                placeholder="Enter your full name"
                className="w-full px-3 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:border-2 transition-colors"
                style={{
                  '--tw-ring-color': themeData?.primaryColor || '#10B981',
                  'focusBorderColor': themeData?.primaryColor || '#10B981'
                } as React.CSSProperties}
                disabled={isLoading}
              />
            </div>
            
            <div>
              <label htmlFor="athleteName" className="block text-sm font-medium text-gray-700 mb-1">
                Athlete Name
              </label>
              <input
                id="athleteName"
                name="athleteName"
                type="text"
                required
                value={registrationForm.athleteName}
                onChange={(e) => setRegistrationForm(prev => ({ ...prev, athleteName: e.target.value }))}
                placeholder="Enter athlete's full name"
                className="w-full px-3 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:border-2 transition-colors"
                style={{
                  '--tw-ring-color': themeData?.primaryColor || '#10B981',
                  'focusBorderColor': themeData?.primaryColor || '#10B981'
                } as React.CSSProperties}
                disabled={isLoading}
              />
            </div>
            
            <div>
              <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
                Email Address
              </label>
              <input
                id="email"
                name="email"
                type="email"
                required
                value={registrationForm.email}
                onChange={(e) => setRegistrationForm(prev => ({ ...prev, email: e.target.value }))}
                placeholder="parent@example.com"
                className="w-full px-3 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:border-2 transition-colors"
                style={{
                  '--tw-ring-color': themeData?.primaryColor || '#10B981',
                  'focusBorderColor': themeData?.primaryColor || '#10B981'
                } as React.CSSProperties}
                disabled={isLoading}
              />
            </div>

            <div>
              <label htmlFor="teamPasscode" className="block text-sm font-medium text-gray-700 mb-1">
                Team Registration Passcode
              </label>
              <div className="relative">
                <input
                  id="teamPasscode"
                  name="teamPasscode"
                  type={showPasscode ? 'text' : 'password'}
                  required
                  value={registrationForm.teamPasscode}
                  onChange={(e) => setRegistrationForm(prev => ({ ...prev, teamPasscode: e.target.value }))}
                  placeholder="Enter team passcode"
                  className="w-full px-3 py-3 pr-10 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:border-2 transition-colors"
                  style={{
                    '--tw-ring-color': themeData?.primaryColor || '#10B981',
                    'focusBorderColor': themeData?.primaryColor || '#10B981'
                  } as React.CSSProperties}
                  disabled={isLoading}
                />
                <button
                  type="button"
                  onClick={() => setShowPasscode(!showPasscode)}
                  className="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-400 hover:text-gray-600"
                  disabled={isLoading}
                >
                  <FontAwesomeIcon icon={showPasscode ? faEyeSlash : faEye} className="w-5 h-5" />
                </button>
              </div>
              <p className="mt-1 text-xs text-gray-500">
                Contact your team coach or administrator for the registration passcode
              </p>
            </div>
            
            <button 
              type="submit"
              disabled={isLoading}
              className="w-full flex justify-center py-3 px-4 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white disabled:opacity-50 disabled:cursor-not-allowed transition-all duration-200 transform hover:scale-[1.02]"
              style={{
                background: `linear-gradient(to right, ${themeData?.primaryColor || '#10B981'}, ${themeData?.primaryColor ? `${themeData.primaryColor}dd` : '#059669'})`,
              }}
            >
              {isLoading ? (
                <div className="flex items-center">
                  <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                  Submitting Request...
                </div>
              ) : (
                `Request to Join ${themeData?.teamName || themeData?.subdomain}`
              )}
            </button>
          </form>

          <div className="mt-6 text-center">
            <button
              onClick={handleBackToTeam}
              className="text-sm text-gray-600 hover:text-gray-800 font-medium transition-colors"
            >
              ← Back to {themeData?.teamName || themeData?.subdomain}
            </button>
          </div>
        </div>

        <div className="mt-6 text-center">
          <p className="text-xs text-gray-500">
            Already a member?{' '}
            <a 
              href="/login" 
              className="font-medium hover:opacity-80 transition-opacity"
              style={{ color: themeData?.primaryColor || '#10B981' }}
            >
              Sign in here
            </a>
          </p>
        </div>
      </div>
    </div>
  );
} 