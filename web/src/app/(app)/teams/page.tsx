'use client';

import { useState, useEffect, useCallback } from 'react';

// Note: metadata export must be in a non-client component, so we'll use a different approach
// We'll use useEffect to set the document title dynamically
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { 
  faBuilding, 
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
  faChevronUp
} from '@fortawesome/free-solid-svg-icons';
import BaseLayout from '@/components/layouts/base-layout';
import { ADMIN_NAV_ITEMS } from '@/components/layouts/navigation-config';
import CreateTeamModal from '../components/teams/create-team-modal';
import EditTeamModal from '../components/teams/edit-team-modal';
import ConfirmationModal from '@/components/ui/confirmation-modal';
import { GlobalAdminTeamDto, TeamStatus, TeamTier, TeamsApiParams } from '@/types/team';
import { teamsApi, ApiError } from '@/utils/api';

interface DropdownMenuProps {
  team: GlobalAdminTeamDto;
  onEdit: (team: GlobalAdminTeamDto) => void;
  onDelete: (team: GlobalAdminTeamDto) => void;
  onPurge: (team: GlobalAdminTeamDto) => void;
}

function DropdownMenu({ team, onEdit, onDelete, onPurge }: DropdownMenuProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [dropdownPosition, setDropdownPosition] = useState({ top: 0, right: 0, dropUp: false });

  const handleToggle = (e: React.MouseEvent) => {
    if (!isOpen) {
      // Get button position relative to viewport
      const rect = e.currentTarget.getBoundingClientRect();
      const spaceBelow = window.innerHeight - rect.bottom;
      const dropdownHeight = 120; // More accurate height for 3 menu items
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
                  onEdit(team);
                  setIsOpen(false);
                }}
                className="flex items-center w-full px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
              >
                <FontAwesomeIcon icon={faEdit} className="w-4 h-4 mr-3" />
                Edit Team
              </button>
              <button
                onClick={() => {
                  onDelete(team);
                  setIsOpen(false);
                }}
                className="flex items-center w-full px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
              >
                <FontAwesomeIcon icon={faTrash} className="w-4 h-4 mr-3" />
                Delete Team
              </button>
              <button
                onClick={() => {
                  onPurge(team);
                  setIsOpen(false);
                }}
                className="flex items-center w-full px-4 py-2 text-sm text-red-600 hover:bg-red-50"
              >
                <FontAwesomeIcon icon={faTrashAlt} className="w-4 h-4 mr-3" />
                Purge Team
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}

const getStatusBadgeClass = (status: TeamStatus): string => {
  switch (status) {
    case TeamStatus.Active:
      return 'bg-green-100 text-green-800';
    case TeamStatus.Suspended:
      return 'bg-red-100 text-red-800';
    case TeamStatus.Expired:
      return 'bg-yellow-100 text-yellow-800';
    case TeamStatus.PendingSetup:
      return 'bg-blue-100 text-blue-800';
    default:
      return 'bg-gray-100 text-gray-800';
  }
};

const getStatusLabel = (status: TeamStatus): string => {
  switch (status) {
    case TeamStatus.Active:
      return 'Active';
    case TeamStatus.Suspended:
      return 'Suspended';
    case TeamStatus.Expired:
      return 'Expired';
    case TeamStatus.PendingSetup:
      return 'Pending Setup';
    default:
      return 'Unknown';
  }
};

const getTierBadgeClass = (tier: TeamTier): string => {
  switch (tier) {
    case TeamTier.Free:
      return 'bg-gray-100 text-gray-800';
    case TeamTier.Standard:
      return 'bg-blue-100 text-blue-800';
    case TeamTier.Premium:
      return 'bg-purple-100 text-purple-800';
    default:
      return 'bg-gray-100 text-gray-800';
  }
};

const getTierLabel = (tier: TeamTier): string => {
  switch (tier) {
    case TeamTier.Free:
      return 'Free';
    case TeamTier.Standard:
      return 'Standard';
    case TeamTier.Premium:
      return 'Premium';
    default:
      return 'Unknown';
  }
};

