'use client';

import { useEffect } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faExclamationTriangle, faClock } from '@fortawesome/free-solid-svg-icons';

interface IdleTimeoutModalProps {
  isOpen: boolean;
  remainingTime: number;
  onContinue: () => void;
  onLogout: () => void;
}

export default function IdleTimeoutModal({ 
  isOpen, 
  remainingTime, 
  onContinue, 
  onLogout 
}: IdleTimeoutModalProps) {
  // console.log('IdleTimeoutModal render - isOpen:', isOpen, 'remainingTime:', remainingTime);
  useEffect(() => {
    // Handle Escape key to continue session
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && isOpen) {
        onContinue();
      }
    };

    if (isOpen) {
      document.addEventListener('keydown', handleKeyDown);
    }

    return () => {
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [isOpen, onContinue]);

  if (!isOpen) return null;

  const minutes = Math.floor(remainingTime / 60);
  const seconds = remainingTime % 60;

  return (
    <>
      {/* Modern backdrop with subtle blur */}
      <div 
        className="fixed inset-0 bg-gray-900/20 backdrop-blur-sm z-50 flex items-start justify-end p-6"
        onClick={(e) => {
          // Prevent clicks from being detected as activity
          e.preventDefault();
          e.stopPropagation();
        }}
      >
        {/* Modern modal - positioned in top-right */}
        <div 
          className="bg-white/95 backdrop-blur-md rounded-2xl shadow-2xl max-w-sm w-full border border-white/20 transform animate-slide-in-right"
          onClick={(e) => {
            // Prevent clicks on modal content from being detected as activity
            e.preventDefault();
            e.stopPropagation();
          }}
        >
          {/* Header */}
          <div className="p-5 pb-3">
            <div className="flex items-center mb-3">
              <div className="flex items-center justify-center w-8 h-8 bg-amber-100 rounded-full mr-3">
                <FontAwesomeIcon 
                  icon={faExclamationTriangle} 
                  className="text-amber-600 text-sm" 
                />
              </div>
              <h3 className="text-lg font-semibold text-gray-900">
                Session Expiring
              </h3>
            </div>
            <p className="text-gray-600 text-sm leading-relaxed">
              Your session will expire due to inactivity
            </p>
          </div>

          {/* Content */}
          <div className="px-5 pb-3">
            <div className="bg-gradient-to-r from-red-50 to-orange-50 rounded-xl p-4 mb-4 border border-red-100">
              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-2">
                  <FontAwesomeIcon icon={faClock} className="text-red-500 text-sm" />
                  <span className="text-sm font-medium text-gray-700">Time remaining</span>
                </div>
                <span className="text-xl font-bold text-red-600 tabular-nums">
                  {minutes.toString().padStart(2, '0')}:
                  {seconds.toString().padStart(2, '0')}
                </span>
              </div>
            </div>

            <p className="text-sm text-gray-600 mb-4 leading-relaxed">
              Continue your session or you&apos;ll be automatically logged out.
            </p>
          </div>

          {/* Actions */}
          <div className="px-5 pb-5">
            <div className="flex space-x-2">
              <button
                onClick={(e) => {
                  e.preventDefault();
                  e.stopPropagation();
                  onContinue();
                }}
                className="flex-1 bg-blue-600 text-white px-3 py-2.5 rounded-xl text-sm font-semibold hover:bg-blue-700 transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 shadow-sm"
              >
                Continue
              </button>
              <button
                onClick={(e) => {
                  e.preventDefault();
                  e.stopPropagation();
                  onLogout();
                }}
                className="flex-1 bg-gray-100 text-gray-700 px-3 py-2.5 rounded-xl text-sm font-semibold hover:bg-gray-200 transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-gray-400 focus:ring-offset-2 border border-gray-200"
              >
                Logout
              </button>
            </div>
          </div>
        </div>
      </div>

      <style jsx>{`
        @keyframes slide-in-right {
          from {
            opacity: 0;
            transform: translateX(100%) translateY(-10px);
          }
          to {
            opacity: 1;
            transform: translateX(0) translateY(0);
          }
        }
        
        .animate-slide-in-right {
          animation: slide-in-right 0.3s cubic-bezier(0.16, 1, 0.3, 1);
        }
      `}</style>
    </>
  );
} 