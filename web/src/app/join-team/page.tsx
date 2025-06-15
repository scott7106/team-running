'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { 
  faSearch,
  faExclamationTriangle,
  faTimes,
  faInfoCircle,
  faArrowLeft
} from '@fortawesome/free-solid-svg-icons';

export default function JoinTeamPage() {
  const [teamSubdomain, setTeamSubdomain] = useState('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const router = useRouter();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!teamSubdomain.trim()) {
      setError('Please enter a team name or subdomain');
      return;
    }

    setIsLoading(true);
    setError('');

    try {
      // Clean up the subdomain input (remove spaces, convert to lowercase)
      const cleanSubdomain = teamSubdomain.trim().toLowerCase();
      
      // Check if team exists by trying to navigate to it
      // We'll redirect to the team's registration page
      window.location.href = `https://${cleanSubdomain}.teamstride.com/register`;
    } catch (error) {
      console.error('Team lookup error:', error);
      setError('Unable to find that team. Please check the team name and try again.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleBackToHome = () => {
    router.push('/');
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        {/* Header */}
        <div>
          <div className="flex justify-center">
            <div className="w-12 h-12 bg-blue-600 rounded-lg flex items-center justify-center">
              <span className="text-white font-bold text-xl">T</span>
            </div>
          </div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            Join a Team
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            Enter your team&apos;s name or subdomain to join
          </p>
        </div>

        {/* Join Team Form */}
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
              <label htmlFor="teamSubdomain" className="block text-sm font-medium text-gray-700 mb-2">
                Team Name or Subdomain *
              </label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <FontAwesomeIcon icon={faSearch} className="h-5 w-5 text-gray-400" />
                </div>
                <input
                  id="teamSubdomain"
                  name="teamSubdomain"
                  type="text"
                  required
                  value={teamSubdomain}
                  onChange={(e) => setTeamSubdomain(e.target.value)}
                  placeholder="e.g., springfield-eagles, central-hs"
                  className="w-full pl-10 pr-3 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-colors"
                  disabled={isLoading}
                />
              </div>
              <p className="mt-2 text-xs text-gray-500">
                This will redirect you to: <span className="font-mono text-blue-600">{teamSubdomain || '[team]'}.teamstride.com</span>
              </p>
            </div>

            <div>
              <button
                type="submit"
                disabled={isLoading || !teamSubdomain.trim()}
                className="w-full flex justify-center py-3 px-4 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {isLoading ? (
                  <>
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                    Finding Team...
                  </>
                ) : (
                  'Go to Team Registration'
                )}
              </button>
            </div>
          </form>

          {/* Help Section */}
          <div className="mt-6 p-4 bg-blue-50 border border-blue-200 rounded-lg">
            <div className="flex items-start">
              <FontAwesomeIcon icon={faInfoCircle} className="text-blue-500 mt-0.5 mr-3 flex-shrink-0" />
              <div className="text-sm text-blue-700">
                <p className="font-medium mb-2">Need help finding your team?</p>
                <ul className="space-y-1 text-xs">
                  <li>• Ask your coach for the team subdomain</li>
                  <li>• Check team communications for the TeamStride link</li>
                  <li>• Look for emails from your team with registration information</li>
                </ul>
              </div>
            </div>
          </div>

          {/* Back to Home */}
          <div className="mt-6 text-center">
            <button
              onClick={handleBackToHome}
              className="text-sm text-gray-600 hover:text-gray-800 font-medium transition-colors flex items-center justify-center"
            >
              <FontAwesomeIcon icon={faArrowLeft} className="mr-2" />
              Back to Home
            </button>
          </div>
        </div>

        {/* Alternative Options */}
        <div className="text-center">
          <p className="text-sm text-gray-600">
                         Don&apos;t have a team yet?{' '}
            <button
              onClick={() => router.push('/register')}
              className="font-medium text-blue-600 hover:text-blue-500 transition-colors"
            >
              Create your own team
            </button>
          </p>
        </div>
      </div>
    </div>
  );
} 