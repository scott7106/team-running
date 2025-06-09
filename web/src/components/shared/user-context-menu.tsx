'use client';

import { useState, useEffect, useRef } from 'react';
import { useRouter } from 'next/navigation';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { 
  faUser, 
  faChevronDown, 
  faSignOutAlt, 
  faExchangeAlt
} from '@fortawesome/free-solid-svg-icons';
import { useAuth, useUser, useTenant } from '@/contexts/auth-context';

interface UserContextMenuProps {
  /** Optional custom styling class */
  className?: string;
  /** Show as button (default) or inline menu */
  variant?: 'button' | 'inline';
}

export default function UserContextMenu({ className = '', variant = 'button' }: UserContextMenuProps) {
  const [isOpen, setIsOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const router = useRouter();
  
  // Use centralized auth state
  const { logout } = useAuth();
  const { user, isAuthenticated } = useUser();
  const { tenant, hasTeam, isGlobalAdmin } = useTenant();

  // No need for useEffect - auth state is managed by AuthContext

  useEffect(() => {
    // Close menu when clicking outside
    function handleClickOutside(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }

    document.addEventListener('mousedown', handleClickOutside);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, []);

  const handleSwitchTeams = () => {
    setIsOpen(false);
    // Navigate to tenant-switcher page
    router.push('/tenant-switcher');
  };

  const handleUserProfile = () => {
    setIsOpen(false);
    // TODO: Navigate to user profile page when implemented
    console.log('Navigate to user profile (not implemented yet)');
  };

  const handleLogout = () => {
    setIsOpen(false);
    logout();
  };

  if (!isAuthenticated || !user) {
    return null; // Don't show if user is not authenticated
  }

  const getUserInitials = () => {
    return `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase();
  };

  const getContextDisplay = () => {
    if (hasTeam && tenant) {
      return tenant.contextLabel;
    } else if (isGlobalAdmin) {
      return 'Global Admin';
    }
    return 'No Team';
  };

  if (variant === 'inline') {
    // Inline variant - show menu items directly without dropdown
    return (
      <div className={`space-y-1 ${className}`}>
        <button
          onClick={handleSwitchTeams}
          className="w-full flex items-center px-3 py-2 text-gray-300 hover:bg-gray-700 hover:text-white rounded-lg transition-colors text-left"
        >
          <FontAwesomeIcon icon={faExchangeAlt} className="w-4 h-4 mr-3" />
          <span>Switch Teams</span>
        </button>
        <button
          onClick={handleUserProfile}
          className="w-full flex items-center px-3 py-2 text-gray-300 hover:bg-gray-700 hover:text-white rounded-lg transition-colors text-left"
        >
          <FontAwesomeIcon icon={faUser} className="w-4 h-4 mr-3" />
          <span>User Profile</span>
        </button>
        <button
          onClick={handleLogout}
          className="w-full flex items-center px-3 py-2 text-gray-300 hover:bg-gray-700 hover:text-white rounded-lg transition-colors text-left"
        >
          <FontAwesomeIcon icon={faSignOutAlt} className="w-4 h-4 mr-3" />
          <span>Logout</span>
        </button>
      </div>
    );
  }

  // Button variant - show dropdown menu
  return (
    <div className={`relative ${className}`} ref={menuRef}>
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center space-x-3 text-gray-700 hover:text-gray-900 p-2 rounded-lg hover:bg-gray-100 transition-colors"
      >
        <div className="w-8 h-8 bg-blue-600 rounded-full flex items-center justify-center">
          <span className="text-white text-sm font-medium">{getUserInitials()}</span>
        </div>
        <div className="hidden sm:block text-left">
          <div className="font-medium text-sm">{user.firstName} {user.lastName}</div>
          <div className="text-xs text-gray-500">{getContextDisplay()}</div>
        </div>
        <FontAwesomeIcon icon={faChevronDown} className="w-4 h-4 text-gray-400" />
      </button>
      
      {isOpen && (
        <div className="absolute right-0 mt-2 w-56 bg-white border border-gray-200 rounded-lg shadow-lg z-50">
          <div className="py-2">
            <button
              onClick={handleSwitchTeams}
              className="w-full flex items-center px-4 py-2 text-gray-700 hover:bg-gray-100 transition-colors text-left"
            >
              <FontAwesomeIcon icon={faExchangeAlt} className="w-4 h-4 mr-3" />
              <span>Switch Teams</span>
            </button>
            <button
              onClick={handleUserProfile}
              className="w-full flex items-center px-4 py-2 text-gray-700 hover:bg-gray-100 transition-colors text-left"
            >
              <FontAwesomeIcon icon={faUser} className="w-4 h-4 mr-3" />
              <span>User Profile</span>
            </button>
            <hr className="my-2 border-gray-200" />
            <button
              onClick={handleLogout}
              className="w-full flex items-center px-4 py-2 text-gray-700 hover:bg-gray-100 transition-colors text-left"
            >
              <FontAwesomeIcon icon={faSignOutAlt} className="w-4 h-4 mr-3" />
              <span>Logout</span>
            </button>
          </div>
        </div>
      )}
    </div>
  );
} 