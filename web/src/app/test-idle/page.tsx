'use client';

import { useEffect, useState } from 'react';

export default function TestIdlePage() {
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  useEffect(() => {
    // Simulate authentication by setting a token
    localStorage.setItem('token', 'test-token-123');
    setIsAuthenticated(true);
  }, []);

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    window.location.href = '/';
  };

  if (!isAuthenticated) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-lg text-gray-600">Setting up test session...</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 p-8">
      <div className="max-w-4xl mx-auto">
        <div className="bg-white rounded-lg shadow-sm p-8">
          <h1 className="text-3xl font-bold text-gray-900 mb-6">
            Idle Timeout Test Page
          </h1>
          
          <div className="space-y-6">
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
              <h2 className="text-lg font-semibold text-blue-900 mb-2">
                How to Test the Idle Timeout:
              </h2>
              <ol className="list-decimal list-inside space-y-2 text-blue-800">
                <li>Stay on this page without moving your mouse or pressing any keys</li>
                <li>After 4 minutes of inactivity, you&apos;ll see a warning modal</li>
                <li>The modal will show a 1-minute countdown</li>
                <li>You can click &quot;Continue Session&quot; to extend your session</li>
                <li>If you don&apos;t respond, you&apos;ll be automatically logged out after 5 minutes total</li>
              </ol>
            </div>

            <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
              <h2 className="text-lg font-semibold text-yellow-900 mb-2">
                Current Status:
              </h2>
              <p className="text-yellow-800">
                ✅ You are authenticated and the idle timeout is active
              </p>
              <p className="text-yellow-800 mt-2">
                The timer resets whenever you:
              </p>
              <ul className="list-disc list-inside mt-2 space-y-1 text-yellow-700">
                <li>Move your mouse</li>
                <li>Click anywhere</li>
                <li>Press any key</li>
                <li>Scroll the page</li>
                <li>Touch the screen (mobile)</li>
              </ul>
            </div>

            <div className="bg-green-50 border border-green-200 rounded-lg p-4">
              <h2 className="text-lg font-semibold text-green-900 mb-2">
                Test Activities:
              </h2>
              <div className="space-y-3">
                <button 
                  className="bg-green-600 text-white px-4 py-2 rounded-lg hover:bg-green-700 transition-colors mr-3"
                  onClick={() => alert('Button clicked! Timer reset.')}
                >
                  Click to Reset Timer
                </button>
                <button 
                  className="bg-red-600 text-white px-4 py-2 rounded-lg hover:bg-red-700 transition-colors"
                  onClick={handleLogout}
                >
                  Manual Logout
                </button>
              </div>
            </div>

            <div className="bg-gray-50 border border-gray-200 rounded-lg p-4">
              <h2 className="text-lg font-semibold text-gray-900 mb-2">
                Implementation Details:
              </h2>
              <ul className="list-disc list-inside space-y-1 text-gray-700">
                <li><strong>Total timeout:</strong> 5 minutes of inactivity</li>
                <li><strong>Warning time:</strong> 1 minute before logout</li>
                <li><strong>Warning appears at:</strong> 4 minutes of inactivity</li>
                <li><strong>Auto-logout at:</strong> 5 minutes of inactivity</li>
                <li><strong>Activity detection:</strong> Mouse, keyboard, touch, scroll events</li>
                <li><strong>Scope:</strong> Only active on authenticated pages</li>
              </ul>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
} 