'use client';

// No React hooks needed - using AuthContext
import { useRouter } from 'next/navigation';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { 
  faUsers, 
  faCalendarDays, 
  faRunning, 
  faComments, 
  faTrophy, 
  faClock
} from '@fortawesome/free-solid-svg-icons';
import UserContextMenu from '@/components/shared/user-context-menu';
import HeroSection from '@/components/shared/hero-section';
import { useAuthTokenRefresh } from '@/hooks/use-auth-token-refresh';
import { useUser, useTenant } from '@/contexts/auth-context';

export default function SiteHomePage() {
  const router = useRouter();

  // Use centralized auth state
  const { user, isAuthenticated } = useUser();
  const { hasTeam, isGlobalAdmin } = useTenant();

  // Auto-refresh token when subdomain context changes
  useAuthTokenRefresh();

  // No longer need useEffect for auth state - managed by AuthContext

  const handleLoginClick = () => {
    router.push('/login');
  };

  const handlePlanSelect = (tier?: string) => {
    if (isAuthenticated) {
      // Navigate to team dashboard/settings for plan management
      router.push('/team');
    } else {
      // Navigate to registration for non-authenticated users with tier parameter
      const tierParam = tier ? `?tier=${tier}` : '';
      router.push(`/register${tierParam}`);
    }
  };

  const handleFreePlanClick = () => handlePlanSelect('free');
  const handleStandardPlanClick = () => handlePlanSelect('standard');
  const handlePremiumPlanClick = () => handlePlanSelect('premium');
  const handleCreateNewTeamClick = () => handlePlanSelect('standard');

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
            {isAuthenticated && user ? (
              <UserContextMenu />
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
      <HeroSection
        onLoginClick={handleLoginClick}
        onPrimaryAction={isAuthenticated ? () => {
          // Navigate to appropriate dashboard based on user context
          if (hasTeam) {
            router.push('/team');
          } else if (isGlobalAdmin) {
            router.push('/admin');
          } else {
            router.push('/team'); // fallback
          }
        } : handleFreePlanClick}
        primaryButtonText={isAuthenticated ? "Go to Dashboard" : "Start Your Free Team"}
        showSecondaryButton={!isAuthenticated}
        onSecondaryAction={() => {
          router.push('/join-team');
        }}
        secondaryButtonText="Join Existing Team"
      />

      {/* Quick Start Guide for New Users */}
      {!isAuthenticated && (
        <section className="py-12 bg-white">
          <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
            <div className="text-center mb-8">
              <h2 className="text-2xl font-bold text-gray-900 mb-2">
                Get Started in 2 Easy Ways
              </h2>
              <p className="text-gray-600">
                Choose the option that best fits your situation
              </p>
            </div>
            
            <div className="grid md:grid-cols-2 gap-8">
              {/* Create Team Option */}
              <div className="bg-gradient-to-br from-blue-50 to-blue-100 rounded-lg p-6 border border-blue-200 flex flex-col">
                <div className="flex items-center mb-4">
                  <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center">
                    <svg className="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
                    </svg>
                  </div>
                  <h3 className="text-lg font-semibold text-gray-900 ml-3">
                    I&apos;m a Coach/Administrator
                  </h3>
                </div>
                <p className="text-gray-600 mb-4 flex-grow">
                  Create a new team, select your plan, and invite athletes to join your program.
                </p>
                <button
                  onClick={handleCreateNewTeamClick}
                  className="w-full bg-blue-600 text-white py-3 px-4 rounded-lg font-medium hover:bg-blue-700 transition-colors mt-auto"
                >
                  Create New Team
                </button>
              </div>

              {/* Join Team Option */}
              <div className="bg-gradient-to-br from-green-50 to-green-100 rounded-lg p-6 border border-green-200 flex flex-col">
                <div className="flex items-center mb-4">
                  <div className="w-8 h-8 bg-green-600 rounded-lg flex items-center justify-center">
                    <svg className="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197m13.5-9a2.5 2.5 0 11-5 0 2.5 2.5 0 015 0z" />
                    </svg>
                  </div>
                  <h3 className="text-lg font-semibold text-gray-900 ml-3">
                    I&apos;m an Athlete/Parent
                  </h3>
                </div>
                <p className="text-gray-600 mb-4 flex-grow">
                  Find your team and register for the current season. You&apos;ll need your team&apos;s registration code.
                </p>
                <button
                  onClick={() => router.push('/join-team')}
                  className="w-full bg-green-600 text-white py-3 px-4 rounded-lg font-medium hover:bg-green-700 transition-colors mt-auto"
                >
                  Join Existing Team
                </button>
              </div>
            </div>

            <div className="mt-8 text-center">
              <p className="text-sm text-gray-500">
                Already have an account?{' '}
                <button
                  onClick={handleLoginClick}
                  className="text-blue-600 hover:text-blue-500 font-medium"
                >
                  Sign in here
                </button>
              </p>
            </div>
          </div>
        </section>
      )}

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
                onClick={handleFreePlanClick}
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
                onClick={handleStandardPlanClick}
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
                onClick={handlePremiumPlanClick}
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