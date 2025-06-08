'use client';

import { useState, useEffect, useRef } from 'react';
import { useRouter } from 'next/navigation';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { 
  faUser, 
  faChevronDown, 
  faSignOutAlt, 
  faHome, 
  faGlobe, 
  faExchangeAlt
} from '@fortawesome/free-solid-svg-icons';
import { getUserFromToken, getTeamContextFromToken, logout, decodeToken } from '@/utils/auth';

interface UserInfo {
  firstName: string;
  lastName: string;
  email: string;
}

interface TeamContext {
  contextLabel: string;
  isGlobalAdmin: boolean;
  hasTeam: boolean;
  teamId?: string;
  teamRole?: string;
}

interface TenantDto {
  teamId: string;
  teamName: string;
  subdomain: string;
  primaryColor: string;
  secondaryColor: string;
}

interface UserContextMenuProps {
  /** Optional custom styling class */
  className?: string;
  /** Show as button (default) or inline menu */
  variant?: 'button' | 'inline';
}

export default function UserContextMenu({ className = '', variant = 'button' }: UserContextMenuProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [userInfo, setUserInfo] = useState<UserInfo | null>(null);
  const [teamContext, setTeamContext] = useState<TeamContext | null>(null);
  const [canSwitchTeams, setCanSwitchTeams] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const router = useRouter();

  useEffect(() => {
    // Load user info and team context
    const user = getUserFromToken();
    const context = getTeamContextFromToken();
    
    setUserInfo(user);
    setTeamContext(context);

    // Load available teams to determine if user can switch
    loadAvailableTeams();
  }, []);

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

  const loadAvailableTeams = async () => {
    try {
      const token = localStorage.getItem('token');
      if (!token) return;

      // Check if user is global admin
      let isGlobalAdmin = false;
      try {
        const adminResponse = await fetch('/api/admin/teams?pageSize=1', {
          headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json',
          },
        });
        isGlobalAdmin = adminResponse.ok;
      } catch {
        // Not a global admin
      }

      // Get user's team access
      const teamsResponse = await fetch('/api/tenant-switcher/tenants', {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
      });

             let userTeams: TenantDto[] = [];
       if (teamsResponse.ok) {
         userTeams = await teamsResponse.json();
       }
       
       // User can switch teams if they have: (Global Admin access) + (1+ teams) OR (2+ teams)
       const totalOptions = (isGlobalAdmin ? 1 : 0) + userTeams.length;
       setCanSwitchTeams(totalOptions > 1);
      
    } catch (error) {
      console.error('Error loading available teams:', error);
    }
  };

  const handleSwitchTeams = () => {
    setIsOpen(false);
    // Navigate to login page which will handle the team selection logic
    router.push('/login');
  };

  const handleDashboard = () => {
    setIsOpen(false);
    
    if (teamContext?.hasTeam) {
      // Get team subdomain from JWT token
      const token = localStorage.getItem('token');
      if (token) {
        try {
          const claims = decodeToken(token);
          if (claims && claims.team_id) {
            const teamSubdomain = claims.team_subdomain;
            const teamId = claims.team_id;
            
            // Use the same routing logic as login page
            routeToTeamPage(teamId, teamSubdomain);
          } else {
            // No valid claims, fallback to basic team page
            router.push('/team');
          }
        } catch (error) {
          console.error('Error parsing token for team routing:', error);
          // Fallback to basic team page
          router.push('/team');
        }
      } else {
        // No token, fallback
        router.push('/team');
      }
    } else if (teamContext?.isGlobalAdmin) {
      // Navigate to admin dashboard
      router.push('/admin');
    } else {
      // Fallback to home
      router.push('/');
    }
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
      // Route to team page with teamId parameter
      router.push(`/team?teamId=${teamId}`);
    }
  };

  const handleTeamStride = () => {
    setIsOpen(false);
    // Navigate to main home page (no team context)
    window.location.href = '/';
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

  if (!userInfo) {
    return null; // Don't show if user is not authenticated
  }

  const getUserInitials = () => {
    return `${userInfo.firstName.charAt(0)}${userInfo.lastName.charAt(0)}`.toUpperCase();
  };

  const getContextDisplay = () => {
    if (teamContext?.hasTeam) {
      return teamContext.contextLabel;
    } else if (teamContext?.isGlobalAdmin) {
      return 'Global Admin';
    }
    return 'No Team';
  };

  if (variant === 'inline') {
    // Inline variant - show menu items directly without dropdown
    return (
      <div className={`space-y-1 ${className}`}>
        {canSwitchTeams && (
          <button
            onClick={handleSwitchTeams}
            className="w-full flex items-center px-3 py-2 text-gray-300 hover:bg-gray-700 hover:text-white rounded-lg transition-colors text-left"
          >
            <FontAwesomeIcon icon={faExchangeAlt} className="w-4 h-4 mr-3" />
            <span>Switch Teams</span>
          </button>
        )}
        <button
          onClick={handleDashboard}
          className="w-full flex items-center px-3 py-2 text-gray-300 hover:bg-gray-700 hover:text-white rounded-lg transition-colors text-left"
        >
          <FontAwesomeIcon icon={faHome} className="w-4 h-4 mr-3" />
          <span>Dashboard</span>
        </button>
        <button
          onClick={handleTeamStride}
          className="w-full flex items-center px-3 py-2 text-gray-300 hover:bg-gray-700 hover:text-white rounded-lg transition-colors text-left"
        >
          <FontAwesomeIcon icon={faGlobe} className="w-4 h-4 mr-3" />
          <span>TeamStride</span>
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
          <div className="font-medium text-sm">{userInfo.firstName} {userInfo.lastName}</div>
          <div className="text-xs text-gray-500">{getContextDisplay()}</div>
        </div>
        <FontAwesomeIcon icon={faChevronDown} className="w-4 h-4 text-gray-400" />
      </button>
      
      {isOpen && (
        <div className="absolute right-0 mt-2 w-56 bg-white rounded-lg shadow-lg border border-gray-200 py-1 z-50">
          {/* User info header */}
          <div className="px-4 py-2 border-b border-gray-100">
            <div className="font-medium text-sm text-gray-900">{userInfo.firstName} {userInfo.lastName}</div>
            <div className="text-xs text-gray-500">{userInfo.email}</div>
            <div className="text-xs text-blue-600 mt-1">{getContextDisplay()}</div>
          </div>
          
          {/* Menu items */}
          <div className="py-1">
            {canSwitchTeams && (
              <button
                onClick={handleSwitchTeams}
                className="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 flex items-center"
              >
                <FontAwesomeIcon icon={faExchangeAlt} className="w-4 h-4 mr-3 text-gray-400" />
                Switch Teams
              </button>
            )}
            <button
              onClick={handleDashboard}
              className="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 flex items-center"
            >
              <FontAwesomeIcon icon={faHome} className="w-4 h-4 mr-3 text-gray-400" />
              Dashboard
            </button>
            <button
              onClick={handleTeamStride}
              className="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 flex items-center"
            >
              <FontAwesomeIcon icon={faGlobe} className="w-4 h-4 mr-3 text-gray-400" />
              TeamStride
            </button>
            <button
              onClick={handleUserProfile}
              className="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 flex items-center"
            >
              <FontAwesomeIcon icon={faUser} className="w-4 h-4 mr-3 text-gray-400" />
              User Profile
            </button>
          </div>
          
          {/* Logout - separated */}
          <div className="border-t border-gray-100 py-1">
            <button
              onClick={handleLogout}
              className="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 flex items-center"
            >
              <FontAwesomeIcon icon={faSignOutAlt} className="w-4 h-4 mr-3 text-gray-400" />
              Logout
            </button>
          </div>
        </div>
      )}
    </div>
  );
} 