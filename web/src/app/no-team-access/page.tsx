'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { 
  faExclamationTriangle,
  faEnvelope,
  faSignOutAlt,
  faArrowLeft
} from '@fortawesome/free-solid-svg-icons';

export default function NoTeamAccessPage() {
  const [userEmail, setUserEmail] = useState<string>('');
  const router = useRouter();

  useEffect(() => {
    // Check if user is authenticated
    const token = localStorage.getItem('token');
    if (!token) {
      router.push('/');
      return;
    }

    // TODO: Add API call to get user email
    // For now, we'll get it from localStorage or decode from token
    // This is a placeholder - in real implementation, decode from JWT or call API
    setUserEmail('user@example.com');
  }, [router]);

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    router.push('/');
  };

  const handleGoBack = () => {
    router.push('/');
  };

  const handleContactSupport = () => {
    window.location.href = 'mailto:support@teamstride.com?subject=Team Access Request&body=Hello, I need help accessing my team. My account email is: ' + userEmail;
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
            <div className="mx-auto flex items-center justify-center h-16 w-16 rounded-full bg-yellow-100 mb-4">
              <FontAwesomeIcon icon={faExclamationTriangle} className="h-8 w-8 text-yellow-600" />
            </div>
            <h3 className="text-lg font-medium text-gray-900 mb-2">
              No Team Access
            </h3>
            <p className="text-sm text-gray-600">
              Your account is not currently associated with any teams.
            </p>
          </div>

          {/* User info */}
          <div className="bg-gray-50 rounded-lg p-4 mb-6">
            <div className="flex items-center">
              <FontAwesomeIcon icon={faEnvelope} className="h-5 w-5 text-gray-400 mr-3" />
              <div>
                <p className="text-sm font-medium text-gray-900">Your Account</p>
                <p className="text-sm text-gray-600">{userEmail}</p>
              </div>
            </div>
          </div>

          {/* Instructions */}
          <div className="mb-6">
            <h4 className="text-sm font-medium text-gray-900 mb-3">What to do next:</h4>
            <ul className="text-sm text-gray-600 space-y-2">
              <li className="flex items-start">
                <span className="font-bold text-blue-600 mr-2">1.</span>
                <span>Contact your team administrator or coach</span>
              </li>
              <li className="flex items-start">
                <span className="font-bold text-blue-600 mr-2">2.</span>
                <span>Ask them to add your email address ({userEmail}) to the team roster</span>
              </li>
              <li className="flex items-start">
                <span className="font-bold text-blue-600 mr-2">3.</span>
                <span>Once added, log out and log back in to access your team</span>
              </li>
            </ul>
          </div>

          {/* Action buttons */}
          <div className="space-y-3">
            <button
              onClick={handleContactSupport}
              className="w-full flex justify-center items-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
            >
              <FontAwesomeIcon icon={faEnvelope} className="h-4 w-4 mr-2" />
              Contact Support
            </button>
            
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

          {/* Additional help */}
          <div className="mt-6 text-center">
            <p className="text-xs text-gray-500">
              Need immediate help? Contact{' '}
              <a href="mailto:support@teamstride.com" className="text-blue-600 hover:text-blue-500">
                support@teamstride.com
              </a>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
} 