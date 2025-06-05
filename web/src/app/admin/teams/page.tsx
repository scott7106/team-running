'use client';

import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { 
  faBuilding, 
  faPlus,
  faSearch
} from '@fortawesome/free-solid-svg-icons';
import AdminLayout from '@/components/AdminLayout';

export default function AdminTeamsPage() {
  return (
    <AdminLayout pageTitle="Team Management" currentSection="teams">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="mb-8">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-3xl font-bold text-gray-900">Team Management</h1>
              <p className="text-gray-600 mt-1">Manage teams across the TeamStride platform</p>
            </div>
            <button className="bg-blue-600 text-white px-4 py-2 rounded-lg font-medium hover:bg-blue-700 transition-colors flex items-center">
              <FontAwesomeIcon icon={faPlus} className="w-4 h-4 mr-2" />
              Create Team
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
                placeholder="Search teams..."
                className="w-full pl-10 pr-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <select className="border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500">
              <option>All Statuses</option>
              <option>Active</option>
              <option>Inactive</option>
              <option>Suspended</option>
            </select>
            <select className="border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500">
              <option>All Tiers</option>
              <option>Free</option>
              <option>Standard</option>
              <option>Premium</option>
            </select>
          </div>
        </div>

        {/* Teams table */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200">
          <div className="p-6">
            <div className="flex items-center mb-6">
              <FontAwesomeIcon icon={faBuilding} className="w-5 h-5 text-blue-600 mr-2" />
              <h2 className="text-xl font-semibold text-gray-900">Teams</h2>
            </div>
            
            <div className="text-center py-12">
              <FontAwesomeIcon icon={faBuilding} className="w-12 h-12 text-gray-300 mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">No teams found</h3>
              <p className="text-gray-500 mb-4">
                This is a placeholder page. Team management functionality will be implemented here.
              </p>
              <button className="text-blue-600 hover:text-blue-700 font-medium">
                Learn more about team management →
              </button>
            </div>
          </div>
        </div>
      </div>
    </AdminLayout>
  );
} 