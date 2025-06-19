'use client';

import { useState, useEffect, useCallback } from 'react';
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
  faExclamationTriangle,
  faSpinner,
  faFilter,
  faChevronDown,
  faChevronUp,
  faIdCard,
  faFileSignature
} from '@fortawesome/free-solid-svg-icons';
import TeamThemeProvider from '../components/team-theme-provider';
import UserContextMenu from '@/components/shared/user-context-menu';
import ConfirmationModal from '@/components/ui/confirmation-modal';
import BaseLayout from '@/components/layouts/base-layout';
import { TEAM_NAV_ITEMS } from '@/components/layouts/navigation-config';
import { SubdomainThemeDto } from '@/types/team';
import CreateAthleteModal from '../components/roster/create-athlete-modal';
import EditAthleteModal from '../components/roster/edit-athlete-modal';
import AthleteProfileModal from '../components/roster/athlete-profile-modal';
import { AthleteDto, AthleteRole, AthleteApiParams } from '@/types/athlete';
import { athletesApi, ApiError } from '@/utils/api';
import { useAuthTokenRefresh } from '@/hooks/use-auth-token-refresh';
import { useUser, useTenant } from '@/contexts/auth-context';

interface DropdownMenuProps {
  athlete: AthleteDto;
  onEdit: (athlete: AthleteDto) => void;
  onDelete: (athlete: AthleteDto) => void;
  onProfile: (athlete: AthleteDto) => void;
  onTogglePhysical: (athlete: AthleteDto) => void;
  onToggleWaiver: (athlete: AthleteDto) => void;
  canEdit: boolean;
  canDelete: boolean;
  canToggleStatus: boolean;
}

function DropdownMenu({ 
  athlete, 
  onEdit, 
  onDelete, 
  onProfile, 
  onTogglePhysical, 
  onToggleWaiver, 
  canEdit,
  canDelete,
  canToggleStatus
}: DropdownMenuProps) {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <div className="relative">
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-full"
      >
        <FontAwesomeIcon icon={faEllipsisV} className="w-4 h-4" />
      </button>
      
      {isOpen && (
        <>
          <div 
            className="fixed inset-0 z-10" 
            onClick={() => setIsOpen(false)}
          />
          <div className="absolute right-0 mt-2 w-48 bg-white rounded-md shadow-lg z-20 border border-gray-200">
            <div className="py-1">
              <button
                onClick={() => {
                  onProfile(athlete);
                  setIsOpen(false);
                }}
                className="flex items-center w-full px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
              >
                <FontAwesomeIcon icon={faIdCard} className="w-4 h-4 mr-3" />
                View Profile
              </button>
              
              {canEdit && (
                <button
                  onClick={() => {
                    onEdit(athlete);
                    setIsOpen(false);
                  }}
                  className="flex items-center w-full px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                >
                  <FontAwesomeIcon icon={faEdit} className="w-4 h-4 mr-3" />
                  Edit Athlete
                </button>
              )}
              
              {canToggleStatus && (
                <>
                  <button
                    onClick={() => {
                      onTogglePhysical(athlete);
                      setIsOpen(false);
                    }}
                    className="flex items-center w-full px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                  >
                    <FontAwesomeIcon icon={faFileSignature} className="w-4 h-4 mr-3" />
                    {athlete.hasPhysicalOnFile ? 'Remove Physical' : 'Mark Physical Complete'}
                  </button>
                  
                  <button
                    onClick={() => {
                      onToggleWaiver(athlete);
                      setIsOpen(false);
                    }}
                    className="flex items-center w-full px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                  >
                    <FontAwesomeIcon icon={faFileSignature} className="w-4 h-4 mr-3" />
                    {athlete.hasWaiverSigned ? 'Remove Waiver' : 'Mark Waiver Signed'}
                  </button>
                </>
              )}
              
              {canDelete && (
                <button
                  onClick={() => {
                    onDelete(athlete);
                    setIsOpen(false);
                  }}
                  className="flex items-center w-full px-4 py-2 text-sm text-red-600 hover:bg-red-50"
                >
                  <FontAwesomeIcon icon={faTrash} className="w-4 h-4 mr-3" />
                  Remove from Team
                </button>
              )}
            </div>
          </div>
        </>
      )}
    </div>
  );
}

