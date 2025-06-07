'use client';

import { useState, useEffect, useCallback } from 'react';

// Note: metadata export must be in a non-client component, so we'll use useEffect to set document title
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { 
  faUsers, 
  faPlus,
  faSearch,
  faChevronLeft,
  faChevronRight,
  faEllipsisV,
  faEdit,
  faTrash,
  faTrashAlt,
  faExclamationTriangle,
  faSpinner,
  faFilter,
  faChevronDown,
  faChevronUp,
  faShield,
  faUserTimes,
  faKey
} from '@fortawesome/free-solid-svg-icons';
import AdminLayout from '@/components/AdminLayout';
import ConfirmationModal from '@/components/confirmation-modal';
import CreateUserModal from '@/components/CreateUserModal';
import EditUserModal from '@/components/EditUserModal';
import ResetPasswordModal from '@/components/ResetPasswordModal';
import { GlobalAdminUserDto, UserStatus, UsersApiParams } from '@/types/user';
import { usersApi, ApiError } from '@/utils/api';

interface DropdownMenuProps {
  user: GlobalAdminUserDto;
  onEdit: (user: GlobalAdminUserDto) => void;
  onDelete: (user: GlobalAdminUserDto) => void;
  onPurge: (user: GlobalAdminUserDto) => void;
  onResetPassword: (user: GlobalAdminUserDto) => void;
  onResetLockout: (user: GlobalAdminUserDto) => void;
}

