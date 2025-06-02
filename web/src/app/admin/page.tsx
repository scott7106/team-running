'use client';

import { useState, useEffect, useRef } from 'react';
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

export default function GlobalAdminPage() {
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
  };

  const handleManageUsers = () => {
    router.push('/admin/users');
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
              className="w-full flex items-center px-4 py-3 text-gray-300 hover:bg-gray-700 hover:text-white rounded-lg transition-colors text-left"
            >
              <FontAwesomeIcon icon={faBuilding} className="w-5 h-5 mr-3" />
              <span>Manage Teams</span>
            </button>
            <button
              onClick={handleManageUsers}
              className="w-full flex items-center px-4 py-3 text-gray-300 hover:bg-gray-700 hover:text-white rounded-lg transition-colors text-left"
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
            
            {/* Desktop: Show "Global Administration" */}
            <h1 className="hidden lg:block text-xl font-semibold text-gray-900">Global Administration</h1>
            
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
          <div className="max-w-4xl mx-auto">
            <div className="mb-8">
              <h2 className="text-2xl font-bold text-gray-900 mb-2">Administration Dashboard</h2>
              <p className="text-gray-600">
                Manage teams and users across the entire TeamStride platform.
              </p>
            </div>

            {/* Main action cards */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {/* Manage Teams Card */}
              <div 
                onClick={handleManageTeams}
                className="bg-white rounded-lg shadow-sm border border-gray-200 hover:shadow-md transition-shadow duration-300 cursor-pointer group"
              >
                <div className="p-8 text-center">
                  <div className="w-16 h-16 bg-blue-100 rounded-lg flex items-center justify-center mx-auto mb-4 group-hover:bg-blue-200 transition-colors">
                    <FontAwesomeIcon icon={faBuilding} className="text-blue-600 text-2xl" />
                  </div>
                  <h3 className="text-xl font-semibold text-gray-900 mb-3">Manage Teams</h3>
                  <p className="text-gray-600 mb-4">
                    Create, edit, and manage teams across the platform. Control team settings, subscriptions, and ownership.
                  </p>
                  <div className="text-blue-600 font-medium group-hover:text-blue-700">
                    Access Team Management →
                  </div>
                </div>
              </div>

              {/* Manage Users Card */}
              <div 
                onClick={handleManageUsers}
                className="bg-white rounded-lg shadow-sm border border-gray-200 hover:shadow-md transition-shadow duration-300 cursor-pointer group"
              >
                <div className="p-8 text-center">
                  <div className="w-16 h-16 bg-green-100 rounded-lg flex items-center justify-center mx-auto mb-4 group-hover:bg-green-200 transition-colors">
                    <FontAwesomeIcon icon={faUsers} className="text-green-600 text-2xl" />
                  </div>
                  <h3 className="text-xl font-semibold text-gray-900 mb-3">Manage Users</h3>
                  <p className="text-gray-600 mb-4">
                    View and manage user accounts, permissions, and global roles across all teams.
                  </p>
                  <div className="text-green-600 font-medium group-hover:text-green-700">
                    Access User Management →
                  </div>
                </div>
              </div>
            </div>

            {/* Quick stats */}
            <div className="mt-8 grid grid-cols-1 sm:grid-cols-3 gap-4">
              <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
                <div className="flex items-center">
                  <div className="w-12 h-12 bg-blue-100 rounded-lg flex items-center justify-center">
                    <FontAwesomeIcon icon={faBuilding} className="text-blue-600" />
                  </div>
                  <div className="ml-4">
                    <p className="text-2xl font-bold text-gray-900">--</p>
                    <p className="text-sm text-gray-600">Active Teams</p>
                  </div>
                </div>
              </div>
              
              <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
                <div className="flex items-center">
                  <div className="w-12 h-12 bg-green-100 rounded-lg flex items-center justify-center">
                    <FontAwesomeIcon icon={faUsers} className="text-green-600" />
                  </div>
                  <div className="ml-4">
                    <p className="text-2xl font-bold text-gray-900">--</p>
                    <p className="text-sm text-gray-600">Total Users</p>
                  </div>
                </div>
              </div>
              
              <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
                <div className="flex items-center">
                  <div className="w-12 h-12 bg-purple-100 rounded-lg flex items-center justify-center">
                    <FontAwesomeIcon icon={faUserShield} className="text-purple-600" />
                  </div>
                  <div className="ml-4">
                    <p className="text-2xl font-bold text-gray-900">--</p>
                    <p className="text-sm text-gray-600">Global Admins</p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
} 