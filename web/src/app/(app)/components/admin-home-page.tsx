'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { 
  faUsers, 
  faBuilding, 
  faUserShield,
  faSpinner
} from '@fortawesome/free-solid-svg-icons';
import { useAuthTokenRefresh } from '@/hooks/use-auth-token-refresh';
import UserContextMenu from '@/components/shared/user-context-menu';
import { useUser, useTenant } from '@/contexts/auth-context';
import { dashboardApi, DashboardStatsDto, ApiError } from '@/utils/api';
import HeroSection from '@/components/shared/hero-section';
import BaseLayout from '@/components/layouts/base-layout';
import { ADMIN_NAV_ITEMS } from '@/components/layouts/navigation-config';

export default function AdminHomePage() {
  const router = useRouter();
  const [stats, setStats] = useState<DashboardStatsDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  
  // Use centralized auth state
  const { isAuthenticated, subdomainAccessDenied } = useUser();
  const { isGlobalAdmin } = useTenant();
  
  // Auto-refresh token when subdomain context changes
  useAuthTokenRefresh();

  // No longer need useEffect for auth - managed by AuthContext

  useEffect(() => {
    const loadDashboardStats = async () => {
      try {
        setLoading(true);
        setError(null);
        const dashboardStats = await dashboardApi.getStats();
        setStats(dashboardStats);
      } catch (err) {
        if (err instanceof ApiError) {
          setError(err.message);
        } else {
          setError('Failed to load dashboard statistics. Please try again.');
        }
        console.error('Error loading dashboard stats:', err);
      } finally {
        setLoading(false);
      }
    };

    // Only load dashboard stats if user is authenticated, has subdomain access, and is a global admin
    if (isAuthenticated && !subdomainAccessDenied && isGlobalAdmin) {
      loadDashboardStats();
    }
  }, [isAuthenticated, subdomainAccessDenied, isGlobalAdmin]);

  const handleManageTeams = () => {
    router.push('/teams');
  };

  const handleManageUsers = () => {
    router.push('/users');
  };

  const handleLoginClick = () => {
    router.push('/login');
  };

  const handleVisitMainSite = () => {
    router.push('/');
  };

  const handleGoToTenantSwitcher = () => {
    router.push('/tenant-switcher');
  };

  // If not authenticated, show simple login interface
  if (!isAuthenticated) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
        {/* Header for unauthenticated users */}
        <header className="bg-white shadow-sm">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <div className="flex justify-between items-center py-4">
              <div className="flex items-center">
                <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center">
                  <span className="text-white font-bold text-lg">T</span>
                </div>
                <span className="ml-2 text-xl font-bold text-gray-900">TeamStride</span>
              </div>
              
              <button
                onClick={handleLoginClick}
                className="bg-blue-600 text-white px-4 py-2 rounded-lg font-medium hover:bg-blue-700 transition-colors"
              >
                Login
              </button>
            </div>
          </div>
        </header>

        {/* Hero Section */}
        <HeroSection
          onLoginClick={handleLoginClick}
          primaryButtonText="Get Started"
          showSecondaryButton={true}
          onSecondaryAction={() => {
            // Watch demo action - could navigate to a demo page or open a modal
            console.log('Watch demo clicked');
          }}
        />
      </div>
    );
  }

  // If authenticated but has subdomain access denied, show access denied
  if (isAuthenticated && subdomainAccessDenied) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
        {/* Header for authenticated non-admin users */}
        <header className="bg-white shadow-sm">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <div className="flex justify-between items-center py-4">
              <div className="flex items-center">
                <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center">
                  <span className="text-white font-bold text-lg">T</span>
                </div>
                <span className="ml-2 text-xl font-bold text-gray-900">TeamStride</span>
              </div>
              
              <UserContextMenu />
            </div>
          </div>
        </header>

        {/* Admin Access Alert */}
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 pt-8">
          <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
            <div className="flex">
              <div className="flex-shrink-0">
                <svg className="h-5 w-5 text-yellow-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                  <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
                </svg>
              </div>
              <div className="ml-3">
                <h3 className="text-sm font-medium text-yellow-800">
                  Admin Access Required
                </h3>
                <div className="mt-2 text-sm text-yellow-700">
                  <p>
                    You do not have access to the admin site. 
                    <a 
                      href="#" 
                      onClick={(e) => {
                        e.preventDefault();
                        handleGoToTenantSwitcher();
                      }}
                      className="font-medium underline text-yellow-800 hover:text-yellow-900 ml-1"
                    >
                      Go to tenant switcher
                    </a> to access your teams or visit our 
                    <a 
                      href="#" 
                      onClick={(e) => {
                        e.preventDefault();
                        handleVisitMainSite();
                      }}
                      className="font-medium underline text-yellow-800 hover:text-yellow-900 ml-1"
                    >
                      main site
                    </a>.
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Hero Section */}
        <HeroSection
          onLoginClick={handleLoginClick}
          primaryButtonText="Get Started"
          showSecondaryButton={true}
          onSecondaryAction={() => {
            // Watch demo action - could navigate to a demo page or open a modal
            console.log('Watch demo clicked');
          }}
        />
      </div>
    );
  }

  // If authenticated but not a global admin, show access denied
  if (isAuthenticated && !subdomainAccessDenied && !isGlobalAdmin) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
        {/* Header for authenticated non-admin users */}
        <header className="bg-white shadow-sm">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <div className="flex justify-between items-center py-4">
              <div className="flex items-center">
                <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center">
                  <span className="text-white font-bold text-lg">T</span>
                </div>
                <span className="ml-2 text-xl font-bold text-gray-900">TeamStride</span>
              </div>
              
              <UserContextMenu />
            </div>
          </div>
        </header>

        {/* Admin Access Alert */}
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 pt-8">
          <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
            <div className="flex">
              <div className="flex-shrink-0">
                <svg className="h-5 w-5 text-yellow-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                  <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
                </svg>
              </div>
              <div className="ml-3">
                <h3 className="text-sm font-medium text-yellow-800">
                  Admin Access Required
                </h3>
                <div className="mt-2 text-sm text-yellow-700">
                  <p>
                    You do not have access to the admin site. 
                    <a 
                      href="#" 
                      onClick={(e) => {
                        e.preventDefault();
                        handleGoToTenantSwitcher();
                      }}
                      className="font-medium underline text-yellow-800 hover:text-yellow-900 ml-1"
                    >
                      Go to tenant switcher
                    </a> to access your teams or visit our 
                    <a 
                      href="#" 
                      onClick={(e) => {
                        e.preventDefault();
                        handleVisitMainSite();
                      }}
                      className="font-medium underline text-yellow-800 hover:text-yellow-900 ml-1"
                    >
                      main site
                    </a>.
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Hero Section */}
        <HeroSection
          onLoginClick={handleLoginClick}
          primaryButtonText="Get Started"
          showSecondaryButton={true}
          onSecondaryAction={() => {
            // Watch demo action - could navigate to a demo page or open a modal
            console.log('Watch demo clicked');
          }}
        />
      </div>
    );
  }

  // If authenticated and is a global admin, show full admin layout
  return (
    <BaseLayout
      pageTitle="Administration Dashboard"
      currentSection="dashboard"
      variant="admin"
      navigationItems={ADMIN_NAV_ITEMS}
      siteName="TeamStride"
    >
      <p className="text-gray-600 mb-8">
        Manage teams and users across the entire TeamStride platform.
      </p>

            {/* Main action cards */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {/* Manage Teams Card */}
              <div 
                onClick={handleManageTeams}
                className="bg-white rounded-lg shadow-sm border border-gray-200 hover:shadow-md transition-shadow duration-300 cursor-pointer group"
              >
                <div className="p-8 text-center h-full flex flex-col justify-between">
                  <div>
                    <div className="w-16 h-16 bg-blue-100 rounded-lg flex items-center justify-center mx-auto mb-4 group-hover:bg-blue-200 transition-colors">
                      <FontAwesomeIcon icon={faBuilding} className="text-blue-600 text-2xl" />
                    </div>
                    <h3 className="text-xl font-semibold text-gray-900 mb-3">Manage Teams</h3>
                    <p className="text-gray-600 mb-4">
                      Create, edit, and manage teams across the platform. Control team settings, subscriptions, and ownership.
                    </p>
                  </div>
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
                <div className="p-8 text-center h-full flex flex-col justify-between">
                  <div>
                    <div className="w-16 h-16 bg-green-100 rounded-lg flex items-center justify-center mx-auto mb-4 group-hover:bg-green-200 transition-colors">
                      <FontAwesomeIcon icon={faUsers} className="text-green-600 text-2xl" />
                    </div>
                    <h3 className="text-xl font-semibold text-gray-900 mb-3">Manage Users</h3>
                    <p className="text-gray-600 mb-4">
                      View and manage user accounts, permissions, and global roles across all teams.
                    </p>
                  </div>
                  <div className="text-green-600 font-medium group-hover:text-green-700">
                    Access User Management →
                  </div>
                </div>
              </div>
            </div>

            {/* Quick stats */}
            {error && (
              <div className="mt-8 bg-red-50 border border-red-200 rounded-lg p-4">
                <p className="text-red-800 text-sm">{error}</p>
              </div>
            )}
            
            <div className="mt-8 grid grid-cols-1 sm:grid-cols-3 gap-4">
              <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
                <div className="flex items-center">
                  <div className="w-12 h-12 bg-blue-100 rounded-lg flex items-center justify-center">
                    <FontAwesomeIcon icon={faBuilding} className="text-blue-600" />
                  </div>
                  <div className="ml-4">
                    <p className="text-2xl font-bold text-gray-900">
                      {loading ? (
                        <FontAwesomeIcon icon={faSpinner} className="animate-spin" />
                      ) : (
                        stats?.activeTeamsCount ?? '--'
                      )}
                    </p>
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
                    <p className="text-2xl font-bold text-gray-900">
                      {loading ? (
                        <FontAwesomeIcon icon={faSpinner} className="animate-spin" />
                      ) : (
                        stats?.totalUsersCount ?? '--'
                      )}
                    </p>
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
                    <p className="text-2xl font-bold text-gray-900">
                      {loading ? (
                        <FontAwesomeIcon icon={faSpinner} className="animate-spin" />
                      ) : (
                        stats?.globalAdminsCount ?? '--'
                      )}
                    </p>
                    <p className="text-sm text-gray-600">Global Admins</p>
                  </div>
                </div>
              </div>
            </div>
    </BaseLayout>
  );
} 