function DropdownMenu({ user, onEdit, onDelete, onPurge, onResetPassword, onResetLockout }: DropdownMenuProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [dropdownPosition, setDropdownPosition] = useState({ top: 0, right: 0, dropUp: false });

  const handleToggle = (e: React.MouseEvent) => {
    if (!isOpen) {
      // Get button position relative to viewport
      const rect = e.currentTarget.getBoundingClientRect();
      const spaceBelow = window.innerHeight - rect.bottom;
      const dropdownHeight = 130; // Height for 4-5 menu items (users has more items than teams)
      const dropUp = spaceBelow < dropdownHeight;
      
      setDropdownPosition({
        top: dropUp ? rect.top - dropdownHeight - 2 : rect.bottom + 2,
        right: window.innerWidth - rect.right + 8, // Adjust to align better with button
        dropUp
      });
    }
    setIsOpen(!isOpen);
  };

  return (
    <div className="relative">
      <button
        onClick={handleToggle}
        className="p-2 hover:bg-gray-100 rounded-lg transition-colors"
        aria-label="Actions"
      >
        <FontAwesomeIcon icon={faEllipsisV} className="w-4 h-4 text-gray-500" />
      </button>
      
      {isOpen && (
        <>
          <div 
            className="fixed inset-0 z-40" 
            onClick={() => setIsOpen(false)}
          />
          <div 
            className="fixed w-48 bg-white rounded-lg shadow-lg border border-gray-200 z-50"
            style={{
              top: `${dropdownPosition.top}px`,
              right: `${dropdownPosition.right}px`
            }}
          >
            <div className="py-1">
              <button
                onClick={() => {
                  onEdit(user);
                  setIsOpen(false);
                }}
                className="flex items-center w-full px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
              >
                <FontAwesomeIcon icon={faEdit} className="w-4 h-4 mr-3" />
                Edit User
              </button>
              <button
                onClick={() => {
                  onResetPassword(user);
                  setIsOpen(false);
                }}
                className="flex items-center w-full px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
              >
                <FontAwesomeIcon icon={faKey} className="w-4 h-4 mr-3" />
                Reset Password
              </button>
              {user.lockoutEnd && new Date(user.lockoutEnd) > new Date() && (
                <button
                  onClick={() => {
                    onResetLockout(user);
                    setIsOpen(false);
                  }}
                  className="flex items-center w-full px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                >
                  <FontAwesomeIcon icon={faUserTimes} className="w-4 h-4 mr-3" />
                  Reset Lockout
                </button>
              )}
              <button
                onClick={() => {
                  onDelete(user);
                  setIsOpen(false);
                }}
                className="flex items-center w-full px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
              >
                <FontAwesomeIcon icon={faTrash} className="w-4 h-4 mr-3" />
                Delete User
              </button>
              <button
                onClick={() => {
                  onPurge(user);
                  setIsOpen(false);
                }}
                className="flex items-center w-full px-4 py-2 text-sm text-red-600 hover:bg-red-50"
              >
                <FontAwesomeIcon icon={faTrashAlt} className="w-4 h-4 mr-3" />
                Purge User
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}

const getStatusBadgeClass = (status: UserStatus): string => {
  switch (status) {
    case UserStatus.Active:
      return 'bg-green-100 text-green-800';
    case UserStatus.Inactive:
      return 'bg-gray-100 text-gray-800';
    case UserStatus.Suspended:
      return 'bg-red-100 text-red-800';
    default:
      return 'bg-gray-100 text-gray-800';
  }
};

const getStatusLabel = (status: UserStatus): string => {
  switch (status) {
    case UserStatus.Active:
      return 'Active';
    case UserStatus.Inactive:
      return 'Inactive';
    case UserStatus.Suspended:
      return 'Suspended';
    default:
      return 'Unknown';
  }
};

const getPrimaryRoleBadge = (user: GlobalAdminUserDto): { label: string; className: string } => {
  if (user.isGlobalAdmin) {
    return { label: 'Global Admin', className: 'bg-purple-100 text-purple-800' };
  }
  
  if (user.applicationRoles.includes('TeamOwner')) {
    return { label: 'Team Owner', className: 'bg-blue-100 text-blue-800' };
  }
  
  if (user.applicationRoles.includes('TeamAdmin')) {
    return { label: 'Team Admin', className: 'bg-indigo-100 text-indigo-800' };
  }
  
  return { label: 'Team Member', className: 'bg-gray-100 text-gray-800' };
};

export default function AdminUsersPage() {
  const [users, setUsers] = useState<GlobalAdminUserDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState<UserStatus | ''>('');
  const [activeFilter, setActiveFilter] = useState<boolean | ''>('');
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [pageSize] = useState(10);
  const [filtersExpanded, setFiltersExpanded] = useState(false);
  
  // Create user modal state
  const [showCreateModal, setShowCreateModal] = useState(false);
  
  // Edit user modal state
  const [showEditModal, setShowEditModal] = useState(false);
  const [userToEdit, setUserToEdit] = useState<GlobalAdminUserDto | null>(null);
  
  // Reset password modal state
  const [showResetPasswordModal, setShowResetPasswordModal] = useState(false);
  const [userToResetPassword, setUserToResetPassword] = useState<GlobalAdminUserDto | null>(null);
  
  // Delete and purge modal state
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [showPurgeModal, setShowPurgeModal] = useState(false);
  const [userToDelete, setUserToDelete] = useState<GlobalAdminUserDto | null>(null);
  const [userToPurge, setUserToPurge] = useState<GlobalAdminUserDto | null>(null);
  const [deleteLoading, setDeleteLoading] = useState(false);
  const [purgeLoading, setPurgeLoading] = useState(false);

  const loadUsers = useCallback(async (params: UsersApiParams = {}) => {
    try {
      setLoading(true);
      setError(null);
      
      const response = await usersApi.getUsers({
        pageNumber: currentPage,
        pageSize,
        searchQuery: searchQuery || undefined,
        status: statusFilter !== '' ? statusFilter : undefined,
        isActive: activeFilter !== '' ? activeFilter : undefined,
        ...params
      });
      
      setUsers(response.items);
      setTotalPages(response.totalPages);
      setTotalCount(response.totalCount);
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('Failed to load users. Please try again.');
      }
      console.error('Error loading users:', err);
    } finally {
      setLoading(false);
    }
  }, [currentPage, pageSize, searchQuery, statusFilter, activeFilter]);

  useEffect(() => {
    loadUsers();
  }, [loadUsers]);

  // Set document title
  useEffect(() => {
    document.title = "TeamStride - Global Administration - Users";
  }, []);

  const handleSearch = (query: string) => {
    setSearchQuery(query);
    setCurrentPage(1);
  };

  const handleStatusFilter = (status: UserStatus | '') => {
    setStatusFilter(status);
    setCurrentPage(1);
  };

  const handleActiveFilter = (isActive: boolean | '') => {
    setActiveFilter(isActive);
    setCurrentPage(1);
  };

  const handlePageChange = (page: number) => {
    setCurrentPage(page);
  };

  const handleEdit = (user: GlobalAdminUserDto) => {
    setUserToEdit(user);
    setShowEditModal(true);
  };

  const handleDelete = (user: GlobalAdminUserDto) => {
    setUserToDelete(user);
    setShowDeleteModal(true);
  };

  const handlePurge = (user: GlobalAdminUserDto) => {
    setUserToPurge(user);
    setShowPurgeModal(true);
  };

  const confirmDelete = async () => {
    if (!userToDelete) return;
    
    try {
      setDeleteLoading(true);
      setError(null);
      
      await usersApi.deleteUser(userToDelete.id);
      
      // Remove the user from the current list or refresh
      setUsers(prev => prev.filter(user => user.id !== userToDelete.id));
      setTotalCount(prev => prev - 1);
      
      setShowDeleteModal(false);
      setUserToDelete(null);
      
      // Refresh the users list to get accurate pagination
      loadUsers();
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('Failed to delete user. Please try again.');
      }
      console.error('Error deleting user:', err);
    } finally {
      setDeleteLoading(false);
    }
  };

  const confirmPurge = async () => {
    if (!userToPurge) return;
    
    try {
      setPurgeLoading(true);
      setError(null);
      
      await usersApi.purgeUser(userToPurge.id);
      
      // Remove the user from the current list
      setUsers(prev => prev.filter(user => user.id !== userToPurge.id));
      setTotalCount(prev => prev - 1);
      
      setShowPurgeModal(false);
      setUserToPurge(null);
      
      // Refresh the users list to get accurate pagination
      loadUsers();
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('Failed to purge user. Please try again.');
      }
      console.error('Error purging user:', err);
    } finally {
      setPurgeLoading(false);
    }
  };

  const handleResetPassword = (user: GlobalAdminUserDto) => {
    setUserToResetPassword(user);
    setShowResetPasswordModal(true);
  };

  const handleResetLockout = async (user: GlobalAdminUserDto) => {
    try {
      setError(null);
      await usersApi.resetLockout(user.id);
      
      // Refresh the users list to reflect the updated lockout status
      loadUsers();
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('Failed to reset lockout. Please try again.');
      }
      console.error('Error resetting lockout:', err);
    }
  };

  const handlePasswordReset = () => {
    // Refresh the users list
    loadUsers();
  };

  const handleCreateUser = () => {
    setShowCreateModal(true);
  };

  const handleUserCreated = () => {
    // Refresh the users list
    loadUsers();
  };

  const handleUserUpdated = () => {
    // Refresh the users list
    loadUsers();
  };

  const startIndex = (currentPage - 1) * pageSize + 1;
  const endIndex = Math.min(currentPage * pageSize, totalCount);

  // Check if any filters are active
  const hasActiveFilters = searchQuery || statusFilter !== '' || activeFilter !== '';

  return (
    <AdminLayout pageTitle="Global Administration" currentSection="users">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="mb-8">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-3xl font-bold text-gray-900">User Management</h1>
              <p className="text-gray-600 mt-1">Manage users across the TeamStride platform</p>
            </div>
            <button 
              onClick={handleCreateUser}
              className="bg-blue-600 text-white px-4 py-2 rounded-lg font-medium hover:bg-blue-700 transition-colors flex items-center"
            >
              <FontAwesomeIcon icon={faPlus} className="w-4 h-4 mr-2" />
              Create User
            </button>
          </div>
        </div>

        {/* Search and filters */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 mb-6">
          {/* Mobile filter toggle header */}
          <div className="sm:hidden flex items-center justify-between p-4 border-b border-gray-200">
            <div className="flex items-center">
              <FontAwesomeIcon icon={faFilter} className="w-4 h-4 text-gray-500 mr-2" />
              <span className="text-sm font-medium text-gray-700">
                Filters
                {hasActiveFilters && (
                  <span className="ml-2 inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                    Active
                  </span>
                )}
              </span>
            </div>
            <button
              onClick={() => setFiltersExpanded(!filtersExpanded)}
              className="p-1 hover:bg-gray-100 rounded-lg transition-colors"
              aria-label={filtersExpanded ? 'Collapse filters' : 'Expand filters'}
            >
              <FontAwesomeIcon 
                icon={filtersExpanded ? faChevronUp : faChevronDown} 
                className="w-4 h-4 text-gray-500" 
              />
            </button>
          </div>

          {/* Filter content - always visible on desktop, collapsible on mobile */}
          <div className={`p-6 ${!filtersExpanded ? 'hidden sm:block' : 'block'}`}>
            <div className="flex flex-col sm:flex-row items-stretch sm:items-center space-y-4 sm:space-y-0 sm:space-x-4">
              <div className="flex-1 relative">
                <FontAwesomeIcon icon={faSearch} className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
                <input
                  type="text"
                  placeholder="Search users by name, email, or ID..."
                  value={searchQuery}
                  onChange={(e) => handleSearch(e.target.value)}
                  className="w-full pl-10 pr-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
              <select 
                value={statusFilter}
                onChange={(e) => handleStatusFilter(e.target.value as UserStatus | '')}
                className="border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value="">All Statuses</option>
                <option value={UserStatus.Active}>Active</option>
                <option value={UserStatus.Inactive}>Inactive</option>
                <option value={UserStatus.Suspended}>Suspended</option>
              </select>
              <select 
                value={activeFilter === '' ? '' : activeFilter.toString()}
                onChange={(e) => handleActiveFilter(e.target.value === '' ? '' : e.target.value === 'true')}
                className="border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value="">All States</option>
                <option value="true">Active</option>
                <option value="false">Inactive</option>
              </select>
            </div>
          </div>
        </div>

        {/* Users table */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200">
          <div className="p-6">
            <div className="flex items-center justify-between mb-6">
              <div className="flex items-center">
                <FontAwesomeIcon icon={faUsers} className="w-5 h-5 text-green-600 mr-2" />
                <h2 className="text-xl font-semibold text-gray-900">Users</h2>
              </div>
              {totalCount > 0 && (
                <p className="text-sm text-gray-500">
                  Showing {startIndex}-{endIndex} of {totalCount} users
                </p>
              )}
            </div>
            
            {error && (
              <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg flex items-center">
                <FontAwesomeIcon icon={faExclamationTriangle} className="w-5 h-5 text-red-600 mr-3" />
                <p className="text-red-700">{error}</p>
              </div>
            )}

            {loading ? (
              <div className="text-center py-12">
                <FontAwesomeIcon icon={faSpinner} className="w-8 h-8 text-blue-600 animate-spin mb-4" />
                <p className="text-gray-500">Loading users...</p>
              </div>
            ) : users.length === 0 ? (
              <div className="text-center py-12">
                <FontAwesomeIcon icon={faUsers} className="w-12 h-12 text-gray-300 mb-4" />
                <h3 className="text-lg font-medium text-gray-900 mb-2">No users found</h3>
                <p className="text-gray-500 mb-4">
                  {searchQuery || statusFilter !== '' || activeFilter !== '' 
                    ? 'Try adjusting your search or filters.'
                    : 'No users have been created yet.'}
                </p>
              </div>
            ) : (
              <>
                {/* Desktop table */}
                <div className="hidden lg:block overflow-x-auto">
                  <table className="min-w-full divide-y divide-gray-200">
                    <thead className="bg-gray-50">
                      <tr>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                          User
                        </th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                          Role
                        </th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                          Status
                        </th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                          Teams
                        </th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                          Last Login
                        </th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                          Created
                        </th>
                        <th className="relative px-6 py-3">
                          <span className="sr-only">Actions</span>
                        </th>
                      </tr>
                    </thead>
                    <tbody className="bg-white divide-y divide-gray-200">
                      {users.map((user) => {
                        const primaryRole = getPrimaryRoleBadge(user);
                        const isLocked = user.lockoutEnd && new Date(user.lockoutEnd) > new Date();
                        
                        return (
                          <tr key={user.id} className="hover:bg-gray-50">
                            <td className="px-6 py-4 whitespace-nowrap">
                              <div className="flex items-center">
                                <div>
                                  <div className="flex items-center">
                                    <div className="text-sm font-medium text-gray-900">
                                      {user.displayName}
                                    </div>
                                    {user.isGlobalAdmin && (
                                      <FontAwesomeIcon icon={faShield} className="w-4 h-4 text-purple-600 ml-2" title="Global Admin" />
                                    )}
                                    {isLocked && (
                                      <FontAwesomeIcon icon={faUserTimes} className="w-4 h-4 text-red-600 ml-2" title="Account Locked" />
                                    )}
                                  </div>
                                  <div className="text-sm text-gray-500">{user.email}</div>
                                </div>
                              </div>
                            </td>
                            <td className="px-6 py-4 whitespace-nowrap">
                              <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${primaryRole.className}`}>
                                {primaryRole.label}
                              </span>
                            </td>
                            <td className="px-6 py-4 whitespace-nowrap">
                              <div className="flex items-center space-x-2">
                                <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${getStatusBadgeClass(user.status)}`}>
                                  {getStatusLabel(user.status)}
                                </span>
                                {!user.isActive && (
                                  <span className="inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-red-100 text-red-800">
                                    Inactive
                                  </span>
                                )}
                              </div>
                            </td>
                            <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                              <div>
                                <div>{user.teamCount} team{user.teamCount !== 1 ? 's' : ''}</div>
                                {user.defaultTeamName && (
                                  <div className="text-xs text-gray-500">Default: {user.defaultTeamName}</div>
                                )}
                              </div>
                            </td>
                            <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                              {user.lastLoginOn ? new Date(user.lastLoginOn).toLocaleDateString() : 'Never'}
                            </td>
                            <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                              {new Date(user.createdOn).toLocaleDateString()}
                            </td>
                            <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                              <DropdownMenu
                                user={user}
                                onEdit={handleEdit}
                                onDelete={handleDelete}
                                onPurge={handlePurge}
                                onResetPassword={handleResetPassword}
                                onResetLockout={handleResetLockout}
                              />
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>

                {/* Mobile cards */}
                <div className="lg:hidden space-y-4">
                  {users.map((user) => {
                    const primaryRole = getPrimaryRoleBadge(user);
                    const isLocked = user.lockoutEnd && new Date(user.lockoutEnd) > new Date();
                    
                    return (
                      <div key={user.id} className="border border-gray-200 rounded-lg p-4">
                        <div className="flex items-center justify-between mb-3">
                          <div className="flex items-center">
                            <h3 className="text-lg font-medium text-gray-900">{user.displayName}</h3>
                            {user.isGlobalAdmin && (
                              <FontAwesomeIcon icon={faShield} className="w-4 h-4 text-purple-600 ml-2" title="Global Admin" />
                            )}
                            {isLocked && (
                              <FontAwesomeIcon icon={faUserTimes} className="w-4 h-4 text-red-600 ml-2" title="Account Locked" />
                            )}
                          </div>
                          <DropdownMenu
                            user={user}
                            onEdit={handleEdit}
                            onDelete={handleDelete}
                            onPurge={handlePurge}
                            onResetPassword={handleResetPassword}
                            onResetLockout={handleResetLockout}
                          />
                        </div>
                        <div className="space-y-2">
                          <p className="text-sm text-gray-600">{user.email}</p>
                          <div className="flex items-center space-x-2">
                            <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${primaryRole.className}`}>
                              {primaryRole.label}
                            </span>
                            <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${getStatusBadgeClass(user.status)}`}>
                              {getStatusLabel(user.status)}
                            </span>
                            {!user.isActive && (
                              <span className="inline-flex px-2 py-1 text-xs font-semibold rounded-full bg-red-100 text-red-800">
                                Inactive
                              </span>
                            )}
                          </div>
                          <div className="flex justify-between text-sm text-gray-500">
                            <span>{user.teamCount} team{user.teamCount !== 1 ? 's' : ''}</span>
                            <span>Last login: {user.lastLoginOn ? new Date(user.lastLoginOn).toLocaleDateString() : 'Never'}</span>
                          </div>
                          <div className="text-sm text-gray-500">
                            Created {new Date(user.createdOn).toLocaleDateString()}
                          </div>
                          {user.defaultTeamName && (
                            <div className="text-sm text-gray-500">
                              Default team: {user.defaultTeamName}
                            </div>
                          )}
                        </div>
                      </div>
                    );
                  })}
                </div>

                {/* Pagination */}
                {totalPages > 1 && (
                  <div className="mt-6 flex items-center justify-between">
                    <div className="text-sm text-gray-700">
                      Showing {startIndex} to {endIndex} of {totalCount} results
                    </div>
                    <div className="flex items-center space-x-2">
                      <button
                        onClick={() => handlePageChange(currentPage - 1)}
                        disabled={currentPage === 1}
                        className="p-2 border border-gray-300 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
                      >
                        <FontAwesomeIcon icon={faChevronLeft} className="w-4 h-4" />
                      </button>
                      
                      <div className="flex items-center space-x-1">
                        {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                          let pageNum;
                          if (totalPages <= 5) {
                            pageNum = i + 1;
                          } else if (currentPage <= 3) {
                            pageNum = i + 1;
                          } else if (currentPage >= totalPages - 2) {
                            pageNum = totalPages - 4 + i;
                          } else {
                            pageNum = currentPage - 2 + i;
                          }
                          
                          return (
                            <button
                              key={pageNum}
                              onClick={() => handlePageChange(pageNum)}
                              className={`px-3 py-2 text-sm rounded-lg ${
                                pageNum === currentPage
                                  ? 'bg-blue-600 text-white'
                                  : 'border border-gray-300 text-gray-700 hover:bg-gray-50'
                              }`}
                            >
                              {pageNum}
                            </button>
                          );
                        })}
                      </div>
                      
                      <button
                        onClick={() => handlePageChange(currentPage + 1)}
                        disabled={currentPage === totalPages}
                        className="p-2 border border-gray-300 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
                      >
                        <FontAwesomeIcon icon={faChevronRight} className="w-4 h-4" />
                      </button>
                    </div>
                  </div>
                )}
              </>
            )}
          </div>
        </div>

      {/* Delete User Confirmation Modal */}
      <ConfirmationModal
        isOpen={showDeleteModal}
        onClose={() => {
          setShowDeleteModal(false);
          setUserToDelete(null);
        }}
        onConfirm={confirmDelete}
        title="Delete User"
        message={`Are you sure you want to delete "${userToDelete?.displayName}"? This will soft delete the user account. The user can be recovered later if needed.`}
        icon={<FontAwesomeIcon icon={faTrash} className="w-5 h-5 text-red-600" />}
        confirmText="Delete User"
        loading={deleteLoading}
      />

      {/* Purge User Confirmation Modal */}
      <ConfirmationModal
        isOpen={showPurgeModal}
        onClose={() => {
          setShowPurgeModal(false);
          setUserToPurge(null);
        }}
        onConfirm={confirmPurge}
        title="Purge User"
        message={`Are you sure you want to permanently purge "${userToPurge?.displayName}"? This action cannot be undone and will permanently remove the user account and all associated data.`}
        icon={<FontAwesomeIcon icon={faTrashAlt} className="w-5 h-5 text-red-600" />}
        confirmText="Purge User"
        confirmButtonClass="bg-red-700 text-white hover:bg-red-800 focus:ring-red-600"
        loading={purgeLoading}
      />

      {/* Create User Modal */}
      <CreateUserModal
        isOpen={showCreateModal}
        onClose={() => setShowCreateModal(false)}
        onUserCreated={handleUserCreated}
      />

      {/* Edit User Modal */}
      <EditUserModal
        isOpen={showEditModal}
        onClose={() => setShowEditModal(false)}
        onUserUpdated={handleUserUpdated}
        user={userToEdit}
      />

      {/* Reset Password Modal */}
      <ResetPasswordModal
        isOpen={showResetPasswordModal}
        onClose={() => setShowResetPasswordModal(false)}
        onPasswordReset={handlePasswordReset}
        user={userToResetPassword}
      />
      </div>
    </AdminLayout>
  );
} 