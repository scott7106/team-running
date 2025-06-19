'use client';

import { useRouter } from 'next/navigation';
import Image from 'next/image';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faTimes } from '@fortawesome/free-solid-svg-icons';
import { NavigationItem } from './navigation-config';

interface NavigationSidebarProps {
  items: NavigationItem[];
  currentSection?: string;
  variant: 'admin' | 'team';
  isOpen: boolean;
  onClose: () => void;
  siteName: string;
  logoUrl?: string;
  showTeamTheme?: boolean;
}

export default function NavigationSidebar({
  items,
  currentSection,
  variant,
  isOpen,
  onClose,
  siteName,
  logoUrl,
  showTeamTheme = false
}: NavigationSidebarProps) {
  const router = useRouter();

  const handleNavigation = (href: string, isImplemented?: boolean) => {
    if (!isImplemented && isImplemented !== undefined) {
      // Show "coming soon" behavior - could be a toast or just do nothing
      console.log('Feature coming soon');
      return;
    }
    
    router.push(href);
    onClose();
  };

  const sidebarBgClass = variant === 'admin' 
    ? 'bg-white' 
    : showTeamTheme 
      ? 'team-sidebar'
      : 'bg-white';

  const headerBgClass = variant === 'admin' 
    ? 'border-b border-gray-200' 
    : showTeamTheme 
      ? 'border-b border-white/20'
      : 'border-b border-gray-200';

  const textColorClass = variant === 'admin'
    ? 'text-gray-900'
    : showTeamTheme
      ? 'text-white'
      : 'text-gray-900';

  const logoBackgroundClass = variant === 'admin'
    ? 'bg-blue-600'
    : showTeamTheme
      ? 'bg-white/20'
      : 'bg-blue-600';

  return (
    <div className={`fixed inset-y-0 left-0 w-64 transform ${
      isOpen ? 'translate-x-0' : '-translate-x-full'
    } transition-transform duration-300 ease-in-out lg:translate-x-0 lg:relative lg:flex-shrink-0 z-50 lg:z-auto ${sidebarBgClass} shadow-xl lg:shadow-none`}>
      
      {/* Sidebar Header */}
      <div className={`flex items-center justify-between h-16 px-4 ${headerBgClass}`}>
        <div className="flex items-center">
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
            <div className={`w-8 h-8 rounded-lg flex items-center justify-center ${logoBackgroundClass}`}>
              <span className="text-white font-bold text-lg">
                {siteName.charAt(0).toUpperCase()}
              </span>
            </div>
          )}
          <span className={`ml-2 text-xl font-bold ${textColorClass}`}>{siteName}</span>
        </div>
        <button
          onClick={onClose}
          className={`p-2 rounded-lg transition-colors lg:hidden ${
            variant === 'admin' 
              ? 'text-gray-400 hover:text-gray-600 hover:bg-gray-100' 
              : showTeamTheme
                ? 'text-white/70 hover:text-white hover:bg-white/10'
                : 'text-gray-400 hover:text-gray-600 hover:bg-gray-100'
          }`}
        >
          <FontAwesomeIcon icon={faTimes} className="w-5 h-5" />
        </button>
      </div>

      {/* Navigation Items */}
      <nav className="mt-8">
        <div className="px-4 space-y-2">
          {items.map((item) => {
            const isActive = currentSection === item.key;
            const isNotImplemented = item.isImplemented === false;
            
            // Define button styles based on variant and state
            let buttonClasses = 'w-full flex items-center px-4 py-3 rounded-lg transition-colors text-left ';
            
            if (variant === 'admin') {
              if (isActive) {
                buttonClasses += 'bg-blue-50 text-blue-700 border border-blue-200';
              } else if (isNotImplemented) {
                buttonClasses += 'text-gray-400 cursor-not-allowed opacity-60';
              } else {
                buttonClasses += 'text-gray-700 hover:bg-gray-50 hover:text-gray-900';
              }
            } else {
              // Team variant
              if (isActive) {
                buttonClasses += showTeamTheme 
                  ? 'bg-white/20 text-white border border-white/30' 
                  : 'bg-blue-50 text-blue-700 border border-blue-200';
              } else if (isNotImplemented) {
                buttonClasses += showTeamTheme 
                  ? 'text-white/40 cursor-not-allowed opacity-60'
                  : 'text-gray-400 cursor-not-allowed opacity-60';
              } else {
                buttonClasses += showTeamTheme 
                  ? 'text-white/90 hover:bg-white/10 hover:text-white'
                  : 'text-gray-700 hover:bg-gray-50 hover:text-gray-900';
              }
            }
            
            return (
              <button
                key={item.key}
                onClick={() => handleNavigation(item.href, item.isImplemented)}
                disabled={isNotImplemented}
                className={buttonClasses}
              >
                <FontAwesomeIcon icon={item.icon} className="w-5 h-5 mr-3" />
                <span>{item.label}</span>
                {isNotImplemented && (
                  <span className={`ml-auto text-xs px-2 py-1 rounded ${
                    variant === 'admin' 
                      ? 'bg-gray-100 text-gray-500'
                      : showTeamTheme
                        ? 'bg-white/20 text-white/70'
                        : 'bg-gray-100 text-gray-500'
                  }`}>Soon</span>
                )}
              </button>
            );
          })}
        </div>
      </nav>
    </div>
  );
} 