'use client';

import { useRouter } from 'next/navigation';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { 
  faUsers, 
  faBuilding, 
  faUserShield
} from '@fortawesome/free-solid-svg-icons';
import AdminLayout from '@/components/AdminLayout';

export default function GlobalAdminPage() {
  const router = useRouter();

  const handleManageTeams = () => {
    router.push('/admin/teams');
  };

  const handleManageUsers = () => {
    router.push('/admin/users');
  };

  return (
    <AdminLayout pageTitle="Global Administration" currentSection="dashboard">
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
    </AdminLayout>
  );
} 