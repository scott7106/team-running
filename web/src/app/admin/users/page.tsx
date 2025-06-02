'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { 
  faUsers, 
  faArrowLeft,
  faPlus,
  faSearch
} from '@fortawesome/free-solid-svg-icons';

export default function AdminUsersPage() {
  const router = useRouter();

  useEffect(() => {
    // Check if user is authenticated
    const token = localStorage.getItem('token');
    if (!token) {
      router.push('/');
      return;
    }
  }, [router]);

  const handleBack = () => {
    router.push('/admin');
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Header */}
        <div className="mb-8">
          <button
            onClick={handleBack}
            className="flex items-center text-gray-600 hover:text-gray-800 mb-4"
          >
            <FontAwesomeIcon icon={faArrowLeft} className="w-4 h-4 mr-2" />
            Back to Admin Dashboard
          </button>
          
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-3xl font-bold text-gray-900">User Management</h1>
              <p className="text-gray-600 mt-1">Manage users across all teams</p>
            </div>
            <button className="bg-blue-600 text-white px-4 py-2 rounded-lg font-medium hover:bg-blue-700 transition-colors flex items-center">
              <FontAwesomeIcon icon={faPlus} className="w-4 h-4 mr-2" />
              Create User
            </button>
          </div>
        </div>

        {/* Search and filters */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6 mb-6">
          <div className="flex items-center space-x-4">
            <div className="flex-1 relative">
              <FontAwesomeIcon icon={faSearch} className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
              <input
                type="text"
                placeholder="Search users..."
                className="w-full pl-10 pr-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <select className="border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500">
              <option>All Roles</option>
              <option>Global Admin</option>
              <option>Team Owner</option>
              <option>Team Admin</option>
              <option>Team Member</option>
            </select>
            <select className="border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500">
              <option>All Statuses</option>
              <option>Active</option>
              <option>Inactive</option>
              <option>Pending</option>
            </select>
          </div>
        </div>

        {/* Users table */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200">
          <div className="p-6">
            <div className="flex items-center mb-6">
              <FontAwesomeIcon icon={faUsers} className="w-5 h-5 text-green-600 mr-2" />
              <h2 className="text-xl font-semibold text-gray-900">Users</h2>
            </div>
            
            <div className="text-center py-12">
              <FontAwesomeIcon icon={faUsers} className="w-12 h-12 text-gray-300 mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">No users found</h3>
              <p className="text-gray-500 mb-4">
                This is a placeholder page. User management functionality will be implemented here.
              </p>
              <button className="text-green-600 hover:text-green-700 font-medium">
                Learn more about user management →
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
} 