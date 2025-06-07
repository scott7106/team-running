'use client';

import { useState, useEffect } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { 
  faExclamationTriangle,
  faSignOutAlt,
  faArrowLeft
} from '@fortawesome/free-solid-svg-icons';

export default function TeamAccessDeniedPage() {
  const [userEmail, setUserEmail] = useState<string>('');
  const [requestedTeam, setRequestedTeam] = useState<string>('');
  const router = useRouter();
  const searchParams = useSearchParams();

  useEffect(() => {
    // Check if user is authenticated
    const token = localStorage.getItem('token');
    if (!token) {
      router.push('/');
      return;
    }

    // Get the requested team from URL parameters
    const teamName = searchParams.get('team');
    if (teamName) {
      setRequestedTeam(teamName);
    }

    // Decode JWT token to get user email
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      setUserEmail(payload.email || payload.sub || 'Unknown');
    } catch (error) {
      console.error('Error decoding token:', error);
      setUserEmail('Unknown');
    }
  }, [router, searchParams]);

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    router.push('/');
  };

  const handleGoBack = () => {
    router.push('/');
  };



  return (
    <div className="min-h-screen bg-gray-50 flex flex-col justify-center py-12 sm:px-6 lg:px-8">
      <div className="sm:mx-auto sm:w-full sm:max-w-md">
        <div className="flex justify-center">
          <div className="w-12 h-12 bg-blue-600 rounded-lg flex items-center justify-center">
            <span className="text-white font-bold text-xl">T</span>
          </div>
        </div>
        <h2 className="mt-4 text-center text-3xl font-extrabold text-gray-900">
          TeamStride
        </h2>
      </div>

      <div className="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
        <div className="bg-white py-8 px-4 shadow sm:rounded-lg sm:px-10">
          {/* Warning icon and message */}
          <div className="text-center mb-6">
            <div className="mx-auto flex items-center justify-center h-16 w-16 rounded-full bg-red-100 mb-4">
              <FontAwesomeIcon icon={faExclamationTriangle} className="h-8 w-8 text-red-600" />
            </div>
            <h3 className="text-lg font-medium text-gray-900 mb-2">
              Access Denied
            </h3>
            <p className="text-sm text-gray-600">
              You do not have access to the requested team{requestedTeam ? `: "${requestedTeam}"` : ''}.
            </p>
          </div>

          {/* User info */}
          <div className="bg-gray-50 rounded-lg p-4 mb-6">
            <div>
              <p className="text-sm font-medium text-gray-900">Your Account</p>
              <p className="text-sm text-gray-600">{userEmail}</p>
            </div>
          </div>

          {/* Instructions */}
          <div className="mb-6">
            <h4 className="text-sm font-medium text-gray-900 mb-3">What to do next:</h4>
            <ul className="text-sm text-gray-600 space-y-2">
              <li className="flex items-start">
                <span className="font-bold text-red-600 mr-2">1.</span>
                <span>Contact the administrator or coach of {requestedTeam ? `"${requestedTeam}"` : 'the requested team'}</span>
              </li>
              <li className="flex items-start">
                <span className="font-bold text-red-600 mr-2">2.</span>
                <span>Ask them to add your email address to their team roster</span>
              </li>
              <li className="flex items-start">
                <span className="font-bold text-red-600 mr-2">3.</span>
                <span>Once added, you will be able to access this team</span>
              </li>
            </ul>
          </div>

          {/* Action buttons */}
          <div className="space-y-3">
            <button
              onClick={handleLogout}
              className="w-full flex justify-center items-center py-2 px-4 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
            >
              <FontAwesomeIcon icon={faSignOutAlt} className="h-4 w-4 mr-2" />
              Logout
            </button>
            
            <button
              onClick={handleGoBack}
              className="w-full flex justify-center items-center py-2 px-4 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
            >
              <FontAwesomeIcon icon={faArrowLeft} className="h-4 w-4 mr-2" />
              Back to Home
            </button>
          </div>


        </div>
      </div>
    </div>
  );
} 