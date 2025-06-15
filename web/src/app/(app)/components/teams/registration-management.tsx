'use client';

import { useState, useEffect } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { 
  faPlus,
  faEdit,
  faCheck,
  faTimes,
  faExclamationTriangle,
  faEye,
  faEyeSlash,
  faClock,
  faListUl
} from '@fortawesome/free-solid-svg-icons';
import { registrationApi } from '@/utils/api';
import { 
  TeamRegistrationWindowDto, 
  CreateRegistrationWindowDto, 
  UpdateRegistrationWindowDto,
  TeamRegistrationDto,
  RegistrationStatus
} from '@/types/registration';

interface RegistrationManagementProps {
  teamId: string;
  teamName: string;
}

interface WindowFormData {
  startDate: string;
  endDate: string;
  maxRegistrations: number;
  registrationPasscode: string;
}

export default function RegistrationManagement({ teamId }: RegistrationManagementProps) {
  const [windows, setWindows] = useState<TeamRegistrationWindowDto[]>([]);
  const [registrations, setRegistrations] = useState<TeamRegistrationDto[]>([]);
  const [waitlist, setWaitlist] = useState<TeamRegistrationDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [editingWindow, setEditingWindow] = useState<TeamRegistrationWindowDto | null>(null);
  const [showRegistrations, setShowRegistrations] = useState(false);
  const [showWaitlist, setShowWaitlist] = useState(false);
  const [showPasscode, setShowPasscode] = useState<Record<string, boolean>>({});
  
  const [formData, setFormData] = useState<WindowFormData>({
    startDate: '',
    endDate: '',
    maxRegistrations: 30,
    registrationPasscode: ''
  });

  useEffect(() => {
    loadData();
  }, [teamId]); // eslint-disable-line react-hooks/exhaustive-deps

  const loadData = async () => {
    setIsLoading(true);
    try {
      const [windowsData, registrationsData, waitlistData] = await Promise.all([
        registrationApi.getRegistrationWindows(teamId),
        registrationApi.getRegistrations(teamId),
        registrationApi.getWaitlist(teamId)
      ]);
      
      setWindows(windowsData);
      setRegistrations(registrationsData);
      setWaitlist(waitlistData);
    } catch (error) {
      console.error('Failed to load registration data:', error);
      setError('Failed to load registration data. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleCreateWindow = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    try {
      const dto: CreateRegistrationWindowDto = {
        startDate: new Date(formData.startDate).toISOString(),
        endDate: new Date(formData.endDate).toISOString(),
        maxRegistrations: formData.maxRegistrations,
        registrationPasscode: formData.registrationPasscode
      };

      await registrationApi.createRegistrationWindow(teamId, dto);
      await loadData();
      setShowCreateForm(false);
      resetForm();
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to create registration window';
      setError(errorMessage);
    }
  };

  const handleUpdateWindow = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingWindow) return;
    
    setError('');

    try {
      const dto: UpdateRegistrationWindowDto = {
        startDate: new Date(formData.startDate).toISOString(),
        endDate: new Date(formData.endDate).toISOString(),
        maxRegistrations: formData.maxRegistrations,
        registrationPasscode: formData.registrationPasscode
      };

      await registrationApi.updateRegistrationWindow(teamId, editingWindow.id, dto);
      await loadData();
      setEditingWindow(null);
      resetForm();
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to update registration window';
      setError(errorMessage);
    }
  };

  const handleUpdateRegistrationStatus = async (registrationId: string, status: RegistrationStatus) => {
    try {
      await registrationApi.updateRegistrationStatus(teamId, registrationId, { status });
      await loadData();
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to update registration status';
      setError(errorMessage);
    }
  };

  const startEdit = (window: TeamRegistrationWindowDto) => {
    setEditingWindow(window);
    setFormData({
      startDate: new Date(window.startDate).toISOString().split('T')[0],
      endDate: new Date(window.endDate).toISOString().split('T')[0],
      maxRegistrations: window.maxRegistrations,
      registrationPasscode: window.registrationPasscode
    });
    setShowCreateForm(true);
  };

  const resetForm = () => {
    setFormData({
      startDate: '',
      endDate: '',
      maxRegistrations: 30,
      registrationPasscode: ''
    });
  };

  const cancelEdit = () => {
    setEditingWindow(null);
    setShowCreateForm(false);
    resetForm();
  };

  const togglePasscodeVisibility = (windowId: string) => {
    setShowPasscode(prev => ({ ...prev, [windowId]: !prev[windowId] }));
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  };

  const getStatusBadgeClass = (status: RegistrationStatus) => {
    switch (status) {
      case RegistrationStatus.Pending:
        return 'bg-yellow-100 text-yellow-800';
      case RegistrationStatus.Approved:
        return 'bg-green-100 text-green-800';
      case RegistrationStatus.Rejected:
        return 'bg-red-100 text-red-800';
      case RegistrationStatus.Waitlisted:
        return 'bg-blue-100 text-blue-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  const getStatusText = (status: RegistrationStatus) => {
    switch (status) {
      case RegistrationStatus.Pending:
        return 'Pending';
      case RegistrationStatus.Approved:
        return 'Approved';
      case RegistrationStatus.Rejected:
        return 'Rejected';
      case RegistrationStatus.Waitlisted:
        return 'Waitlisted';
      default:
        return 'Unknown';
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center p-8">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold text-gray-900">Registration Management</h2>
        <div className="flex space-x-3">
          <button
            onClick={() => setShowRegistrations(!showRegistrations)}
            className="inline-flex items-center px-4 py-2 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
          >
            <FontAwesomeIcon icon={faListUl} className="mr-2" />
            View Registrations ({registrations.length})
          </button>
          <button
            onClick={() => setShowWaitlist(!showWaitlist)}
            className="inline-flex items-center px-4 py-2 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
          >
            <FontAwesomeIcon icon={faClock} className="mr-2" />
            View Waitlist ({waitlist.length})
          </button>
          <button
            onClick={() => setShowCreateForm(true)}
            className="inline-flex items-center px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
          >
            <FontAwesomeIcon icon={faPlus} className="mr-2" />
            Create Registration Window
          </button>
        </div>
      </div>

      {error && (
        <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
          <div className="flex items-start">
            <FontAwesomeIcon icon={faExclamationTriangle} className="text-red-500 mt-0.5 mr-3 flex-shrink-0" />
            <div className="flex-1">
              <p className="text-sm text-red-700">{error}</p>
            </div>
            <button
              onClick={() => setError('')}
              className="text-red-400 hover:text-red-600 ml-2 flex-shrink-0"
            >
              <FontAwesomeIcon icon={faTimes} className="w-4 h-4" />
            </button>
          </div>
        </div>
      )}

      {/* Create/Edit Window Form */}
      {showCreateForm && (
        <div className="bg-white p-6 border border-gray-200 rounded-lg shadow-sm">
          <h3 className="text-lg font-medium text-gray-900 mb-4">
            {editingWindow ? 'Edit Registration Window' : 'Create Registration Window'}
          </h3>
          
          <form onSubmit={editingWindow ? handleUpdateWindow : handleCreateWindow} className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Start Date *
                </label>
                <input
                  type="date"
                  required
                  value={formData.startDate}
                  onChange={(e) => setFormData(prev => ({ ...prev, startDate: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                />
              </div>
              
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  End Date *
                </label>
                <input
                  type="date"
                  required
                  value={formData.endDate}
                  onChange={(e) => setFormData(prev => ({ ...prev, endDate: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                />
              </div>
            </div>
            
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Maximum Registrations *
                </label>
                <input
                  type="number"
                  required
                  min="1"
                  value={formData.maxRegistrations}
                  onChange={(e) => setFormData(prev => ({ ...prev, maxRegistrations: parseInt(e.target.value) }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                />
              </div>
              
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Registration Passcode *
                </label>
                <input
                  type="text"
                  required
                  value={formData.registrationPasscode}
                  onChange={(e) => setFormData(prev => ({ ...prev, registrationPasscode: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                  placeholder="Enter a unique passcode"
                />
              </div>
            </div>
            
            <div className="flex justify-end space-x-3">
              <button
                type="button"
                onClick={cancelEdit}
                className="px-4 py-2 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
              >
                Cancel
              </button>
              <button
                type="submit"
                className="px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
              >
                {editingWindow ? 'Update Window' : 'Create Window'}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Registration Windows */}
      <div className="bg-white shadow-sm rounded-lg overflow-hidden">
        <div className="px-6 py-4 border-b border-gray-200">
          <h3 className="text-lg font-medium text-gray-900">Registration Windows</h3>
        </div>
        
        {windows.length === 0 ? (
          <div className="px-6 py-8 text-center">
            <p className="text-gray-500">No registration windows created yet.</p>
          </div>
        ) : (
          <div className="divide-y divide-gray-200">
            {windows.map((window) => (
              <div key={window.id} className="px-6 py-4">
                <div className="flex items-center justify-between">
                  <div className="flex-1">
                    <div className="flex items-center space-x-4">
                      <div>
                        <p className="text-sm font-medium text-gray-900">
                          {formatDate(window.startDate)} - {formatDate(window.endDate)}
                        </p>
                        <p className="text-sm text-gray-500">
                          Max Athletes: {window.maxRegistrations} | 
                          Status: <span className={window.isActive ? 'text-green-600' : 'text-gray-600'}>
                            {window.isActive ? 'Active' : 'Inactive'}
                          </span>
                        </p>
                      </div>
                      <div>
                        <p className="text-sm text-gray-700">
                          Passcode: 
                          <span className="ml-2 font-mono">
                            {showPasscode[window.id] ? window.registrationPasscode : '••••••••'}
                          </span>
                          <button
                            onClick={() => togglePasscodeVisibility(window.id)}
                            className="ml-2 text-gray-400 hover:text-gray-600"
                          >
                            <FontAwesomeIcon 
                              icon={showPasscode[window.id] ? faEyeSlash : faEye} 
                              className="w-4 h-4" 
                            />
                          </button>
                        </p>
                      </div>
                    </div>
                  </div>
                  <div className="flex items-center space-x-2">
                    <button
                      onClick={() => startEdit(window)}
                      className="p-2 text-gray-400 hover:text-gray-600"
                    >
                      <FontAwesomeIcon icon={faEdit} className="w-4 h-4" />
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Registrations List */}
      {showRegistrations && (
        <div className="bg-white shadow-sm rounded-lg overflow-hidden">
          <div className="px-6 py-4 border-b border-gray-200">
            <h3 className="text-lg font-medium text-gray-900">All Registrations</h3>
          </div>
          
          {registrations.length === 0 ? (
            <div className="px-6 py-8 text-center">
              <p className="text-gray-500">No registrations yet.</p>
            </div>
          ) : (
            <div className="divide-y divide-gray-200">
              {registrations.map((registration) => (
                <div key={registration.id} className="px-6 py-4">
                  <div className="flex items-center justify-between">
                    <div className="flex-1">
                      <p className="text-sm font-medium text-gray-900">
                        {registration.firstName} {registration.lastName}
                      </p>
                      <p className="text-sm text-gray-500">
                        {registration.email} | Emergency: {registration.emergencyContactName} ({registration.emergencyContactPhone})
                      </p>
                      <p className="text-sm text-gray-500">
                        Athletes: {registration.athletes.map(a => `${a.firstName} ${a.lastName}`).join(', ')}
                      </p>
                      <p className="text-xs text-gray-400">
                        Submitted: {formatDate(registration.createdOn)}
                      </p>
                    </div>
                    <div className="flex items-center space-x-3">
                      <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${getStatusBadgeClass(registration.status)}`}>
                        {getStatusText(registration.status)}
                      </span>
                      {registration.status === RegistrationStatus.Pending && (
                        <div className="flex space-x-2">
                          <button
                            onClick={() => handleUpdateRegistrationStatus(registration.id, RegistrationStatus.Approved)}
                            className="p-1 text-green-600 hover:text-green-800"
                            title="Approve"
                          >
                            <FontAwesomeIcon icon={faCheck} className="w-4 h-4" />
                          </button>
                          <button
                            onClick={() => handleUpdateRegistrationStatus(registration.id, RegistrationStatus.Rejected)}
                            className="p-1 text-red-600 hover:text-red-800"
                            title="Reject"
                          >
                            <FontAwesomeIcon icon={faTimes} className="w-4 h-4" />
                          </button>
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Waitlist */}
      {showWaitlist && (
        <div className="bg-white shadow-sm rounded-lg overflow-hidden">
          <div className="px-6 py-4 border-b border-gray-200">
            <h3 className="text-lg font-medium text-gray-900">Waitlist</h3>
          </div>
          
          {waitlist.length === 0 ? (
            <div className="px-6 py-8 text-center">
              <p className="text-gray-500">No one on the waitlist.</p>
            </div>
          ) : (
            <div className="divide-y divide-gray-200">
              {waitlist.map((registration, index) => (
                <div key={registration.id} className="px-6 py-4">
                  <div className="flex items-center justify-between">
                    <div className="flex-1">
                      <p className="text-sm font-medium text-gray-900">
                        #{index + 1} - {registration.firstName} {registration.lastName}
                      </p>
                      <p className="text-sm text-gray-500">
                        {registration.email} | Athletes: {registration.athletes.length}
                      </p>
                      <p className="text-xs text-gray-400">
                        Waitlisted: {formatDate(registration.createdOn)}
                      </p>
                    </div>
                    <div className="flex items-center space-x-3">
                      <button
                        onClick={() => handleUpdateRegistrationStatus(registration.id, RegistrationStatus.Approved)}
                        className="px-3 py-1 text-sm bg-blue-600 text-white rounded hover:bg-blue-700"
                      >
                        Approve from Waitlist
                      </button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
} 