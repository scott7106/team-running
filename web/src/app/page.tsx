'use client';

import { useState } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { 
  faUsers, 
  faCalendarDays, 
  faRunning, 
  faComments, 
  faTrophy, 
  faClock 
} from '@fortawesome/free-solid-svg-icons';

export default function Home() {
  const [showLogin, setShowLogin] = useState(false);

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
            <button
              onClick={() => setShowLogin(!showLogin)}
              className="bg-blue-600 text-white px-4 py-2 rounded-lg font-medium hover:bg-blue-700 transition-colors"
            >
              Login
            </button>
          </div>
        </div>
      </header>

      {/* Login Modal */}
      {showLogin && (
        <div className="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-lg p-6 w-full max-w-md">
            <div className="flex justify-between items-center mb-4">
              <h2 className="text-xl font-bold text-gray-900">Login to TeamStride</h2>
              <button
                onClick={() => setShowLogin(false)}
                className="text-gray-400 hover:text-gray-600"
              >
                ✕
              </button>
            </div>
            
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Team Subdomain
                </label>
                <div className="flex">
                  <input
                    type="text"
                    placeholder="yourteam"
                    className="flex-1 px-3 py-2 border border-gray-300 rounded-l-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                  <span className="px-3 py-2 bg-gray-100 border border-l-0 border-gray-300 rounded-r-lg text-sm text-gray-600">
                    .teamstride.com
                  </span>
                </div>
              </div>
              
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Email
                </label>
                <input
                  type="email"
                  placeholder="coach@example.com"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
              
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Password
                </label>
                <input
                  type="password"
                  placeholder="••••••••"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
              
              <button className="w-full bg-blue-600 text-white py-2 rounded-lg font-medium hover:bg-blue-700 transition-colors">
                Sign In
              </button>
              
              <div className="text-center text-sm text-gray-600">
                Or sign in with
              </div>
              
              <div className="grid grid-cols-2 gap-3">
                <button className="flex items-center justify-center px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors">
                  <span className="text-sm font-medium">Microsoft</span>
                </button>
                <button className="flex items-center justify-center px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors">
                  <span className="text-sm font-medium">Google</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Hero Section */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12 sm:py-20">
        <div className="text-center">
          <h1 className="text-4xl sm:text-5xl lg:text-6xl font-bold text-gray-900 mb-6">
            Running Team Management
            <span className="block text-blue-600">Made Simple</span>
          </h1>
          <p className="text-lg sm:text-xl text-gray-600 mb-8 max-w-3xl mx-auto">
            Empower coaches to efficiently manage rosters, schedules, training plans, 
            communications, and more. Built mobile-first for coaches on the go.
          </p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <button className="bg-blue-600 text-white px-8 py-4 rounded-lg text-lg font-semibold hover:bg-blue-700 transition-colors">
              Start Your Free Team
            </button>
            <button className="border-2 border-blue-600 text-blue-600 px-8 py-4 rounded-lg text-lg font-semibold hover:bg-blue-50 transition-colors">
              Watch Demo
            </button>
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
            <div className="bg-white rounded-lg shadow-lg p-8 border-2 border-gray-200">
              <div className="text-center">
                <h3 className="text-2xl font-bold text-gray-900 mb-2">Free</h3>
                <div className="text-4xl font-bold text-gray-900 mb-4">$0<span className="text-lg text-gray-600">/month</span></div>
                <p className="text-gray-600 mb-6">Perfect for small teams getting started</p>
              </div>
              
              <ul className="space-y-3 mb-8">
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
              
              <button className="w-full bg-gray-600 text-white py-3 rounded-lg font-semibold hover:bg-gray-700 transition-colors">
                Get Started Free
              </button>
            </div>
            
            {/* Standard Plan */}
            <div className="bg-white rounded-lg shadow-lg p-8 border-2 border-gray-200">
              <div className="text-center">
                <h3 className="text-2xl font-bold text-gray-900 mb-2">Standard</h3>
                <div className="text-4xl font-bold text-gray-900 mb-4">$29<span className="text-lg text-gray-600">/month</span></div>
                <p className="text-gray-600 mb-6">Great for growing teams</p>
              </div>
              
              <ul className="space-y-3 mb-8">
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Up to 30 athletes</span>
                </li>
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Manual data entry</span>
                </li>
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Full messaging</span>
                </li>
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Basic reporting</span>
                </li>
              </ul>
              
              <button className="w-full bg-blue-600 text-white py-3 rounded-lg font-semibold hover:bg-blue-700 transition-colors">
                Start Standard Plan
              </button>
            </div>
            
            {/* Premium Plan */}
            <div className="bg-white rounded-lg shadow-lg p-8 border-2 border-blue-500 relative">
              <div className="absolute -top-4 left-1/2 transform -translate-x-1/2">
                <span className="bg-blue-500 text-white px-4 py-1 rounded-full text-sm font-semibold">
                  Most Popular
                </span>
              </div>
              
              <div className="text-center">
                <h3 className="text-2xl font-bold text-gray-900 mb-2">Premium</h3>
                <div className="text-4xl font-bold text-gray-900 mb-4">$79<span className="text-lg text-gray-600">/month</span></div>
                <p className="text-gray-600 mb-6">Everything you need for serious teams</p>
              </div>
              
              <ul className="space-y-3 mb-8">
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Unlimited athletes</span>
                </li>
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Garmin & MileSplit sync</span>
                </li>
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Import/export data</span>
                </li>
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Advanced reporting</span>
                </li>
                <li className="flex items-center">
                  <span className="text-green-500 mr-3">✓</span>
                  <span className="text-gray-700">Group training plans</span>
                </li>
              </ul>
              
              <button className="w-full bg-blue-600 text-white py-3 rounded-lg font-semibold hover:bg-blue-700 transition-colors">
                Start Premium Plan
              </button>
            </div>
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="bg-blue-600 py-16">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h2 className="text-3xl sm:text-4xl font-bold text-white mb-4">
            Ready to Transform Your Team Management?
          </h2>
          <p className="text-xl text-blue-100 mb-8 max-w-2xl mx-auto">
            Join hundreds of coaches who have simplified their team management with TeamStride. 
            Start your free trial today - no credit card required.
          </p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <button className="bg-white text-blue-600 px-8 py-4 rounded-lg text-lg font-semibold hover:bg-gray-100 transition-colors">
              Start Your Free Team
            </button>
            <button className="border-2 border-white text-white px-8 py-4 rounded-lg text-lg font-semibold hover:bg-blue-700 transition-colors">
              Schedule a Demo
            </button>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="bg-gray-900 text-white py-12">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-1 md:grid-cols-4 gap-8">
            <div>
              <div className="flex items-center mb-4">
                <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center">
                  <span className="text-white font-bold text-lg">T</span>
                </div>
                <span className="ml-2 text-xl font-bold">TeamStride</span>
              </div>
              <p className="text-gray-400">
                Empowering coaches to manage running teams efficiently with modern, mobile-first tools.
              </p>
            </div>
            
            <div>
              <h4 className="text-lg font-semibold mb-4">Product</h4>
              <ul className="space-y-2 text-gray-400">
                <li><a href="#" className="hover:text-white transition-colors">Features</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Pricing</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Integrations</a></li>
                <li><a href="#" className="hover:text-white transition-colors">API</a></li>
              </ul>
            </div>
            
            <div>
              <h4 className="text-lg font-semibold mb-4">Support</h4>
              <ul className="space-y-2 text-gray-400">
                <li><a href="#" className="hover:text-white transition-colors">Help Center</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Contact Us</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Status</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Community</a></li>
              </ul>
            </div>
            
            <div>
              <h4 className="text-lg font-semibold mb-4">Legal</h4>
              <ul className="space-y-2 text-gray-400">
                <li><a href="#" className="hover:text-white transition-colors">Privacy Policy</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Terms of Service</a></li>
                <li><a href="#" className="hover:text-white transition-colors">COPPA Compliance</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Security</a></li>
              </ul>
            </div>
          </div>
          
          <div className="border-t border-gray-800 mt-8 pt-8 text-center text-gray-400">
            <p>&copy; 2024 TeamStride. All rights reserved.</p>
          </div>
        </div>
      </footer>
    </div>
  );
}
