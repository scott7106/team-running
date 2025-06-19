'use client';

import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faBars } from '@fortawesome/free-solid-svg-icons';
import UserContextMenu from '@/components/shared/user-context-menu';
import { useUser } from '@/contexts/auth-context';
import Image from 'next/image';

interface UnifiedHeaderProps {
  variant: 'admin' | 'team';
  siteName: string;
  logoUrl?: string;
  onMobileMenuToggle: () => void;
  showTeamTheme?: boolean;
}

export default function UnifiedHeader({
  variant,
  siteName,
  logoUrl,
  onMobileMenuToggle,
  showTeamTheme = false
}: UnifiedHeaderProps) {
  const { isAuthenticated } = useUser();

  const headerBgClass = variant === 'team' && showTeamTheme 
    ? 'team-navbar' 
    : 'bg-white';
  
  const textColorClass = variant === 'team' && showTeamTheme 
    ? 'text-white' 
    : 'text-gray-900';

  return (
    <div className={`shadow-sm border-b ${headerBgClass}`}>
      <div className="flex items-center justify-between h-16 px-4">
        <div className="flex items-center">
          <button
            onClick={onMobileMenuToggle}
            className={`mr-3 hover:opacity-75 lg:hidden ${
              variant === 'team' && showTeamTheme ? 'text-white' : 'text-gray-500'
            }`}
          >
            <FontAwesomeIcon icon={faBars} className="w-6 h-6" />
          </button>
          
          {/* Logo and Site Name */}
          <div className="flex items-center space-x-3">
            {logoUrl ? (
              <div className="w-8 h-8 rounded-lg flex items-center justify-center overflow-hidden bg-white">
                <Image 
                  src={logoUrl} 
                  alt={`${siteName} logo`} 
                  width={24}
                  height={24}
                  className="object-contain"
                />
              </div>
            ) : (
              <div className={`w-8 h-8 rounded-lg flex items-center justify-center ${
                variant === 'admin' 
                  ? 'bg-blue-600' 
                  : variant === 'team' && showTeamTheme
                    ? 'bg-white bg-opacity-20'
                    : 'bg-blue-600'
              }`}>
                <span className={`font-bold text-lg ${
                  variant === 'admin' 
                    ? 'text-white' 
                    : variant === 'team' && showTeamTheme
                      ? 'text-white'
                      : 'text-white'
                }`}>
                  {siteName.charAt(0).toUpperCase()}
                </span>
              </div>
            )}
            <span className={`text-xl font-bold ${textColorClass}`}>
              {siteName}
            </span>
          </div>
        </div>

        {/* Right side - Login button or User Context Menu */}
        <div className="flex items-center space-x-4">
          {isAuthenticated ? (
            <UserContextMenu />
          ) : (
            <a
              href="/login"
              className={`px-4 py-2 rounded-md text-sm font-medium transition-colors ${
                variant === 'team' && showTeamTheme
                  ? 'bg-white bg-opacity-20 text-white hover:bg-opacity-30'
                  : 'bg-blue-600 hover:bg-blue-700 text-white'
              }`}
            >
              Login
            </a>
          )}
        </div>
      </div>
    </div>
  );
} 