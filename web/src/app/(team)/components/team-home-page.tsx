'use client';

import { useState, useEffect } from 'react';
import TeamThemeProvider from './team-theme-provider';
import UserContextMenu from '@/components/shared/user-context-menu';
import { useAuthTokenRefresh } from '@/hooks/use-auth-token-refresh';
import { useUser, useTenant } from '@/contexts/auth-context';
import BaseLayout from '@/components/layouts/base-layout';
import { TEAM_NAV_ITEMS } from '@/components/layouts/navigation-config';
import { getCurrentTeamThemeData } from '@/utils/team-theme';
import { SubdomainThemeDto } from '@/types/team';

export default function TeamHomePage() {
  const [isTeamMember, setIsTeamMember] = useState(false);
  const [teamName, setTeamName] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [themeData, setThemeData] = useState<SubdomainThemeDto | null>(null);

  // Use centralized auth state
  const { isAuthenticated, subdomainAccessDenied } = useUser();
  const { tenant, hasTeam } = useTenant();

  // Auto-refresh token when subdomain context changes
  useAuthTokenRefresh();

  useEffect(() => {
    // Get team info from window location (middleware sets context)
    const hostname = window.location.hostname;
    let subdomain = '';
    
    if (hostname.includes('localhost')) {
      const parts = hostname.split('.');
      if (parts.length > 1) {
        subdomain = parts[0];
      }
    } else {
      const parts = hostname.split('.');
      if (parts.length > 2) {
        subdomain = parts[0];
      }
    }

    // Load theme data and check team membership
    const loadTeamData = async () => {
      try {
        const theme = await getCurrentTeamThemeData();
        setThemeData(theme);
        setTeamName(theme.teamName || subdomain.charAt(0).toUpperCase() + subdomain.slice(1));
      } catch (error) {
        console.error('Failed to load theme data:', error);
        setTeamName(subdomain.charAt(0).toUpperCase() + subdomain.slice(1));
      }

      // Check if current subdomain matches user's team context
      // Users with subdomainAccessDenied should be treated as non-members
      const hasTeamAccess = !subdomainAccessDenied && hasTeam && tenant?.teamSubdomain === subdomain;
      setIsTeamMember(hasTeamAccess || false);
      
      setIsLoading(false);
    };

    loadTeamData();
  }, [hasTeam, tenant, subdomainAccessDenied]);

  if (isLoading) {
    return (
      <TeamThemeProvider>
        <div className="min-h-screen flex items-center justify-center">
          <div className="text-center">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto"></div>
            <p className="mt-2 text-gray-600">Loading...</p>
          </div>
        </div>
      </TeamThemeProvider>
    );
  }

  if (!teamName) {
    return (
      <TeamThemeProvider>
        <div className="min-h-screen flex items-center justify-center">
          <div className="text-center">
            <h1 className="text-2xl font-bold text-gray-900 mb-4">Team Not Found</h1>
            <p className="text-gray-600">This team could not be found or is not available.</p>
          </div>
        </div>
      </TeamThemeProvider>
    );
  }

  // For authenticated team members, use BaseLayout
  if (isAuthenticated && isTeamMember) {
    return (
      <TeamThemeProvider>
        <BaseLayout
          pageTitle={`Welcome back to ${teamName}!`}
          currentSection="dashboard"
          variant="team"
          navigationItems={TEAM_NAV_ITEMS}
          siteName={teamName}
          logoUrl={themeData?.logoUrl}
          showTeamTheme={true}
        >
          <p className="text-gray-600 mb-8">
            Here&apos;s what&apos;s happening with your team today.
          </p>

          {/* Dashboard Content for Team Members */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {/* Quick Stats */}
            <div className="bg-white rounded-lg shadow p-6">
              <h3 className="text-lg font-semibold text-gray-900 mb-4">Team Stats</h3>
              <div className="space-y-2">
                <div className="flex justify-between">
                  <span className="text-gray-600">Active Members:</span>
                  <span className="font-medium">24</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600">This Week&apos;s Miles:</span>
                  <span className="font-medium">156</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600">Upcoming Races:</span>
                  <span className="font-medium">3</span>
                </div>
              </div>
            </div>

            {/* Recent Activities */}
            <div className="bg-white rounded-lg shadow p-6">
              <h3 className="text-lg font-semibold text-gray-900 mb-4">Recent Activities</h3>
              <div className="space-y-3">
                <div className="flex items-center space-x-3">
                  <div 
                    className="w-2 h-2 rounded-full"
                    style={{ backgroundColor: 'var(--team-primary)' }}
                  />
                  <span className="text-sm text-gray-600">Sarah completed 5 mile run</span>
                </div>
                <div className="flex items-center space-x-3">
                  <div 
                    className="w-2 h-2 rounded-full"
                    style={{ backgroundColor: 'var(--team-primary)' }}
                  />
                  <span className="text-sm text-gray-600">Mike logged track workout</span>
                </div>
                <div className="flex items-center space-x-3">
                  <div 
                    className="w-2 h-2 rounded-full"
                    style={{ backgroundColor: 'var(--team-primary)' }}
                  />
                  <span className="text-sm text-gray-600">Team practice scheduled</span>
                </div>
              </div>
            </div>

            {/* Coaches Corner */}
            <div className="bg-white rounded-lg shadow p-6">
              <h3 className="text-lg font-semibold text-gray-900 mb-4">Coaches Corner</h3>
              <div className="space-y-3">
                <div className="text-sm text-gray-600">
                  <p className="font-medium">Coach Johnson</p>
                  <p>&quot;Great work this week team! Remember to focus on form during tomorrow&apos;s tempo run.&quot;</p>
                </div>
              </div>
            </div>
          </div>
        </BaseLayout>
      </TeamThemeProvider>
    );
  }

  // For public/unauthenticated users, use existing layout
  return (
    <TeamThemeProvider>
      <div className="min-h-screen" style={{ backgroundColor: 'var(--team-primary-bg)' }}>
        {/* Header - always visible, conditionally shows user menu */}
        <header className="border-b border-gray-200 bg-white shadow-sm">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <div className="flex justify-between items-center h-16">
              {/* Team branding */}
              <div className="flex items-center space-x-3">
                <div 
                  className="w-8 h-8 rounded-full"
                  style={{ backgroundColor: 'var(--team-primary)' }}
                />
                <h1 
                  className="text-xl font-bold"
                  style={{ color: 'var(--team-primary)' }}
                >
                  {teamName}
                </h1>
              </div>

              {/* Header actions - login button or user menu */}
              <div className="flex items-center space-x-4">
                {isAuthenticated ? (
                  <UserContextMenu />
                ) : (
                  <a
                    href="/login"
                    className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium transition-colors"
                    style={{ 
                      backgroundColor: 'var(--team-primary)',
                      color: 'white'
                    }}
                  >
                    Login
                  </a>
                )}
              </div>
            </div>
          </div>
        </header>

        {/* Main content */}
        <main className="flex-1 p-0">
          {/* Public Team Page */}
          <div>
            {/* Hero Section */}
            <section 
              className="py-20 px-8 text-center text-white"
              style={{ backgroundColor: 'var(--team-primary)' }}
            >
              <div className="max-w-4xl mx-auto">
                <h1 className="text-5xl font-bold mb-6">{teamName}</h1>
                <p className="text-xl mb-8 opacity-90">
                  Join our running community and achieve your personal best
                </p>
                {!isAuthenticated ? (
                  <a 
                    href="/login"
                    className="inline-block bg-white text-gray-900 px-8 py-3 rounded-lg font-semibold hover:bg-gray-100 transition-colors"
                  >
                    Join Our Team
                  </a>
                ) : (
                  <div className="space-y-4">
                    <a 
                      href="/register"
                      className="inline-block bg-white text-gray-900 px-8 py-3 rounded-lg font-semibold hover:bg-gray-100 transition-colors"
                    >
                      Register for Team
                    </a>
                    {subdomainAccessDenied && (
                      <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4 max-w-md mx-auto">
                        <div className="flex">
                          <div className="flex-shrink-0">
                            <svg className="h-5 w-5 text-yellow-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                              <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
                            </svg>
                          </div>
                          <div className="ml-3">
                            <p className="text-sm text-yellow-700">
                              You do not have access to this team. 
                              <a 
                                href="/tenant-switcher" 
                                className="font-medium underline text-yellow-800 hover:text-yellow-900 ml-1"
                              >
                                Go to tenant switcher
                              </a> to access your teams.
                            </p>
                          </div>
                        </div>
                      </div>
                    )}
                  </div>
                )}
              </div>
            </section>

            {/* Public Content */}
            <section className="py-16 px-8">
              <div className="max-w-6xl mx-auto">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-12">
                  {/* About Team */}
                  <div>
                    <h2 className="text-3xl font-bold text-gray-900 mb-6">About Our Team</h2>
                    <p className="text-gray-600 mb-6">
                      {teamName} is a community of passionate runners dedicated to helping each other 
                      achieve their goals. Whether you&apos;re training for your first 5K or aiming for 
                      a marathon PR, you&apos;ll find the support and motivation you need here.
                    </p>
                    <ul className="space-y-3 text-gray-600">
                      <li className="flex items-center space-x-3">
                        <div 
                          className="w-2 h-2 rounded-full"
                          style={{ backgroundColor: 'var(--team-primary)' }}
                        />
                        <span>All skill levels welcome</span>
                      </li>
                      <li className="flex items-center space-x-3">
                        <div 
                          className="w-2 h-2 rounded-full"
                          style={{ backgroundColor: 'var(--team-primary)' }}
                        />
                        <span>Professional coaching staff</span>
                      </li>
                      <li className="flex items-center space-x-3">
                        <div 
                          className="w-2 h-2 rounded-full"
                          style={{ backgroundColor: 'var(--team-primary)' }}
                        />
                        <span>Regular group training sessions</span>
                      </li>
                      <li className="flex items-center space-x-3">
                        <div 
                          className="w-2 h-2 rounded-full"
                          style={{ backgroundColor: 'var(--team-primary)' }}
                        />
                        <span>Race participation opportunities</span>
                      </li>
                    </ul>
                  </div>

                  {/* Upcoming Events */}
                  <div>
                    <h2 className="text-3xl font-bold text-gray-900 mb-6">Upcoming Events</h2>
                    <div className="space-y-4">
                      <div className="bg-white border border-gray-200 rounded-lg p-6">
                        <div className="flex items-start justify-between">
                          <div>
                            <h3 className="font-semibold text-gray-900">Weekly Group Run</h3>
                            <p className="text-gray-600 text-sm">Saturday, 8:00 AM</p>
                            <p className="text-gray-500 text-sm">Central Park - North Meadow</p>
                          </div>
                          <div 
                            className="w-12 h-12 rounded-full flex items-center justify-center text-white text-sm font-semibold"
                            style={{ backgroundColor: 'var(--team-primary)' }}
                          >
                            Dec<br/>14
                          </div>
                        </div>
                      </div>

                      <div className="bg-white border border-gray-200 rounded-lg p-6">
                        <div className="flex items-start justify-between">
                          <div>
                            <h3 className="font-semibold text-gray-900">Holiday 5K Fun Run</h3>
                            <p className="text-gray-600 text-sm">Sunday, 9:00 AM</p>
                            <p className="text-gray-500 text-sm">Riverside Park</p>
                          </div>
                          <div 
                            className="w-12 h-12 rounded-full flex items-center justify-center text-white text-sm font-semibold"
                            style={{ backgroundColor: 'var(--team-primary)' }}
                          >
                            Dec<br/>22
                          </div>
                        </div>
                      </div>

                      <div className="bg-white border border-gray-200 rounded-lg p-6">
                        <div className="flex items-start justify-between">
                          <div>
                            <h3 className="font-semibold text-gray-900">New Year Resolution Run</h3>
                            <p className="text-gray-600 text-sm">Monday, 10:00 AM</p>
                            <p className="text-gray-500 text-sm">Brooklyn Bridge</p>
                          </div>
                          <div 
                            className="w-12 h-12 rounded-full flex items-center justify-center text-white text-sm font-semibold"
                            style={{ backgroundColor: 'var(--team-primary)' }}
                          >
                            Jan<br/>1
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </section>
          </div>
        </main>
      </div>
    </TeamThemeProvider>
  );
} 