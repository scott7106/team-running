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
    ? 'bg-gray-900' 
    : showTeamTheme 
      ? 'team-sidebar'
      : 'bg-gray-900';

  const headerBgClass = variant === 'admin' 
    ? 'bg-gray-800' 
    : showTeamTheme 
      ? 'bg-black bg-opacity-20'
      : 'bg-gray-800';

  return (
    <div className={`fixed inset-y-0 left-0 w-64 transform ${
      isOpen ? 'translate-x-0' : '-translate-x-full'
    } transition-transform duration-300 ease-in-out lg:translate-x-0 lg:relative lg:flex-shrink-0 z-50 lg:z-auto ${sidebarBgClass}`}>
      
      {/* Sidebar Header */}
      <div className={`flex items-center justify-center h-16 px-4 ${headerBgClass}`}>
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
          <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center">
            <span className="text-white font-bold text-lg">
              {siteName.charAt(0).toUpperCase()}
            </span>
          </div>
        )}
        <span className="ml-2 text-xl font-bold text-white">{siteName}</span>
        <button
          onClick={onClose}
          className="ml-auto text-gray-400 hover:text-white lg:hidden"
        >
          <FontAwesomeIcon icon={faTimes} />
        </button>
      </div>

      {/* Navigation Items */}
      <nav className="mt-8">
        <div className="px-4 space-y-2">
          {items.map((item) => {
            const isActive = currentSection === item.key;
            const isNotImplemented = item.isImplemented === false;
            
            return (
              <button
                key={item.key}
                onClick={() => handleNavigation(item.href, item.isImplemented)}
                disabled={isNotImplemented}
                className={`w-full flex items-center px-4 py-3 rounded-lg transition-colors text-left ${
                  isActive 
                    ? 'bg-gray-700 text-white' 
                    : isNotImplemented
                      ? 'text-gray-300 cursor-not-allowed opacity-60'
                      : 'text-gray-300 hover:bg-gray-700 hover:text-white'
                }`}
              >
                <FontAwesomeIcon icon={item.icon} className="w-5 h-5 mr-3" />
                <span>{item.label}</span>
                {isNotImplemented && (
                  <span className="ml-auto text-xs bg-gray-700 px-2 py-1 rounded">Soon</span>
                )}
              </button>
            );
          })}
        </div>
      </nav>
    </div>
  );
} 