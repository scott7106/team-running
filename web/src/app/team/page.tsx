'use client';

import { useState, useEffect } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { 
  faUsers, 
  faCalendarDays, 
  faRunning, 
  faComments, 
  faTrophy, 
  faShirt,
  faSignOutAlt,
  faBars,
  faTimes,
  faUserShield,
  faCog,
  faChartLine,
  faMapMarkerAlt
} from '@fortawesome/free-solid-svg-icons';

interface TeamInfo {
  teamId: string;
  teamName: string;
  subdomain: string;
  primaryColor: string;
  secondaryColor: string;
}

export default function TeamHomePage() {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [teamInfo, setTeamInfo] = useState<TeamInfo | null>(null);
  const [userInfo, setUserInfo] = useState<{
    firstName: string;
    lastName: string;
    email: string;
    role: string;
  } | null>(null);
  const router = useRouter();
  const searchParams = useSearchParams();

  useEffect(() => {
    // Check if user is authenticated
    const token = localStorage.getItem('token');
    if (!token) {
      router.push('/');
      return;
    }

    // Get subdomain from URL params for development
    const subdomain = searchParams.get('subdomain');
    
    // TODO: Add API call to get team info and user info
    // For now, we'll set placeholder data
    setTeamInfo({
      teamId: 'team-123',
      teamName: subdomain ? `${subdomain.charAt(0).toUpperCase() + subdomain.slice(1)} Running Club` : 'Sample Running Club',
      subdomain: subdomain || 'sample',
      primaryColor: '#2563eb',
      secondaryColor: '#1e40af'
    });

    setUserInfo({
      firstName: 'Coach',
      lastName: 'Smith',
      email: 'coach@example.com',
      role: 'Team Owner'
    });
  }, [router, searchParams]);

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    router.push('/');
  };

  const handleMenuClick = (section: string) => {
    // TODO: Implement navigation to different sections
    console.log(`Navigate to ${section}`);
  };

  if (!teamInfo || !userInfo) {
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
        
        {/* Team header */}
        <div className="flex items-center justify-center h-16 px-4 bg-gray-800">
          <div 
            className="w-8 h-8 rounded-lg flex items-center justify-center"
            style={{ backgroundColor: teamInfo.primaryColor }}
          >
            <span className="text-white font-bold text-lg">
              {teamInfo.teamName.charAt(0)}
            </span>
          </div>
          <span className="ml-2 text-lg font-bold text-white truncate">{teamInfo.teamName}</span>
          <button
            onClick={() => setIsSidebarOpen(false)}
            className="ml-auto text-gray-400 hover:text-white lg:hidden"
          >
            <FontAwesomeIcon icon={faTimes} />
          </button>
        </div>

        {/* Navigation menu */}
        <nav className="mt-8">
          <div className="px-4 space-y-2">
            <button
              onClick={() => handleMenuClick('roster')}
              className="w-full flex items-center px-4 py-3 text-gray-300 hover:bg-gray-700 hover:text-white rounded-lg transition-colors text-left"
            >
              <FontAwesomeIcon icon={faUsers} className="w-5 h-5 mr-3" />
              <span>Roster</span>
            </button>
            
            <button
              onClick={() => handleMenuClick('practices')}
              className="w-full flex items-center px-4 py-3 text-gray-300 hover:bg-gray-700 hover:text-white rounded-lg transition-colors text-left"
            >
              <FontAwesomeIcon icon={faCalendarDays} className="w-5 h-5 mr-3" />
              <span>Practices</span>
            </button>
            
            <button
              onClick={() => handleMenuClick('races')}
              className="w-full flex items-center px-4 py-3 text-gray-300 hover:bg-gray-700 hover:text-white rounded-lg transition-colors text-left"
            >
              <FontAwesomeIcon icon={faTrophy} className="w-5 h-5 mr-3" />
              <span>Races</span>
            </button>
            
            <button
              onClick={() => handleMenuClick('training')}
              className="w-full flex items-center px-4 py-3 text-gray-300 hover:bg-gray-700 hover:text-white rounded-lg transition-colors text-left"
            >
              <FontAwesomeIcon icon={faRunning} className="w-5 h-5 mr-3" />
              <span>Training Plans</span>
            </button>
            
            <button
              onClick={() => handleMenuClick('uniforms')}
              className="w-full flex items-center px-4 py-3 text-gray-300 hover:bg-gray-700 hover:text-white rounded-lg transition-colors text-left"
            >
              <FontAwesomeIcon icon={faShirt} className="w-5 h-5 mr-3" />
              <span>Uniforms</span>
            </button>
            
            <button
              onClick={() => handleMenuClick('messages')}
              className="w-full flex items-center px-4 py-3 text-gray-300 hover:bg-gray-700 hover:text-white rounded-lg transition-colors text-left"
            >
              <FontAwesomeIcon icon={faComments} className="w-5 h-5 mr-3" />
              <span>Messages</span>
            </button>
            
            <button
              onClick={() => handleMenuClick('analytics')}
              className="w-full flex items-center px-4 py-3 text-gray-300 hover:bg-gray-700 hover:text-white rounded-lg transition-colors text-left"
            >
              <FontAwesomeIcon icon={faChartLine} className="w-5 h-5 mr-3" />
              <span>Analytics</span>
            </button>
            
            <button
              onClick={() => handleMenuClick('locations')}
              className="w-full flex items-center px-4 py-3 text-gray-300 hover:bg-gray-700 hover:text-white rounded-lg transition-colors text-left"
            >
              <FontAwesomeIcon icon={faMapMarkerAlt} className="w-5 h-5 mr-3" />
              <span>Locations</span>
            </button>
          </div>

          {/* Team Owner specific section */}
          {userInfo.role === 'Team Owner' && (
            <div className="mt-8 px-4">
              <div className="text-xs font-semibold text-gray-400 uppercase tracking-wider mb-3">
                Team Settings
              </div>
              <div className="space-y-2">
                <button
                  onClick={() => handleMenuClick('settings')}
                  className="w-full flex items-center px-4 py-3 text-gray-300 hover:bg-gray-700 hover:text-white rounded-lg transition-colors text-left"
                >
                  <FontAwesomeIcon icon={faCog} className="w-5 h-5 mr-3" />
                  <span>Settings</span>
                </button>
              </div>
            </div>
          )}
        </nav>

        {/* User info and logout */}
        <div className="absolute bottom-0 w-full p-4 border-t border-gray-700">
          <div className="flex items-center mb-3">
            <div className="w-8 h-8 bg-blue-600 rounded-full flex items-center justify-center">
              <FontAwesomeIcon icon={faUserShield} className="text-white text-sm" />
            </div>
            <div className="ml-3 text-sm">
              <p className="text-white font-medium">{userInfo.firstName} {userInfo.lastName}</p>
              <p className="text-gray-400 text-xs">{userInfo.role}</p>
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
            <h1 className="text-xl font-semibold text-gray-900">{teamInfo.teamName}</h1>
            <div className="hidden lg:flex items-center space-x-4">
              <span className="text-sm text-gray-600">Welcome, {userInfo.firstName}</span>
            </div>
          </div>
        </div>

        {/* Page content */}
        <div className="flex-1 p-6 lg:overflow-y-auto">
          <div className="max-w-6xl mx-auto">
            <div className="mb-8">
              <h2 className="text-2xl font-bold text-gray-900 mb-2">Team Dashboard</h2>
              <p className="text-gray-600">
                Manage your team&apos;s activities, members, and performance.
              </p>
            </div>

            {/* Quick action cards */}
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
              <div 
                onClick={() => handleMenuClick('roster')}
                className="bg-white rounded-lg shadow-sm border border-gray-200 hover:shadow-md transition-shadow duration-300 cursor-pointer group"
              >
                <div className="p-6 text-center">
                  <div className="w-12 h-12 bg-blue-100 rounded-lg flex items-center justify-center mx-auto mb-4 group-hover:bg-blue-200 transition-colors">
                    <FontAwesomeIcon icon={faUsers} className="text-blue-600 text-xl" />
                  </div>
                  <h3 className="text-lg font-semibold text-gray-900 mb-2">Roster</h3>
                  <p className="text-sm text-gray-600">Manage team members</p>
                </div>
              </div>

              <div 
                onClick={() => handleMenuClick('practices')}
                className="bg-white rounded-lg shadow-sm border border-gray-200 hover:shadow-md transition-shadow duration-300 cursor-pointer group"
              >
                <div className="p-6 text-center">
                  <div className="w-12 h-12 bg-green-100 rounded-lg flex items-center justify-center mx-auto mb-4 group-hover:bg-green-200 transition-colors">
                    <FontAwesomeIcon icon={faCalendarDays} className="text-green-600 text-xl" />
                  </div>
                  <h3 className="text-lg font-semibold text-gray-900 mb-2">Practices</h3>
                  <p className="text-sm text-gray-600">Schedule training</p>
                </div>
              </div>

              <div 
                onClick={() => handleMenuClick('races')}
                className="bg-white rounded-lg shadow-sm border border-gray-200 hover:shadow-md transition-shadow duration-300 cursor-pointer group"
              >
                <div className="p-6 text-center">
                  <div className="w-12 h-12 bg-yellow-100 rounded-lg flex items-center justify-center mx-auto mb-4 group-hover:bg-yellow-200 transition-colors">
                    <FontAwesomeIcon icon={faTrophy} className="text-yellow-600 text-xl" />
                  </div>
                  <h3 className="text-lg font-semibold text-gray-900 mb-2">Races</h3>
                  <p className="text-sm text-gray-600">Track competitions</p>
                </div>
              </div>

              <div 
                onClick={() => handleMenuClick('messages')}
                className="bg-white rounded-lg shadow-sm border border-gray-200 hover:shadow-md transition-shadow duration-300 cursor-pointer group"
              >
                <div className="p-6 text-center">
                  <div className="w-12 h-12 bg-purple-100 rounded-lg flex items-center justify-center mx-auto mb-4 group-hover:bg-purple-200 transition-colors">
                    <FontAwesomeIcon icon={faComments} className="text-purple-600 text-xl" />
                  </div>
                  <h3 className="text-lg font-semibold text-gray-900 mb-2">Messages</h3>
                  <p className="text-sm text-gray-600">Team communication</p>
                </div>
              </div>
            </div>

            {/* Recent activity and stats */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {/* Recent Activity */}
              <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
                <h3 className="text-lg font-semibold text-gray-900 mb-4">Recent Activity</h3>
                <div className="space-y-4">
                  <div className="flex items-center space-x-3">
                    <div className="w-8 h-8 bg-blue-100 rounded-full flex items-center justify-center">
                      <FontAwesomeIcon icon={faUsers} className="text-blue-600 text-sm" />
                    </div>
                    <div className="flex-1">
                      <p className="text-sm font-medium text-gray-900">New member added</p>
                      <p className="text-xs text-gray-500">Sarah Johnson joined the team</p>
                    </div>
                    <span className="text-xs text-gray-400">2h ago</span>
                  </div>
                  
                  <div className="flex items-center space-x-3">
                    <div className="w-8 h-8 bg-green-100 rounded-full flex items-center justify-center">
                      <FontAwesomeIcon icon={faCalendarDays} className="text-green-600 text-sm" />
                    </div>
                    <div className="flex-1">
                      <p className="text-sm font-medium text-gray-900">Practice scheduled</p>
                      <p className="text-xs text-gray-500">Wednesday morning workout</p>
                    </div>
                    <span className="text-xs text-gray-400">5h ago</span>
                  </div>
                  
                  <div className="flex items-center space-x-3">
                    <div className="w-8 h-8 bg-yellow-100 rounded-full flex items-center justify-center">
                      <FontAwesomeIcon icon={faTrophy} className="text-yellow-600 text-sm" />
                    </div>
                    <div className="flex-1">
                      <p className="text-sm font-medium text-gray-900">Race results updated</p>
                      <p className="text-xs text-gray-500">City Marathon results</p>
                    </div>
                    <span className="text-xs text-gray-400">1d ago</span>
                  </div>
                </div>
              </div>

              {/* Team Stats */}
              <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
                <h3 className="text-lg font-semibold text-gray-900 mb-4">Team Statistics</h3>
                <div className="grid grid-cols-2 gap-4">
                  <div className="text-center">
                    <p className="text-2xl font-bold text-blue-600">24</p>
                    <p className="text-sm text-gray-600">Active Members</p>
                  </div>
                  <div className="text-center">
                    <p className="text-2xl font-bold text-green-600">8</p>
                    <p className="text-sm text-gray-600">Upcoming Races</p>
                  </div>
                  <div className="text-center">
                    <p className="text-2xl font-bold text-yellow-600">156</p>
                    <p className="text-sm text-gray-600">Miles This Week</p>
                  </div>
                  <div className="text-center">
                    <p className="text-2xl font-bold text-purple-600">3</p>
                    <p className="text-sm text-gray-600">Practice Sessions</p>
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