// Helper function to convert AthleteRole enum to display string
function getAthleteRoleDisplayName(role: AthleteRole): string {
  switch (role) {
    case AthleteRole.Athlete:
      return 'Athlete';
    case AthleteRole.Captain:
      return 'Captain';
    default:
      return 'Athlete';
  }
}

export default function RosterPage() {
  const [isTeamMember, setIsTeamMember] = useState(false);
  const [teamName, setTeamName] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [themeData] = useState<SubdomainThemeDto | null>(null);
  const [athletes, setAthletes] = useState<AthleteDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [roleFilter, setRoleFilter] = useState<AthleteRole | ''>('');
  const [physicalFilter, setPhysicalFilter] = useState<boolean | ''>('');
  const [waiverFilter, setWaiverFilter] = useState<boolean | ''>('');
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [pageSize] = useState(10);
  const [filtersExpanded, setFiltersExpanded] = useState(false);
  
  // Create athlete modal state
  const [showCreateModal, setShowCreateModal] = useState(false);
  
  // Edit athlete modal state
  const [showEditModal, setShowEditModal] = useState(false);
  const [athleteToEdit, setAthleteToEdit] = useState<AthleteDto | null>(null);
  
  // Profile modal state
  const [showProfileModal, setShowProfileModal] = useState(false);
  const [athleteToView, setAthleteToView] = useState<AthleteDto | null>(null);
  
  // Delete modal state
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [athleteToDelete, setAthleteToDelete] = useState<AthleteDto | null>(null);
  const [deleteLoading, setDeleteLoading] = useState(false);

  // Use centralized auth state
  const { user, isAuthenticated, subdomainAccessDenied } = useUser();
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

    // Check team membership using centralized auth state
    const checkTeamMembership = () => {
      // Check if current subdomain matches user's team context
      // Users with subdomainAccessDenied should be treated as non-members
      const hasTeamAccess = !subdomainAccessDenied && hasTeam && tenant?.teamSubdomain === subdomain;
      setIsTeamMember(hasTeamAccess || false);
      
      // Set team info from subdomain
      setTeamName(subdomain.charAt(0).toUpperCase() + subdomain.slice(1));
      setIsLoading(false);
    };

    checkTeamMembership();
  }, [hasTeam, tenant, subdomainAccessDenied]);

  // Permission checks
  const canEditAthlete = (athlete: AthleteDto): boolean => {
    if (!tenant?.teamId) return false;
    
    // Coaches can edit any athlete
    if (tenant.teamRole === 'TeamOwner' || tenant.teamRole === 'TeamAdmin') return true;
    
    // Athletes can edit their own record
    return athlete.userId === user?.id;
  };

  const canDeleteAthlete = (): boolean => {
    if (!tenant?.teamId) return false;
    
    // Only coaches can delete athletes
    return tenant.teamRole === 'TeamOwner' || tenant.teamRole === 'TeamAdmin';
  };

  const canToggleStatus = (athlete: AthleteDto): boolean => {
    if (!tenant?.teamId) return false;
    
    // Coaches can toggle status for any athlete
    if (tenant.teamRole === 'TeamOwner' || tenant.teamRole === 'TeamAdmin') return true;
    
    // Athletes can toggle their own status
    return athlete.userId === user?.id;
  };

  const canCreateAthlete = (): boolean => {
    if (!tenant?.teamId) return false;
    
    // Only coaches can create athletes
    return tenant.teamRole === 'TeamOwner' || tenant.teamRole === 'TeamAdmin';
  };

  const loadAthletes = useCallback(async (params: AthleteApiParams = {}) => {
    try {
      setLoading(true);
      setError(null);
      
      const response = await athletesApi.getAthletes({
        pageNumber: currentPage,
        pageSize,
        searchQuery: searchQuery || undefined,
        role: roleFilter !== '' ? roleFilter : undefined,
        hasPhysical: physicalFilter !== '' ? physicalFilter : undefined,
        hasWaiver: waiverFilter !== '' ? waiverFilter : undefined,
        ...params
      });
      
      setAthletes(response.items);
      setTotalPages(response.totalPages);
      setTotalCount(response.totalCount);
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('Failed to load athletes. Please try again.');
      }
      console.error('Error loading athletes:', err);
    } finally {
      setLoading(false);
    }
  }, [currentPage, pageSize, searchQuery, roleFilter, physicalFilter, waiverFilter]);

  useEffect(() => {
    if (isAuthenticated && isTeamMember && !isLoading) {
      loadAthletes();
    }
  }, [loadAthletes, isAuthenticated, isTeamMember, isLoading]);

  const handleSearch = (query: string) => {
    setSearchQuery(query);
    setCurrentPage(1);
  };

  const handleRoleFilter = (role: AthleteRole | '') => {
    setRoleFilter(role);
    setCurrentPage(1);
  };

  const handlePhysicalFilter = (hasPhysical: boolean | '') => {
    setPhysicalFilter(hasPhysical);
    setCurrentPage(1);
  };

  const handleWaiverFilter = (hasWaiver: boolean | '') => {
    setWaiverFilter(hasWaiver);
    setCurrentPage(1);
  };

  const handlePageChange = (page: number) => {
    setCurrentPage(page);
  };

  const handleEdit = (athlete: AthleteDto) => {
    setAthleteToEdit(athlete);
    setShowEditModal(true);
  };

  const handleDelete = (athlete: AthleteDto) => {
    setAthleteToDelete(athlete);
    setShowDeleteModal(true);
  };

  const handleProfile = (athlete: AthleteDto) => {
    setAthleteToView(athlete);
    setShowProfileModal(true);
  };

  const handleTogglePhysical = async (athlete: AthleteDto) => {
    try {
      setError(null);
      await athletesApi.updatePhysicalStatus(athlete.id, !athlete.hasPhysicalOnFile);
      
      // Update local state
      setAthletes(prev => prev.map(a => 
        a.id === athlete.id 
          ? { ...a, hasPhysicalOnFile: !a.hasPhysicalOnFile }
          : a
      ));
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('Failed to update physical status. Please try again.');
      }
      console.error('Error updating physical status:', err);
    }
  };

  const handleToggleWaiver = async (athlete: AthleteDto) => {
    try {
      setError(null);
      await athletesApi.updateWaiverStatus(athlete.id, !athlete.hasWaiverSigned);
      
      // Update local state
      setAthletes(prev => prev.map(a => 
        a.id === athlete.id 
          ? { ...a, hasWaiverSigned: !a.hasWaiverSigned }
          : a
      ));
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('Failed to update waiver status. Please try again.');
      }
      console.error('Error updating waiver status:', err);
    }
  };

  const confirmDelete = async () => {
    if (!athleteToDelete) return;
    
    try {
      setDeleteLoading(true);
      setError(null);
      
      await athletesApi.deleteAthlete(athleteToDelete.id);
      
      // Remove the athlete from the current list
      setAthletes(prev => prev.filter(athlete => athlete.id !== athleteToDelete.id));
      setTotalCount(prev => prev - 1);
      
      setShowDeleteModal(false);
      setAthleteToDelete(null);
      
      // Refresh the athletes list to get accurate pagination
      loadAthletes();
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('Failed to remove athlete. Please try again.');
      }
      console.error('Error removing athlete:', err);
    } finally {
      setDeleteLoading(false);
    }
  };

  const handleCreateAthlete = () => {
    setShowCreateModal(true);
  };

  const handleAthleteCreated = () => {
    // Refresh the athletes list
    loadAthletes();
  };

  const handleAthleteUpdated = () => {
    // Refresh the athletes list
    loadAthletes();
  };

  const startIndex = (currentPage - 1) * pageSize + 1;
  const endIndex = Math.min(currentPage * pageSize, totalCount);

  // Check if any filters are active
  const hasActiveFilters = searchQuery || roleFilter !== '' || physicalFilter !== '' || waiverFilter !== '';

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
          pageTitle="Team Roster"
          currentSection="roster"
          variant="team"
          navigationItems={TEAM_NAV_ITEMS}
          siteName={teamName}
          logoUrl={themeData?.logoUrl}
          showTeamTheme={true}
        >
          <div>
            {/* Header */}
            <div className="mb-8">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-gray-600 mt-1">Manage your team athletes</p>
                </div>
                {canCreateAthlete() && (
                  <button 
                    onClick={handleCreateAthlete}
                    className="bg-blue-600 text-white px-4 py-2 rounded-lg font-medium hover:bg-blue-700 transition-colors flex items-center"
                  >
                    <FontAwesomeIcon icon={faPlus} className="w-4 h-4 mr-2" />
                    Add Athlete
                  </button>
                )}
              </div>
            </div>

            {/* Search and filters */}
            <div className="bg-white rounded-lg shadow-sm border border-gray-200 mb-6">
              <div className="p-4">
                <div className="flex flex-col sm:flex-row gap-4">
                  <div className="flex-1">
                    <div className="relative">
                      <FontAwesomeIcon icon={faSearch} className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
                      <input
                        type="text"
                        placeholder="Search athletes by name or email..."
                        value={searchQuery}
                        onChange={(e) => handleSearch(e.target.value)}
                        className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                      />
                    </div>
                  </div>
                  <button
                    onClick={() => setFiltersExpanded(!filtersExpanded)}
                    className={`flex items-center px-4 py-2 border rounded-lg font-medium transition-colors ${
                      hasActiveFilters 
                        ? 'border-blue-500 text-blue-600 bg-blue-50' 
                        : 'border-gray-300 text-gray-700 hover:bg-gray-50'
                    }`}
                  >
                    <FontAwesomeIcon icon={faFilter} className="w-4 h-4 mr-2" />
                    Filters
                    <FontAwesomeIcon 
                      icon={filtersExpanded ? faChevronUp : faChevronDown} 
                      className="w-4 h-4 ml-2" 
                    />
                  </button>
                </div>
                
                {filtersExpanded && (
                  <div className="mt-4 pt-4 border-t border-gray-200">
                    <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                      <div>
                        <label htmlFor="role-filter" className="block text-sm font-medium text-gray-700 mb-1">
                          Role
                        </label>
                        <select
                          id="role-filter"
                          value={roleFilter}
                          onChange={(e) => handleRoleFilter(e.target.value === '' ? '' : parseInt(e.target.value) as AthleteRole)}
                          className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                        >
                          <option value="">All Roles</option>
                          <option value={AthleteRole.Athlete}>Athlete</option>
                          <option value={AthleteRole.Captain}>Captain</option>
                        </select>
                      </div>
                      
                      <div>
                        <label htmlFor="physical-filter" className="block text-sm font-medium text-gray-700 mb-1">
                          Physical Status
                        </label>
                        <select
                          id="physical-filter"
                          value={physicalFilter.toString()}
                          onChange={(e) => handlePhysicalFilter(e.target.value === '' ? '' : e.target.value === 'true')}
                          className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                        >
                          <option value="">All</option>
                          <option value="true">Physical Complete</option>
                          <option value="false">Physical Needed</option>
                        </select>
                      </div>
                      
                      <div>
                        <label htmlFor="waiver-filter" className="block text-sm font-medium text-gray-700 mb-1">
                          Waiver Status
                        </label>
                        <select
                          id="waiver-filter"
                          value={waiverFilter.toString()}
                          onChange={(e) => handleWaiverFilter(e.target.value === '' ? '' : e.target.value === 'true')}
                          className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                        >
                          <option value="">All</option>
                          <option value="true">Waiver Signed</option>
                          <option value="false">Waiver Needed</option>
                        </select>
                      </div>
                    </div>
                  </div>
                )}
              </div>
            </div>

            {/* Error message */}
            {error && (
              <div className="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
                <div className="flex">
                  <FontAwesomeIcon icon={faExclamationTriangle} className="w-5 h-5 text-red-400 mr-3 mt-0.5" />
                  <div className="text-red-700">{error}</div>
                </div>
              </div>
            )}

            {/* Athletes table */}
            <div className="bg-white rounded-lg shadow-sm border border-gray-200">
              {loading ? (
                <div className="flex items-center justify-center py-12">
                  <FontAwesomeIcon icon={faSpinner} className="w-8 h-8 text-blue-600" spin />
                  <span className="ml-3 text-gray-600">Loading athletes...</span>
                </div>
              ) : athletes.length === 0 ? (
                <div className="text-center py-12">
                  <FontAwesomeIcon icon={faUsers} className="w-12 h-12 text-gray-400 mb-4" />
                  <h3 className="text-lg font-medium text-gray-900 mb-2">No athletes found</h3>
                  <p className="text-gray-500 mb-4">
                    {hasActiveFilters ? 'Try adjusting your search or filters.' : 'Get started by adding your first athlete.'}
                  </p>
                  {canCreateAthlete() && !hasActiveFilters && (
                    <button
                      onClick={handleCreateAthlete}
                      className="bg-blue-600 text-white px-4 py-2 rounded-lg font-medium hover:bg-blue-700 transition-colors"
                    >
                      <FontAwesomeIcon icon={faPlus} className="w-4 h-4 mr-2" />
                      Add First Athlete
                    </button>
                  )}
                </div>
              ) : (
                <>
                  {/* Desktop table */}
                  <div className="hidden md:block overflow-x-auto">
                    <table className="min-w-full divide-y divide-gray-200">
                      <thead className="bg-gray-50">
                        <tr>
                          <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                            Athlete
                          </th>
                          <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                            Role
                          </th>
                          <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                            Grade
                          </th>
                          <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                            Physical
                          </th>
                          <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                            Waiver
                          </th>
                          <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                            Emergency Contact
                          </th>
                          <th className="relative px-6 py-3">
                            <span className="sr-only">Actions</span>
                          </th>
                        </tr>
                      </thead>
                      <tbody className="bg-white divide-y divide-gray-200">
                        {athletes.map((athlete) => (
                          <tr key={athlete.id} className="hover:bg-gray-50">
                            <td className="px-6 py-4 whitespace-nowrap">
                              <div className="flex items-center">
                                <div>
                                  <div className="text-sm font-medium text-gray-900">
                                    {athlete.firstName} {athlete.lastName}
                                  </div>
                                  {athlete.email && (
                                    <div className="text-sm text-gray-500">{athlete.email}</div>
                                  )}
                                </div>
                              </div>
                            </td>
                            <td className="px-6 py-4 whitespace-nowrap">
                              <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                                athlete.role === AthleteRole.Captain
                                  ? 'bg-yellow-100 text-yellow-800'
                                  : 'bg-gray-100 text-gray-800'
                              }`}>
                                {getAthleteRoleDisplayName(athlete.role)}
                              </span>
                            </td>
                            <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                              {athlete.grade || '-'}
                            </td>
                            <td className="px-6 py-4 whitespace-nowrap">
                              <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                                athlete.hasPhysicalOnFile
                                  ? 'bg-green-100 text-green-800'
                                  : 'bg-red-100 text-red-800'
                              }`}>
                                {athlete.hasPhysicalOnFile ? 'Complete' : 'Needed'}
                              </span>
                            </td>
                            <td className="px-6 py-4 whitespace-nowrap">
                              <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                                athlete.hasWaiverSigned
                                  ? 'bg-green-100 text-green-800'
                                  : 'bg-red-100 text-red-800'
                              }`}>
                                {athlete.hasWaiverSigned ? 'Signed' : 'Needed'}
                              </span>
                            </td>
                            <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                              {athlete.emergencyContactName || '-'}
                            </td>
                            <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                              <DropdownMenu
                                athlete={athlete}
                                onEdit={handleEdit}
                                onDelete={handleDelete}
                                onProfile={handleProfile}
                                onTogglePhysical={handleTogglePhysical}
                                onToggleWaiver={handleToggleWaiver}
                                canEdit={canEditAthlete(athlete)}
                                canDelete={canDeleteAthlete()}
                                canToggleStatus={canToggleStatus(athlete)}
                              />
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>

                  {/* Mobile cards */}
                  <div className="md:hidden">
                    <div className="divide-y divide-gray-200">
                      {athletes.map((athlete) => (
                        <div key={athlete.id} className="p-4">
                          <div className="flex items-center justify-between mb-3">
                            <div>
                              <h3 className="text-lg font-medium text-gray-900">
                                {athlete.firstName} {athlete.lastName}
                              </h3>
                              {athlete.email && (
                                <p className="text-sm text-gray-500">{athlete.email}</p>
                              )}
                            </div>
                            <DropdownMenu
                              athlete={athlete}
                              onEdit={handleEdit}
                              onDelete={handleDelete}
                              onProfile={handleProfile}
                              onTogglePhysical={handleTogglePhysical}
                              onToggleWaiver={handleToggleWaiver}
                              canEdit={canEditAthlete(athlete)}
                              canDelete={canDeleteAthlete()}
                              canToggleStatus={canToggleStatus(athlete)}
                            />
                          </div>
                          
                          <div className="grid grid-cols-2 gap-4 text-sm">
                            <div>
                              <span className="text-gray-500">Role:</span>
                              <span className={`ml-2 inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                                athlete.role === AthleteRole.Captain
                                  ? 'bg-yellow-100 text-yellow-800'
                                  : 'bg-gray-100 text-gray-800'
                              }`}>
                                {getAthleteRoleDisplayName(athlete.role)}
                              </span>
                            </div>
                            
                            <div>
                              <span className="text-gray-500">Grade:</span>
                              <span className="ml-2 text-gray-900">{athlete.grade || '-'}</span>
                            </div>
                            
                            <div>
                              <span className="text-gray-500">Physical:</span>
                              <span className={`ml-2 inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                                athlete.hasPhysicalOnFile
                                  ? 'bg-green-100 text-green-800'
                                  : 'bg-red-100 text-red-800'
                              }`}>
                                {athlete.hasPhysicalOnFile ? 'Complete' : 'Needed'}
                              </span>
                            </div>
                            
                            <div>
                              <span className="text-gray-500">Waiver:</span>
                              <span className={`ml-2 inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                                athlete.hasWaiverSigned
                                  ? 'bg-green-100 text-green-800'
                                  : 'bg-red-100 text-red-800'
                              }`}>
                                {athlete.hasWaiverSigned ? 'Signed' : 'Needed'}
                              </span>
                            </div>
                          </div>
                          
                          {athlete.emergencyContactName && (
                            <div className="mt-2 text-sm">
                              <span className="text-gray-500">Emergency Contact:</span>
                              <span className="ml-2 text-gray-900">{athlete.emergencyContactName}</span>
                              {athlete.emergencyContactPhone && (
                                <span className="ml-2 text-gray-500">({athlete.emergencyContactPhone})</span>
                              )}
                            </div>
                          )}
                        </div>
                      ))}
                    </div>
                  </div>

                  {/* Pagination */}
                  {totalPages > 1 && (
                    <div className="px-6 py-3 flex items-center justify-between border-t border-gray-200 bg-gray-50">
                      <div className="flex-1 flex justify-between sm:hidden">
                        <button
                          onClick={() => handlePageChange(currentPage - 1)}
                          disabled={currentPage === 1}
                          className="relative inline-flex items-center px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                        >
                          Previous
                        </button>
                        <button
                          onClick={() => handlePageChange(currentPage + 1)}
                          disabled={currentPage === totalPages}
                          className="ml-3 relative inline-flex items-center px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                        >
                          Next
                        </button>
                      </div>
                      <div className="hidden sm:flex-1 sm:flex sm:items-center sm:justify-between">
                        <div>
                          <p className="text-sm text-gray-700">
                            Showing <span className="font-medium">{startIndex}</span> to{' '}
                            <span className="font-medium">{endIndex}</span> of{' '}
                            <span className="font-medium">{totalCount}</span> results
                          </p>
                        </div>
                        <div>
                          <nav className="relative z-0 inline-flex rounded-md shadow-sm -space-x-px">
                            <button
                              onClick={() => handlePageChange(currentPage - 1)}
                              disabled={currentPage === 1}
                              className="relative inline-flex items-center px-2 py-2 rounded-l-md border border-gray-300 bg-white text-sm font-medium text-gray-500 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                            >
                              <FontAwesomeIcon icon={faChevronLeft} className="w-4 h-4" />
                            </button>
                            {Array.from({ length: totalPages }, (_, i) => i + 1).map((page) => (
                              <button
                                key={page}
                                onClick={() => handlePageChange(page)}
                                className={`relative inline-flex items-center px-4 py-2 border text-sm font-medium ${
                                  page === currentPage
                                    ? 'z-10 bg-blue-50 border-blue-500 text-blue-600'
                                    : 'bg-white border-gray-300 text-gray-500 hover:bg-gray-50'
                                }`}
                              >
                                {page}
                              </button>
                            ))}
                            <button
                              onClick={() => handlePageChange(currentPage + 1)}
                              disabled={currentPage === totalPages}
                              className="relative inline-flex items-center px-2 py-2 rounded-r-md border border-gray-300 bg-white text-sm font-medium text-gray-500 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                            >
                              <FontAwesomeIcon icon={faChevronRight} className="w-4 h-4" />
                            </button>
                          </nav>
                        </div>
                      </div>
                    </div>
                  )}
                </>
              )}
            </div>
          </div>

          {/* Modals */}
          {showCreateModal && (
            <CreateAthleteModal
              onClose={() => setShowCreateModal(false)}
              onAthleteCreated={handleAthleteCreated}
            />
          )}

          {showEditModal && athleteToEdit && (
            <EditAthleteModal
              athlete={athleteToEdit}
              onClose={() => {
                setShowEditModal(false);
                setAthleteToEdit(null);
              }}
              onAthleteUpdated={handleAthleteUpdated}
            />
          )}

          {showProfileModal && athleteToView && (
            <AthleteProfileModal
              athlete={athleteToView}
              onClose={() => {
                setShowProfileModal(false);
                setAthleteToView(null);
              }}
              canEdit={canEditAthlete(athleteToView)}
              onProfileUpdated={handleAthleteUpdated}
            />
          )}

          {showDeleteModal && athleteToDelete && (
            <ConfirmationModal
              isOpen={showDeleteModal}
              onClose={() => {
                setShowDeleteModal(false);
                setAthleteToDelete(null);
              }}
              onConfirm={confirmDelete}
              title="Remove Athlete from Team"
              message={`Are you sure you want to remove ${athleteToDelete.firstName} ${athleteToDelete.lastName} from the team? This will remove them from the roster and their team membership, but will not delete their user account.`}
              confirmText="Remove from Team"
              loading={deleteLoading}
              confirmButtonClass="bg-red-600 text-white hover:bg-red-700 focus:ring-red-500"
            />
          )}
        </BaseLayout>
      </TeamThemeProvider>
    );
  }

  // For non-authenticated users or non-team members, use the existing public layout
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

        {/* Main content for non-team members */}
        <main className="p-0">
          {/* Access Denied - not a team member */}
          <div className="min-h-screen flex items-center justify-center">
            <div className="text-center">
              <h1 className="text-2xl font-bold text-gray-900 mb-4">Access Denied</h1>
              <p className="text-gray-600">You need to be a team member to access the roster.</p>
              {!isAuthenticated && (
                <a
                  href="/login"
                  className="mt-4 inline-block bg-blue-600 text-white px-4 py-2 rounded-lg font-medium hover:bg-blue-700 transition-colors"
                >
                  Login to Continue
                </a>
              )}
            </div>
          </div>
        </main>
      </div>
              </TeamThemeProvider>
   );
  } 