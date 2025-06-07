'use client';

import { useState, useEffect, useRef } from 'react';
import { useRouter } from 'next/navigation';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { 
  faUsers, 
  faCalendarDays, 
  faRunning, 
  faComments, 
  faTrophy, 
  faClock,
  faUser,
  faChevronDown,
  faSignOutAlt,
  faBuilding
} from '@fortawesome/free-solid-svg-icons';
import { getUserFromToken, logout, isTokenExpired } from '@/utils/auth';

export default function Home() {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [userInfo, setUserInfo] = useState<{
    firstName: string;
    lastName: string;
    email: string;
  } | null>(null);
  const [isUserMenuOpen, setIsUserMenuOpen] = useState(false);
  const router = useRouter();
  const userMenuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    // Check authentication status on component mount
    const checkAuth = () => {
      const token = localStorage.getItem('token');
      if (token) {
        try {
          if (!isTokenExpired(token)) {
            setIsAuthenticated(true);
            const user = getUserFromToken();
            if (user) {
              setUserInfo(user);
            } else {
              // Fallback if we can't decode user info from token
              setUserInfo({
                firstName: 'User',
                lastName: '',
                email: 'user@example.com'
              });
            }
          } else {
            // Token is expired, clean up
            localStorage.removeItem('token');
            localStorage.removeItem('refreshToken');
            setIsAuthenticated(false);
          }
        } catch (error) {
          console.error('Error validating token:', error);
          // If validation fails, clean up potentially invalid tokens
          localStorage.removeItem('token');
          localStorage.removeItem('refreshToken');
          setIsAuthenticated(false);
        }
      } else {
        setIsAuthenticated(false);
      }
    };

    checkAuth();

    // Listen for storage changes (e.g., when user logs out in another tab)
    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === 'token') {
        checkAuth();
      }
    };

    window.addEventListener('storage', handleStorageChange);

    return () => {
      window.removeEventListener('storage', handleStorageChange);
    };
  }, []);

  useEffect(() => {
    // Close user menu when clicking outside
    function handleClickOutside(event: MouseEvent) {
      if (userMenuRef.current && !userMenuRef.current.contains(event.target as Node)) {
        setIsUserMenuOpen(false);
      }
    }

    if (isUserMenuOpen) {
      document.addEventListener('mousedown', handleClickOutside);
      return () => {
        document.removeEventListener('mousedown', handleClickOutside);
      };
    }
  }, [isUserMenuOpen]);

  const handleLoginClick = () => {
    router.push('/login');
  };

  const handleLogout = () => {
    logout();
  };

  const handleDashboard = () => {
    // Navigate to team or admin dashboard based on user's role
    // For now, we'll go to the team page
    router.push('/team');
    setIsUserMenuOpen(false);
  };

  const handleProfile = () => {
    // TODO: Implement profile navigation
    console.log('Navigate to profile');
    setIsUserMenuOpen(false);
  };

  const handlePlanSelect = () => {
    if (isAuthenticated) {
      // Navigate to team dashboard/settings for plan management
      router.push('/team');
    } else {
      // Navigate to login for non-authenticated users
      router.push('/login');
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
      {/* Header */}
      <header className="bg-white shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center py-4">
            <div className="flex items-center">
              <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center">
                <span className="text-white font-bold text-lg">T</span>
              </div>
              <span className="ml-2 text-xl font-bold text-gray-900">TeamStride</span>
            </div>
            
            {/* Authenticated User Menu or Login Button */}
            {isAuthenticated && userInfo ? (
              <div className="relative" ref={userMenuRef}>
                <button
                  onClick={() => setIsUserMenuOpen(!isUserMenuOpen)}
                  className="flex items-center space-x-2 text-gray-700 hover:text-gray-900 bg-gray-50 hover:bg-gray-100 px-3 py-2 rounded-lg transition-colors"
                >
                  <div className="w-8 h-8 bg-blue-600 rounded-full flex items-center justify-center">
                    <FontAwesomeIcon icon={faUser} className="text-white text-sm" />
                  </div>
                  <span className="font-medium">{userInfo.firstName}</span>
                  <FontAwesomeIcon icon={faChevronDown} className="w-4 h-4" />
                </button>
                
                {/* User Dropdown Menu */}
                {isUserMenuOpen && (
                  <div className="absolute right-0 mt-2 w-48 bg-white rounded-lg shadow-lg border border-gray-200 py-1 z-50">
                    <div className="px-4 py-2 border-b border-gray-200">
                      <p className="text-sm font-medium text-gray-900">{userInfo.firstName} {userInfo.lastName}</p>
                      <p className="text-xs text-gray-500">{userInfo.email}</p>
                    </div>
                    <button
                      onClick={handleDashboard}
                      className="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 flex items-center"
                    >
                      <FontAwesomeIcon icon={faBuilding} className="w-4 h-4 mr-3" />
                      Dashboard
                    </button>
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
            ) : (
              <button
                onClick={handleLoginClick}
                className="bg-blue-600 text-white px-4 py-2 rounded-lg font-medium hover:bg-blue-700 transition-colors"
              >
                Login
              </button>
            )}
          </div>
        </div>
      </header>

      {/* Hero Section */}
      <section className="relative overflow-hidden">
        {/* Background Elements */}
        <div className="absolute inset-0 bg-gradient-to-br from-blue-50 via-indigo-50 to-purple-50"></div>
        <div className="absolute top-0 left-0 w-full h-full">
          <div className="absolute top-20 left-10 w-72 h-72 bg-blue-200 rounded-full mix-blend-multiply filter blur-xl opacity-30 animate-pulse"></div>
          <div className="absolute top-40 right-10 w-72 h-72 bg-purple-200 rounded-full mix-blend-multiply filter blur-xl opacity-30 animate-pulse animation-delay-2000"></div>
          <div className="absolute -bottom-8 left-20 w-72 h-72 bg-indigo-200 rounded-full mix-blend-multiply filter blur-xl opacity-30 animate-pulse animation-delay-4000"></div>
        </div>
        
        {/* Content */}
        <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-20 sm:py-32">
          <div className="text-center">
            {/* Main Headline */}
            <h1 className="text-5xl sm:text-6xl lg:text-7xl font-extrabold text-gray-900 mb-6 leading-tight">
              <span className="block animate-slide-up">Running Team</span>
              <span className="block bg-gradient-to-r from-blue-600 via-purple-600 to-indigo-600 bg-clip-text text-transparent animate-slide-up animation-delay-200">
                Management
              </span>
              <span className="block text-4xl sm:text-5xl lg:text-6xl mt-2 animate-slide-up animation-delay-400">
                Made Simple
              </span>
            </h1>
            
            {/* Subtitle */}
            <p className="text-xl sm:text-2xl text-gray-600 mb-10 max-w-4xl mx-auto leading-relaxed animate-fade-in animation-delay-600">
              Empower coaches to efficiently manage rosters, schedules, training plans, 
              communications, and more. Built mobile-first for coaches on the go.
            </p>
            
            {/* CTA Buttons */}
            <div className="flex flex-col sm:flex-row gap-4 justify-center mb-12 animate-fade-in animation-delay-800">
              {isAuthenticated ? (
                // Authenticated user buttons
                <>
                  <button 
                    onClick={handleDashboard}
                    className="group bg-gradient-to-r from-blue-600 to-indigo-600 text-white px-10 py-5 rounded-xl text-lg font-bold hover:from-blue-700 hover:to-indigo-700 transition-all duration-300 transform hover:scale-105 hover:shadow-xl"
                  >
                    <span className="flex items-center justify-center">
                      Go to Dashboard
                      <svg className="w-5 h-5 ml-2 group-hover:translate-x-1 transition-transform" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 7l5 5m0 0l-5 5m5-5H6" />
                      </svg>
                    </span>
                  </button>
                  <button className="group border-2 border-gray-300 text-gray-700 px-10 py-5 rounded-xl text-lg font-bold hover:border-blue-500 hover:text-blue-600 transition-all duration-300 transform hover:scale-105 hover:shadow-lg bg-white/80 backdrop-blur-sm">
                    <span className="flex items-center justify-center">
                      <svg className="w-5 h-5 mr-2 group-hover:scale-110 transition-transform" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M14.828 14.828a4 4 0 01-5.656 0M9 10h1m4 0h1m-6 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                      </svg>
                      Watch Demo
                    </span>
                  </button>
                </>
              ) : (
                // Non-authenticated user buttons
                <>
                  <button 
                    onClick={handleLoginClick}
                    className="group bg-gradient-to-r from-blue-600 to-indigo-600 text-white px-10 py-5 rounded-xl text-lg font-bold hover:from-blue-700 hover:to-indigo-700 transition-all duration-300 transform hover:scale-105 hover:shadow-xl"
                  >
                    <span className="flex items-center justify-center">
                      Start Your Free Team
                      <svg className="w-5 h-5 ml-2 group-hover:translate-x-1 transition-transform" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 7l5 5m0 0l-5 5m5-5H6" />
                      </svg>
                    </span>
                  </button>
                  <button className="group border-2 border-gray-300 text-gray-700 px-10 py-5 rounded-xl text-lg font-bold hover:border-blue-500 hover:text-blue-600 transition-all duration-300 transform hover:scale-105 hover:shadow-lg bg-white/80 backdrop-blur-sm">
                    <span className="flex items-center justify-center">
                      <svg className="w-5 h-5 mr-2 group-hover:scale-110 transition-transform" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M14.828 14.828a4 4 0 01-5.656 0M9 10h1m4 0h1m-6 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                      </svg>
                      Watch Demo
                    </span>
                  </button>
                </>
              )}
            </div>
            
            {/* Social Proof */}
            <div className="flex flex-col sm:flex-row items-center justify-center gap-8 text-sm text-gray-500 animate-fade-in animation-delay-1000">
              <div className="flex items-center">
                <div className="flex -space-x-2 mr-3">
                  <div className="w-8 h-8 bg-gradient-to-r from-blue-400 to-blue-600 rounded-full border-2 border-white"></div>
                  <div className="w-8 h-8 bg-gradient-to-r from-purple-400 to-purple-600 rounded-full border-2 border-white"></div>
                  <div className="w-8 h-8 bg-gradient-to-r from-indigo-400 to-indigo-600 rounded-full border-2 border-white"></div>
                  <div className="w-8 h-8 bg-gradient-to-r from-green-400 to-green-600 rounded-full border-2 border-white"></div>
                </div>
                <span>Join 2,500+ coaches</span>
              </div>
              <div className="flex items-center">
                <span className="text-yellow-400 mr-1">★★★★★</span>
                <span>4.9/5 from 200+ reviews</span>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section className="bg-white py-16">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-12">
            <h2 className="text-3xl sm:text-4xl font-bold text-gray-900 mb-4">
              Everything You Need to Manage Your Team
            </h2>
            <p className="text-lg text-gray-600">
              From roster management to race results, TeamStride has you covered
            </p>
          </div>
          
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {/* Roster Management Card */}
            <div className="bg-white rounded-lg border border-gray-200 shadow-sm hover:shadow-md transition-shadow duration-300 p-6 text-center">
              <div className="w-12 h-12 bg-blue-100 rounded-lg flex items-center justify-center mx-auto mb-4">
                <FontAwesomeIcon icon={faUsers} className="text-blue-600 text-xl" />
              </div>
              <h3 className="text-xl font-semibold text-gray-900 mb-3">Roster Management</h3>
              <p className="text-gray-600 leading-relaxed">Add, edit, and organize athlete profiles with role assignments and contact information.</p>
            </div>
            
            {/* Smart Scheduling Card */}
            <div className="bg-white rounded-lg border border-gray-200 shadow-sm hover:shadow-md transition-shadow duration-300 p-6 text-center">
              <div className="w-12 h-12 bg-blue-100 rounded-lg flex items-center justify-center mx-auto mb-4">
                <FontAwesomeIcon icon={faCalendarDays} className="text-blue-600 text-xl" />
              </div>
              <h3 className="text-xl font-semibold text-gray-900 mb-3">Smart Scheduling</h3>
              <p className="text-gray-600 leading-relaxed">Manage practices and races with conflict detection and automated notifications.</p>
            </div>
            
            {/* Training Plans Card */}
            <div className="bg-white rounded-lg border border-gray-200 shadow-sm hover:shadow-md transition-shadow duration-300 p-6 text-center">
              <div className="w-12 h-12 bg-blue-100 rounded-lg flex items-center justify-center mx-auto mb-4">
                <FontAwesomeIcon icon={faRunning} className="text-blue-600 text-xl" />
              </div>
              <h3 className="text-xl font-semibold text-gray-900 mb-3">Training Plans</h3>
              <p className="text-gray-600 leading-relaxed">Create team-wide or individual training plans with goal tracking and progress monitoring.</p>
            </div>
            
            {/* Team Communication Card */}
            <div className="bg-white rounded-lg border border-gray-200 shadow-sm hover:shadow-md transition-shadow duration-300 p-6 text-center">
              <div className="w-12 h-12 bg-blue-100 rounded-lg flex items-center justify-center mx-auto mb-4">
                <FontAwesomeIcon icon={faComments} className="text-blue-600 text-xl" />
              </div>
              <h3 className="text-xl font-semibold text-gray-900 mb-3">Team Communication</h3>
              <p className="text-gray-600 leading-relaxed">Send messages via email and SMS to teams, groups, or individuals with read receipts.</p>
            </div>
            
            {/* Race Results Card */}
            <div className="bg-white rounded-lg border border-gray-200 shadow-sm hover:shadow-md transition-shadow duration-300 p-6 text-center">
              <div className="w-12 h-12 bg-blue-100 rounded-lg flex items-center justify-center mx-auto mb-4">
                <FontAwesomeIcon icon={faTrophy} className="text-blue-600 text-xl" />
              </div>
              <h3 className="text-xl font-semibold text-gray-900 mb-3">Race Results</h3>
              <p className="text-gray-600 leading-relaxed">Track race results, personal records, and team statistics with MileSplit integration.</p>
            </div>
            
            {/* Garmin Integration Card */}
            <div className="bg-white rounded-lg border border-gray-200 shadow-sm hover:shadow-md transition-shadow duration-300 p-6 text-center">
              <div className="w-12 h-12 bg-blue-100 rounded-lg flex items-center justify-center mx-auto mb-4">
                <FontAwesomeIcon icon={faClock} className="text-blue-600 text-xl" />
              </div>
              <h3 className="text-xl font-semibold text-gray-900 mb-3">Garmin Integration</h3>
              <p className="text-gray-600 leading-relaxed">Automatically sync workout data including mileage, pace, and heart rate from Garmin devices.</p>
            </div>
          </div>
        </div>
      </section>

      {/* Pricing Section */}
      <section className="py-16 bg-gray-50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-12">
            <h2 className="text-3xl sm:text-4xl font-bold text-gray-900 mb-4">
              Choose Your Plan
            </h2>
            <p className="text-lg text-gray-600">
              Start free and upgrade as your team grows
            </p>
          </div>
          
          <div className="grid grid-cols-1 md:grid-cols-3 gap-8 max-w-5xl mx-auto">
            {/* Free Plan */}
            <div className="bg-white rounded-lg shadow-lg p-8 border-2 border-gray-200 flex flex-col">
              <div className="text-center">
                <h3 className="text-2xl font-bold text-gray-900 mb-2">Free</h3>
                <div className="text-4xl font-bold text-gray-900 mb-4">$0<span className="text-lg text-gray-600">/month</span></div>
                <p className="text-gray-600 mb-6">Perfect for small teams getting started</p>
              </div>
              
              <ul className="space-y-3 mb-8 flex-grow">
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Up to 7 athletes</span>
                </li>
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Basic team setup</span>
                </li>
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Manual data entry</span>
                </li>
                <li className="flex items-center">
                  <span className="text-yellow-500 mr-3">⚠</span>
                  <span className="text-gray-700">Demo messaging only</span>
                </li>
              </ul>
              
              <button 
                onClick={handlePlanSelect}
                className="w-full bg-gray-600 text-white py-3 rounded-lg font-semibold hover:bg-gray-700 transition-colors mt-auto"
              >
                {isAuthenticated ? 'Manage Plan' : 'Get Started Free'}
              </button>
            </div>
            
            {/* Standard Plan */}
            <div className="bg-white rounded-lg shadow-lg p-8 border-2 border-blue-500 flex flex-col relative">
              <div className="absolute -top-4 left-1/2 transform -translate-x-1/2">
                <span className="bg-blue-500 text-white px-4 py-1 rounded-full text-sm font-medium">Most Popular</span>
              </div>
              <div className="text-center">
                <h3 className="text-2xl font-bold text-gray-900 mb-2">Standard</h3>
                <div className="text-4xl font-bold text-gray-900 mb-4">$39<span className="text-lg text-gray-600">/year</span></div>
                <p className="text-gray-600 mb-6">Great for growing teams</p>
              </div>
              
              <ul className="space-y-3 mb-8 flex-grow">
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Up to 35 athletes</span>
                </li>
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Full team management</span>
                </li>
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Email & SMS messaging</span>
                </li>
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Garmin integration</span>
                </li>
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Priority support</span>
                </li>
              </ul>
              
              <button 
                onClick={handlePlanSelect}
                className="w-full bg-blue-600 text-white py-3 rounded-lg font-semibold hover:bg-blue-700 transition-colors mt-auto"
              >
                {isAuthenticated ? 'Upgrade to Standard' : 'Start Standard Plan'}
              </button>
            </div>
            
            {/* Pro Plan */}
            <div className="bg-white rounded-lg shadow-lg p-8 border-2 border-gray-200 flex flex-col">
              <div className="text-center">
                <h3 className="text-2xl font-bold text-gray-900 mb-2">Pro</h3>
                <div className="text-4xl font-bold text-gray-900 mb-4">$79<span className="text-lg text-gray-600">/year</span></div>
                <p className="text-gray-600 mb-6">For large teams and programs</p>
              </div>
              
              <ul className="space-y-3 mb-8 flex-grow">
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Unlimited athletes</span>
                </li>
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Everything in Standard</span>
                </li>
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Advanced analytics</span>
                </li>
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Custom branding</span>
                </li>
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">API access</span>
                </li>
              </ul>
              
              <button 
                onClick={handlePlanSelect}
                className="w-full bg-indigo-600 text-white py-3 rounded-lg font-semibold hover:bg-indigo-700 transition-colors mt-auto"
              >
                {isAuthenticated ? 'Upgrade to Pro' : 'Start Pro Plan'}
              </button>
            </div>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="bg-gray-900 text-white py-12">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-1 md:grid-cols-4 gap-8">
            <div className="col-span-1 md:col-span-2">
              <div className="flex items-center mb-4">
                <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center">
                  <span className="text-white font-bold text-lg">T</span>
                </div>
                <span className="ml-2 text-xl font-bold">TeamStride</span>
              </div>
              <p className="text-gray-400 mb-4 max-w-md">
                The modern platform for running team management. Built by coaches, for coaches.
              </p>
              <div className="flex space-x-4">
                <a href="#" className="text-gray-400 hover:text-white transition-colors">
                  <span className="sr-only">Twitter</span>
                  <svg className="h-6 w-6" fill="currentColor" viewBox="0 0 24 24">
                    <path d="M8.29 20.251c7.547 0 11.675-6.253 11.675-11.675 0-.178 0-.355-.012-.53A8.348 8.348 0 0022 5.92a8.19 8.19 0 01-2.357.646 4.118 4.118 0 001.804-2.27 8.224 8.224 0 01-2.605.996 4.107 4.107 0 00-6.993 3.743 11.65 11.65 0 01-8.457-4.287 4.106 4.106 0 001.27 5.477A4.072 4.072 0 012.8 9.713v.052a4.105 4.105 0 003.292 4.022 4.095 4.095 0 01-1.853.07 4.108 4.108 0 003.834 2.85A8.233 8.233 0 012 18.407a11.616 11.616 0 006.29 1.84" />
                  </svg>
                </a>
                <a href="#" className="text-gray-400 hover:text-white transition-colors">
                  <span className="sr-only">GitHub</span>
                  <svg className="h-6 w-6" fill="currentColor" viewBox="0 0 24 24">
                    <path fillRule="evenodd" d="M12 2C6.477 2 2 6.484 2 12.017c0 4.425 2.865 8.18 6.839 9.504.5.092.682-.217.682-.483 0-.237-.008-.868-.013-1.703-2.782.605-3.369-1.343-3.369-1.343-.454-1.158-1.11-1.466-1.11-1.466-.908-.62.069-.608.069-.608 1.003.07 1.531 1.032 1.531 1.032.892 1.53 2.341 1.088 2.91.832.092-.647.35-1.088.636-1.338-2.22-.253-4.555-1.113-4.555-4.951 0-1.093.39-1.988 1.029-2.688-.103-.253-.446-1.272.098-2.65 0 0 .84-.27 2.75 1.026A9.564 9.564 0 0112 6.844c.85.004 1.705.115 2.504.337 1.909-1.296 2.747-1.027 2.747-1.027.546 1.379.202 2.398.1 2.651.64.7 1.028 1.595 1.028 2.688 0 3.848-2.339 4.695-4.566 4.943.359.309.678.92.678 1.855 0 1.338-.012 2.419-.012 2.747 0 .268.18.58.688.482A10.019 10.019 0 0022 12.017C22 6.484 17.522 2 12 2z" clipRule="evenodd" />
                  </svg>
                </a>
                <a href="#" className="text-gray-400 hover:text-white transition-colors">
                  <span className="sr-only">LinkedIn</span>
                  <svg className="h-6 w-6" fill="currentColor" viewBox="0 0 24 24">
                    <path fillRule="evenodd" d="M20.447 20.452h-3.554v-5.569c0-1.328-.027-3.037-1.852-3.037-1.853 0-2.136 1.445-2.136 2.939v5.667H9.351V9h3.414v1.561h.046c.477-.9 1.637-1.85 3.37-1.85 3.601 0 4.267 2.37 4.267 5.455v6.286zM5.337 7.433c-1.144 0-2.063-.926-2.063-2.065 0-1.138.92-2.063 2.063-2.063 1.14 0 2.064.925 2.064 2.063 0 1.139-.925 2.065-2.064 2.065zm1.782 13.019H3.555V9h3.564v11.452zM22.225 0H1.771C.792 0 0 .774 0 1.729v20.542C0 23.227.792 24 1.771 24h20.451C23.2 24 24 23.227 24 22.271V1.729C24 .774 23.2 0 22.222 0h.003z" clipRule="evenodd" />
                  </svg>
                </a>
              </div>
            </div>
            
            <div>
              <h3 className="text-sm font-semibold text-gray-200 tracking-wider uppercase mb-4">
                Product
              </h3>
              <ul className="space-y-3">
                <li><a href="#" className="text-gray-400 hover:text-white transition-colors">Features</a></li>
                <li><a href="#" className="text-gray-400 hover:text-white transition-colors">Pricing</a></li>
                <li><a href="#" className="text-gray-400 hover:text-white transition-colors">Integrations</a></li>
                <li><a href="#" className="text-gray-400 hover:text-white transition-colors">API</a></li>
              </ul>
            </div>
            
            <div>
              <h3 className="text-sm font-semibold text-gray-200 tracking-wider uppercase mb-4">
                Support
              </h3>
              <ul className="space-y-3">
                <li><a href="#" className="text-gray-400 hover:text-white transition-colors">Documentation</a></li>
                <li><a href="#" className="text-gray-400 hover:text-white transition-colors">Help Center</a></li>
                <li><a href="#" className="text-gray-400 hover:text-white transition-colors">Contact Us</a></li>
                <li><a href="#" className="text-gray-400 hover:text-white transition-colors">Status</a></li>
              </ul>
            </div>
          </div>
          
          <div className="mt-8 pt-8 border-t border-gray-800">
            <p className="text-center text-gray-400 text-sm">
              © 2024 TeamStride. All rights reserved.
            </p>
          </div>
        </div>
      </footer>
    </div>
  );
}
