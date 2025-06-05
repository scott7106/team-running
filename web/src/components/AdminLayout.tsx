'use client';

import { useState, useEffect, useRef, ReactNode } from 'react';
import { useRouter } from 'next/navigation';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { 
  faUsers, 
  faBuilding, 
  faSignOutAlt,
  faUserShield,
  faBars,
  faTimes,
  faUser,
  faChevronDown
} from '@fortawesome/free-solid-svg-icons';
import { getUserFromToken, logout } from '@/utils/auth';

interface AdminLayoutProps {
  children: ReactNode;
  pageTitle: string;
  currentSection?: 'dashboard' | 'teams' | 'users';
}

export default function AdminLayout({ children, pageTitle, currentSection = 'dashboard' }: AdminLayoutProps) {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [isUserMenuOpen, setIsUserMenuOpen] = useState(false);
  const [userInfo, setUserInfo] = useState<{
    firstName: string;
    lastName: string;
    email: string;
  } | null>(null);
  const router = useRouter();
  const userMenuRef = useRef<HTMLDivElement>(null);

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

  useEffect(() => {
    // Close user menu when clicking outside
    function handleClickOutside(event: MouseEvent) {
      if (userMenuRef.current && !userMenuRef.current.contains(event.target as Node)) {
        setIsUserMenuOpen(false);
      }
    }

    document.addEventListener('mousedown', handleClickOutside);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, []);

  const handleLogout = () => {
    logout();
  };

  const handleProfile = () => {
    // TODO: Implement profile navigation
    console.log('Navigate to profile');
    setIsUserMenuOpen(false);
  };

  const handleManageTeams = () => {
    router.push('/admin/teams');
    setIsSidebarOpen(false);
  };

  const handleManageUsers = () => {
    router.push('/admin/users');
    setIsSidebarOpen(false);
  };

  const handleDashboard = () => {
    router.push('/admin');
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
              onClick={handleDashboard}
              className={`w-full flex items-center px-4 py-3 rounded-lg transition-colors text-left ${
                currentSection === 'dashboard' 
                  ? 'bg-gray-700 text-white' 
                  : 'text-gray-300 hover:bg-gray-700 hover:text-white'
              }`}
            >
              <FontAwesomeIcon icon={faUserShield} className="w-5 h-5 mr-3" />
              <span>Dashboard</span>
            </button>
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

        {/* User info and logout */}
        <div className="absolute bottom-0 w-full p-4 border-t border-gray-700">
          <div className="flex items-center mb-3">
            <div className="w-8 h-8 bg-blue-600 rounded-full flex items-center justify-center">
              <FontAwesomeIcon icon={faUserShield} className="text-white text-sm" />
            </div>
            <div className="ml-3 text-sm">
              <p className="text-white font-medium">{userInfo.firstName} {userInfo.lastName}</p>
              <p className="text-gray-400">{userInfo.email}</p>
            </div>
          </div>
          <button
            onClick={handleLogout}
            className="w-full flex items-center px-3 py-2 text-gray-300 hover:bg-gray-700 hover:text-white rounded-lg transition-colors text-left"
          >
            <FontAwesomeIcon icon={faSignOutAlt} className="w-4 h-4 mr-2" />
            <span className="text-sm">Logout</span>
          </button>
        </div>
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
            
            {/* Mobile: Show User Menu */}
            <div className="lg:hidden relative" ref={userMenuRef}>
              <button
                onClick={() => setIsUserMenuOpen(!isUserMenuOpen)}
                className="flex items-center space-x-2 text-gray-700 hover:text-gray-900"
              >
                <div className="w-8 h-8 bg-blue-600 rounded-full flex items-center justify-center">
                  <FontAwesomeIcon icon={faUser} className="text-white text-sm" />
                </div>
                <span className="font-medium">{userInfo.firstName}</span>
                <FontAwesomeIcon icon={faChevronDown} className="w-4 h-4" />
              </button>
              
              {/* Mobile User Dropdown Menu */}
              {isUserMenuOpen && (
                <div className="absolute right-0 mt-2 w-48 bg-white rounded-lg shadow-lg border border-gray-200 py-1 z-50">
                  <button
                    onClick={handleProfile}
                    className="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 flex items-center"
                  >
                    <FontAwesomeIcon icon={faUser} className="w-4 h-4 mr-3" />
                    Profile
                  </button>
                  <button
                    onClick={handleLogout}
                    className="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 flex items-center"
                  >
                    <FontAwesomeIcon icon={faSignOutAlt} className="w-4 h-4 mr-3" />
                    Logout
                  </button>
                </div>
              )}
            </div>
            
            <div className="hidden lg:flex items-center space-x-4">
              <span className="text-sm text-gray-600">Welcome, {userInfo.firstName}</span>
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