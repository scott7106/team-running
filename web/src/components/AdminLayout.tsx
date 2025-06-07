'use client';

import { useState, useEffect, ReactNode } from 'react';
import { useRouter } from 'next/navigation';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { 
  faUsers, 
  faBuilding, 
  faBars,
  faTimes
} from '@fortawesome/free-solid-svg-icons';
import { getUserFromToken } from '@/utils/auth';
import UserContextMenu from './user-context-menu';

interface AdminLayoutProps {
  children: ReactNode;
  pageTitle: string;
  currentSection?: 'dashboard' | 'teams' | 'users';
}

export default function AdminLayout({ children, pageTitle, currentSection = 'dashboard' }: AdminLayoutProps) {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [userInfo, setUserInfo] = useState<{
    firstName: string;
    lastName: string;
    email: string;
  } | null>(null);
  const router = useRouter();

  useEffect(() => {
    // Check if user is authenticated and get user info from token
    const token = localStorage.getItem('token');
    if (!token) {
      router.push('/');
      return;
    }

    const user = getUserFromToken();
    if (user) {
      setUserInfo(user);
    } else {
      // If we can't get user from token, fallback to placeholder
      setUserInfo({
        firstName: 'Admin',
        lastName: 'User',
        email: 'admin@teamstride.com'
      });
    }
  }, [router]);

  const handleManageTeams = () => {
    router.push('/admin/teams');
    setIsSidebarOpen(false);
  };

  const handleManageUsers = () => {
    router.push('/admin/users');
    setIsSidebarOpen(false);
  };

  if (!userInfo) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-lg text-gray-600">Loading...</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 lg:flex">
      {/* Sidebar */}
      <div className={`fixed inset-y-0 left-0 w-64 bg-gray-900 transform ${
        isSidebarOpen ? 'translate-x-0' : '-translate-x-full'
      } transition-transform duration-300 ease-in-out lg:translate-x-0 lg:relative lg:flex-shrink-0 z-50 lg:z-auto`}>
        <div className="flex items-center justify-center h-16 px-4 bg-gray-800">
          <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center">
            <span className="text-white font-bold text-lg">T</span>
          </div>
          <span className="ml-2 text-xl font-bold text-white">TeamStride</span>
          <button
            onClick={() => setIsSidebarOpen(false)}
            className="ml-auto text-gray-400 hover:text-white lg:hidden"
          >
            <FontAwesomeIcon icon={faTimes} />
          </button>
        </div>

        <nav className="mt-8">
          <div className="px-4 space-y-2">
            <button
              onClick={handleManageTeams}
              className={`w-full flex items-center px-4 py-3 rounded-lg transition-colors text-left ${
                currentSection === 'teams' 
                  ? 'bg-gray-700 text-white' 
                  : 'text-gray-300 hover:bg-gray-700 hover:text-white'
              }`}
            >
              <FontAwesomeIcon icon={faBuilding} className="w-5 h-5 mr-3" />
              <span>Manage Teams</span>
            </button>
            <button
              onClick={handleManageUsers}
              className={`w-full flex items-center px-4 py-3 rounded-lg transition-colors text-left ${
                currentSection === 'users' 
                  ? 'bg-gray-700 text-white' 
                  : 'text-gray-300 hover:bg-gray-700 hover:text-white'
              }`}
            >
              <FontAwesomeIcon icon={faUsers} className="w-5 h-5 mr-3" />
              <span>Manage Users</span>
            </button>
          </div>
        </nav>


      </div>

      {/* Main content */}
      <div className="flex-1 lg:flex lg:flex-col lg:overflow-hidden">
        {/* Top bar */}
        <div className="bg-white shadow-sm border-b">
          <div className="flex items-center justify-between h-16 px-4">
            <button
              onClick={() => setIsSidebarOpen(true)}
              className="text-gray-500 hover:text-gray-700 lg:hidden"
            >
              <FontAwesomeIcon icon={faBars} className="w-6 h-6" />
            </button>
            
            {/* Desktop: Show page title */}
            <h1 className="hidden lg:block text-xl font-semibold text-gray-900">{pageTitle}</h1>
            
            {/* Mobile & Desktop: User Context Menu */}
            <UserContextMenu className="lg:hidden" />
            
            <div className="hidden lg:flex items-center space-x-4">
              <UserContextMenu />
            </div>
          </div>
        </div>

        {/* Page content */}
        <div className="flex-1 p-6 lg:overflow-y-auto">
          {children}
        </div>
      </div>

      {/* Mobile overlay */}
      {isSidebarOpen && (
        <div 
          className="fixed inset-0 bg-black bg-opacity-50 z-40 lg:hidden"
          onClick={() => setIsSidebarOpen(false)}
        />
      )}
    </div>
  );
} 