export default function AdminTeamsPage() {
  const [teams, setTeams] = useState<GlobalAdminTeamDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState<TeamStatus | ''>('');
  const [tierFilter, setTierFilter] = useState<TeamTier | ''>('');
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [pageSize] = useState(10);
  const [filtersExpanded, setFiltersExpanded] = useState(false);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [selectedTeam, setSelectedTeam] = useState<GlobalAdminTeamDto | null>(null);
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [showPurgeModal, setShowPurgeModal] = useState(false);
  const [teamToDelete, setTeamToDelete] = useState<GlobalAdminTeamDto | null>(null);
  const [teamToPurge, setTeamToPurge] = useState<GlobalAdminTeamDto | null>(null);
  const [deleteLoading, setDeleteLoading] = useState(false);
  const [purgeLoading, setPurgeLoading] = useState(false);

  const loadTeams = useCallback(async (params: TeamsApiParams = {}) => {
    try {
      setLoading(true);
      setError(null);
      
      const response = await teamsApi.getTeams({
        pageNumber: currentPage,
        pageSize,
        searchQuery: searchQuery || undefined,
        status: statusFilter !== '' ? statusFilter : undefined,
        tier: tierFilter !== '' ? tierFilter : undefined,
        ...params
      });
      
      setTeams(response.items);
      setTotalPages(response.totalPages);
      setTotalCount(response.totalCount);
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('Failed to load teams. Please try again.');
      }
      console.error('Error loading teams:', err);
    } finally {
      setLoading(false);
    }
  }, [currentPage, pageSize, searchQuery, statusFilter, tierFilter]);

  useEffect(() => {
    loadTeams();
  }, [loadTeams]);

  // Set document title
  useEffect(() => {
    document.title = "TeamStride - Global Administration - Teams";
  }, []);

  const handleSearch = (query: string) => {
    setSearchQuery(query);
    setCurrentPage(1);
  };

  const handleStatusFilter = (status: TeamStatus | '') => {
    setStatusFilter(status);
    setCurrentPage(1);
  };

  const handleTierFilter = (tier: TeamTier | '') => {
    setTierFilter(tier);
    setCurrentPage(1);
  };

  const handlePageChange = (page: number) => {
    setCurrentPage(page);
  };

  const handleEdit = (team: GlobalAdminTeamDto) => {
    setSelectedTeam(team);
    setShowEditModal(true);
  };

  const handleDelete = (team: GlobalAdminTeamDto) => {
    setTeamToDelete(team);
    setShowDeleteModal(true);
  };

  const handlePurge = (team: GlobalAdminTeamDto) => {
    setTeamToPurge(team);
    setShowPurgeModal(true);
  };

  const confirmDelete = async () => {
    if (!teamToDelete) return;
    
    try {
      setDeleteLoading(true);
      setError(null);
      
      await teamsApi.deleteTeam(teamToDelete.id);
      
      // Remove the team from the current list or refresh
      setTeams(prev => prev.filter(team => team.id !== teamToDelete.id));
      setTotalCount(prev => prev - 1);
      
      setShowDeleteModal(false);
      setTeamToDelete(null);
      
      // Refresh the teams list to get accurate pagination
      loadTeams();
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('Failed to delete team. Please try again.');
      }
      console.error('Error deleting team:', err);
    } finally {
      setDeleteLoading(false);
    }
  };

  const confirmPurge = async () => {
    if (!teamToPurge) return;
    
    try {
      setPurgeLoading(true);
      setError(null);
      
      await teamsApi.purgeTeam(teamToPurge.id);
      
      // Remove the team from the current list
      setTeams(prev => prev.filter(team => team.id !== teamToPurge.id));
      setTotalCount(prev => prev - 1);
      
      setShowPurgeModal(false);
      setTeamToPurge(null);
      
      // Refresh the teams list to get accurate pagination
      loadTeams();
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('Failed to purge team. Please try again.');
      }
      console.error('Error purging team:', err);
    } finally {
      setPurgeLoading(false);
    }
  };

  const handleTeamCreated = (newTeam: GlobalAdminTeamDto) => {
    // Add the new team to the current list if it matches the current filters
    setTeams(prev => [newTeam, ...prev]);
    setTotalCount(prev => prev + 1);
    // Refresh the teams list to get accurate pagination
    loadTeams();
  };

  const handleTeamUpdated = (updatedTeam: GlobalAdminTeamDto) => {
    // Update the team in the current list
    setTeams(prev => prev.map(team => 
      team.id === updatedTeam.id ? updatedTeam : team
    ));
    // Refresh the teams list to ensure accurate data
    loadTeams();
  };

  const startIndex = (currentPage - 1) * pageSize + 1;
  const endIndex = Math.min(currentPage * pageSize, totalCount);

  // Check if any filters are active
  const hasActiveFilters = searchQuery || statusFilter !== '' || tierFilter !== '';

  return (
    <BaseLayout 
      pageTitle="Team Management" 
      currentSection="teams"
      variant="admin"
      navigationItems={ADMIN_NAV_ITEMS}
      siteName="TeamStride"
    >
      {/* Page Actions */}
      <div className="mb-8 flex items-center justify-between">
        <p className="text-gray-600">Manage teams across the TeamStride platform</p>
        <button 
          onClick={() => setShowCreateModal(true)}
          className="bg-blue-600 text-white px-4 py-2 rounded-lg font-medium hover:bg-blue-700 transition-colors flex items-center"
        >
          <FontAwesomeIcon icon={faPlus} className="w-4 h-4 mr-2" />
          Create Team
        </button>
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
                  placeholder="Search teams by name, subdomain, or owner..."
                  value={searchQuery}
                  onChange={(e) => handleSearch(e.target.value)}
                  className="w-full pl-10 pr-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
              <select 
                value={statusFilter}
                onChange={(e) => handleStatusFilter(e.target.value as TeamStatus | '')}
                className="border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value="">All Statuses</option>
                <option value={TeamStatus.Active}>Active</option>
                <option value={TeamStatus.Suspended}>Suspended</option>
                <option value={TeamStatus.Expired}>Expired</option>
                <option value={TeamStatus.PendingSetup}>Pending Setup</option>
              </select>
              <select 
                value={tierFilter}
                onChange={(e) => handleTierFilter(e.target.value as TeamTier | '')}
                className="border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value="">All Tiers</option>
                <option value={TeamTier.Free}>Free</option>
                <option value={TeamTier.Standard}>Standard</option>
                <option value={TeamTier.Premium}>Premium</option>
              </select>
            </div>
          </div>
        </div>

        {/* Teams table */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200">
          <div className="p-6">
            <div className="flex items-center justify-between mb-6">
              <div className="flex items-center">
                <FontAwesomeIcon icon={faBuilding} className="w-5 h-5 text-blue-600 mr-2" />
                <h2 className="text-xl font-semibold text-gray-900">Teams</h2>
              </div>
              {totalCount > 0 && (
                <p className="text-sm text-gray-500">
                  Showing {startIndex}-{endIndex} of {totalCount} teams
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
                <p className="text-gray-500">Loading teams...</p>
              </div>
            ) : teams.length === 0 ? (
              <div className="text-center py-12">
                <FontAwesomeIcon icon={faBuilding} className="w-12 h-12 text-gray-300 mb-4" />
                <h3 className="text-lg font-medium text-gray-900 mb-2">No teams found</h3>
                <p className="text-gray-500 mb-4">
                  {searchQuery || statusFilter !== '' || tierFilter !== '' 
                    ? 'Try adjusting your search or filters.'
                    : 'No teams have been created yet.'}
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
                          Team
                        </th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                          Owner
                        </th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                          Status
                        </th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                          Tier
                        </th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                          Members
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
                      {teams.map((team) => (
                        <tr key={team.id} className="hover:bg-gray-50">
                          <td className="px-6 py-4 whitespace-nowrap">
                            <div>
                              <div className="text-sm font-medium text-gray-900">{team.name}</div>
                              <div className="text-sm text-gray-500">{team.subdomain}.teamstride.net</div>
                            </div>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap">
                            <div>
                              <div className="text-sm text-gray-900">{team.ownerDisplayName}</div>
                              <div className="text-sm text-gray-500">{team.ownerEmail}</div>
                            </div>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap">
                            <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${getStatusBadgeClass(team.status)}`}>
                              {getStatusLabel(team.status)}
                            </span>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap">
                            <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${getTierBadgeClass(team.tier)}`}>
                              {getTierLabel(team.tier)}
                            </span>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                            <div>
                              <div>{team.memberCount} total</div>
                              <div className="text-xs text-gray-500">{team.athleteCount} athletes</div>
                            </div>
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                            {new Date(team.createdOn).toLocaleDateString()}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                            <DropdownMenu
                              team={team}
                              onEdit={handleEdit}
                              onDelete={handleDelete}
                              onPurge={handlePurge}
                            />
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                {/* Mobile cards */}
                <div className="lg:hidden space-y-4">
                  {teams.map((team) => (
                    <div key={team.id} className="border border-gray-200 rounded-lg p-4">
                      <div className="flex items-center justify-between mb-3">
                        <h3 className="text-lg font-medium text-gray-900">{team.name}</h3>
                        <DropdownMenu
                          team={team}
                          onEdit={handleEdit}
                          onDelete={handleDelete}
                          onPurge={handlePurge}
                        />
                      </div>
                      <div className="space-y-2">
                        <p className="text-sm text-gray-600">{team.subdomain}.teamstride.net</p>
                        <div className="flex items-center space-x-2">
                          <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${getStatusBadgeClass(team.status)}`}>
                            {getStatusLabel(team.status)}
                          </span>
                          <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${getTierBadgeClass(team.tier)}`}>
                            {getTierLabel(team.tier)}
                          </span>
                        </div>
                        <div className="text-sm">
                          <p className="text-gray-900">Owner: {team.ownerDisplayName}</p>
                          <p className="text-gray-500">{team.ownerEmail}</p>
                        </div>
                        <div className="flex justify-between text-sm text-gray-500">
                          <span>{team.memberCount} members ({team.athleteCount} athletes)</span>
                          <span>Created {new Date(team.createdOn).toLocaleDateString()}</span>
                        </div>
                      </div>
                    </div>
                  ))}
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

      {/* Create Team Modal */}
      <CreateTeamModal
        isOpen={showCreateModal}
        onClose={() => setShowCreateModal(false)}
        onTeamCreated={handleTeamCreated}
      />

      {/* Edit Team Modal */}
      <EditTeamModal
        isOpen={showEditModal}
        onClose={() => setShowEditModal(false)}
        onTeamUpdated={handleTeamUpdated}
        team={selectedTeam}
      />

      {/* Delete Team Confirmation Modal */}
      <ConfirmationModal
        isOpen={showDeleteModal}
        onClose={() => {
          setShowDeleteModal(false);
          setTeamToDelete(null);
        }}
        onConfirm={confirmDelete}
        title="Delete Team"
        message={`Are you sure you want to delete "${teamToDelete?.name}"? This will soft delete the team and deactivate all its members. The team can be recovered later if needed.`}
        icon={<FontAwesomeIcon icon={faTrash} className="w-5 h-5 text-red-600" />}
        confirmText="Delete Team"
        loading={deleteLoading}
      />

      {/* Purge Team Confirmation Modal */}
      <ConfirmationModal
        isOpen={showPurgeModal}
        onClose={() => {
          setShowPurgeModal(false);
          setTeamToPurge(null);
        }}
        onConfirm={confirmPurge}
        title="Purge Team"
        message={`Are you sure you want to permanently purge "${teamToPurge?.name}"? This action cannot be undone and will permanently remove all team data, including members, activities, and settings.`}
        icon={<FontAwesomeIcon icon={faTrashAlt} className="w-5 h-5 text-red-600" />}
        confirmText="Purge Team"
        confirmButtonClass="bg-red-700 text-white hover:bg-red-800 focus:ring-red-600"
        loading={purgeLoading}
      />
    </BaseLayout>
  );
} 