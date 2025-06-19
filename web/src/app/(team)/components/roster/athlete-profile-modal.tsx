'use client';

import { useState } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faIdCard, faEdit } from '@fortawesome/free-solid-svg-icons';
import FormModal from '@/components/ui/form-modal';
import { AthleteDto, UpdateAthleteProfileDto, getGradeLevelDisplayName } from '@/types/athlete';
import { athletesApi, ApiError } from '@/utils/api';

interface AthleteProfileModalProps {
  athlete: AthleteDto;
  onClose: () => void;
  canEdit: boolean;
  onProfileUpdated: () => void;
}

export default function AthleteProfileModal({ athlete, onClose, canEdit, onProfileUpdated }: AthleteProfileModalProps) {
  const [isEditing, setIsEditing] = useState(false);
  const [formData, setFormData] = useState<UpdateAthleteProfileDto>({
    preferredEvents: athlete.profile?.preferredEvents || '',
    personalBests: athlete.profile?.personalBests || '',
    goals: athlete.profile?.goals || '',
    trainingNotes: athlete.profile?.trainingNotes || '',
    medicalNotes: athlete.profile?.medicalNotes || '',
    dietaryRestrictions: athlete.profile?.dietaryRestrictions || '',
    uniformSize: athlete.profile?.uniformSize || '',
    warmupRoutine: athlete.profile?.warmupRoutine || ''
  });
  
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    try {
      setLoading(true);
      setError(null);

      // Clean up form data - remove empty strings and only include changed values
      const cleanedData: UpdateAthleteProfileDto = {};
      
      if (formData.preferredEvents?.trim() !== (athlete.profile?.preferredEvents || '')) {
        cleanedData.preferredEvents = formData.preferredEvents?.trim() || undefined;
      }
      if (formData.personalBests?.trim() !== (athlete.profile?.personalBests || '')) {
        cleanedData.personalBests = formData.personalBests?.trim() || undefined;
      }
      if (formData.goals?.trim() !== (athlete.profile?.goals || '')) {
        cleanedData.goals = formData.goals?.trim() || undefined;
      }
      if (formData.trainingNotes?.trim() !== (athlete.profile?.trainingNotes || '')) {
        cleanedData.trainingNotes = formData.trainingNotes?.trim() || undefined;
      }
      if (formData.medicalNotes?.trim() !== (athlete.profile?.medicalNotes || '')) {
        cleanedData.medicalNotes = formData.medicalNotes?.trim() || undefined;
      }
      if (formData.dietaryRestrictions?.trim() !== (athlete.profile?.dietaryRestrictions || '')) {
        cleanedData.dietaryRestrictions = formData.dietaryRestrictions?.trim() || undefined;
      }
      if (formData.uniformSize?.trim() !== (athlete.profile?.uniformSize || '')) {
        cleanedData.uniformSize = formData.uniformSize?.trim() || undefined;
      }
      if (formData.warmupRoutine?.trim() !== (athlete.profile?.warmupRoutine || '')) {
        cleanedData.warmupRoutine = formData.warmupRoutine?.trim() || undefined;
      }

      // Only make request if there are changes
      if (Object.keys(cleanedData).length > 0) {
        await athletesApi.updateAthleteProfile(athlete.id, cleanedData);
        onProfileUpdated();
      }
      
      setIsEditing(false);
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('Failed to update profile. Please try again.');
      }
      console.error('Error updating profile:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (field: keyof UpdateAthleteProfileDto, value: string) => {
    setFormData(prev => ({
      ...prev,
      [field]: value
    }));
  };

  const handleEdit = () => {
    setIsEditing(true);
    setError(null);
  };

  const handleCancelEdit = () => {
    setIsEditing(false);
    setError(null);
    // Reset form data to original values
    setFormData({
      preferredEvents: athlete.profile?.preferredEvents || '',
      personalBests: athlete.profile?.personalBests || '',
      goals: athlete.profile?.goals || '',
      trainingNotes: athlete.profile?.trainingNotes || '',
      medicalNotes: athlete.profile?.medicalNotes || '',
      dietaryRestrictions: athlete.profile?.dietaryRestrictions || '',
      uniformSize: athlete.profile?.uniformSize || '',
      warmupRoutine: athlete.profile?.warmupRoutine || ''
    });
  };

  if (isEditing && canEdit) {
    return (
      <FormModal
        isOpen={true}
        onClose={handleCancelEdit}
        title={`Edit Profile - ${athlete.firstName} ${athlete.lastName}`}
        onSubmit={handleSubmit}
        submitText="Update Profile"
        loading={loading}
        error={error}
        icon={<FontAwesomeIcon icon={faEdit} />}
      >
        <div className="space-y-4">
          <div>
            <label htmlFor="preferredEvents" className="block text-sm font-medium text-gray-700 mb-1">
              Preferred Events
            </label>
            <textarea
              id="preferredEvents"
              value={formData.preferredEvents || ''}
              onChange={(e) => handleChange('preferredEvents', e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              rows={3}
              placeholder="e.g., 800m, 1600m, 5K"
            />
          </div>

          <div>
            <label htmlFor="personalBests" className="block text-sm font-medium text-gray-700 mb-1">
              Personal Bests
            </label>
            <textarea
              id="personalBests"
              value={formData.personalBests || ''}
              onChange={(e) => handleChange('personalBests', e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              rows={3}
              placeholder="e.g., 800m: 2:15, 1600m: 5:00"
            />
          </div>

          <div>
            <label htmlFor="goals" className="block text-sm font-medium text-gray-700 mb-1">
              Goals
            </label>
            <textarea
              id="goals"
              value={formData.goals || ''}
              onChange={(e) => handleChange('goals', e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              rows={3}
              placeholder="Season and long-term goals"
            />
          </div>

          <div>
            <label htmlFor="trainingNotes" className="block text-sm font-medium text-gray-700 mb-1">
              Training Notes
            </label>
            <textarea
              id="trainingNotes"
              value={formData.trainingNotes || ''}
              onChange={(e) => handleChange('trainingNotes', e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              rows={3}
              placeholder="Training preferences, injury history, etc."
            />
          </div>

          <div>
            <label htmlFor="medicalNotes" className="block text-sm font-medium text-gray-700 mb-1">
              Medical Notes
            </label>
            <textarea
              id="medicalNotes"
              value={formData.medicalNotes || ''}
              onChange={(e) => handleChange('medicalNotes', e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              rows={2}
              placeholder="Allergies, medications, medical conditions"
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label htmlFor="dietaryRestrictions" className="block text-sm font-medium text-gray-700 mb-1">
                Dietary Restrictions
              </label>
              <input
                type="text"
                id="dietaryRestrictions"
                value={formData.dietaryRestrictions || ''}
                onChange={(e) => handleChange('dietaryRestrictions', e.target.value)}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                placeholder="e.g., Vegetarian, Gluten-free"
              />
            </div>

            <div>
              <label htmlFor="uniformSize" className="block text-sm font-medium text-gray-700 mb-1">
                Uniform Size
              </label>
              <input
                type="text"
                id="uniformSize"
                value={formData.uniformSize || ''}
                onChange={(e) => handleChange('uniformSize', e.target.value)}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                placeholder="e.g., M, L, XL"
              />
            </div>
          </div>

          <div>
            <label htmlFor="warmupRoutine" className="block text-sm font-medium text-gray-700 mb-1">
              Warmup Routine
            </label>
            <textarea
              id="warmupRoutine"
              value={formData.warmupRoutine || ''}
              onChange={(e) => handleChange('warmupRoutine', e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              rows={3}
              placeholder="Pre-race/practice warmup preferences"
            />
          </div>
        </div>
      </FormModal>
    );
  }

  // View mode
  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
      <div className="bg-white rounded-lg shadow-xl max-w-2xl w-full max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between p-6 border-b border-gray-200">
          <div className="flex items-center">
            <FontAwesomeIcon icon={faIdCard} className="w-6 h-6 text-blue-600 mr-3" />
            <h2 className="text-xl font-semibold text-gray-900">
              {athlete.firstName} {athlete.lastName} - Profile
            </h2>
          </div>
          <div className="flex items-center space-x-2">
            {canEdit && (
              <button
                onClick={handleEdit}
                className="px-3 py-2 text-sm font-medium text-blue-600 hover:text-blue-700 hover:bg-blue-50 rounded-lg transition-colors"
              >
                <FontAwesomeIcon icon={faEdit} className="w-4 h-4 mr-2" />
                Edit
              </button>
            )}
            <button
              onClick={onClose}
              className="text-gray-400 hover:text-gray-600 transition-colors"
            >
              <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>
        
        <div className="p-6 space-y-6">
          {/* Basic Info */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <h3 className="text-sm font-medium text-gray-700 mb-1">Role</h3>
              <p className="text-sm text-gray-900">{athlete.firstName} {athlete.lastName}</p>
            </div>
            <div>
              <h3 className="text-sm font-medium text-gray-700 mb-1">Grade</h3>
              <p className="text-sm text-gray-900">{getGradeLevelDisplayName(athlete.gradeLevel)}</p>
            </div>
          </div>

          {/* Profile Information */}
          {athlete.profile?.preferredEvents && (
            <div>
              <h3 className="text-sm font-medium text-gray-700 mb-2">Preferred Events</h3>
              <p className="text-sm text-gray-900 whitespace-pre-wrap">{athlete.profile.preferredEvents}</p>
            </div>
          )}

          {athlete.profile?.personalBests && (
            <div>
              <h3 className="text-sm font-medium text-gray-700 mb-2">Personal Bests</h3>
              <p className="text-sm text-gray-900 whitespace-pre-wrap">{athlete.profile.personalBests}</p>
            </div>
          )}

          {athlete.profile?.goals && (
            <div>
              <h3 className="text-sm font-medium text-gray-700 mb-2">Goals</h3>
              <p className="text-sm text-gray-900 whitespace-pre-wrap">{athlete.profile.goals}</p>
            </div>
          )}

          {athlete.profile?.trainingNotes && (
            <div>
              <h3 className="text-sm font-medium text-gray-700 mb-2">Training Notes</h3>
              <p className="text-sm text-gray-900 whitespace-pre-wrap">{athlete.profile.trainingNotes}</p>
            </div>
          )}

          {athlete.profile?.medicalNotes && (
            <div>
              <h3 className="text-sm font-medium text-gray-700 mb-2">Medical Notes</h3>
              <p className="text-sm text-gray-900 whitespace-pre-wrap">{athlete.profile.medicalNotes}</p>
            </div>
          )}

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {athlete.profile?.dietaryRestrictions && (
              <div>
                <h3 className="text-sm font-medium text-gray-700 mb-1">Dietary Restrictions</h3>
                <p className="text-sm text-gray-900">{athlete.profile.dietaryRestrictions}</p>
              </div>
            )}

            {athlete.profile?.uniformSize && (
              <div>
                <h3 className="text-sm font-medium text-gray-700 mb-1">Uniform Size</h3>
                <p className="text-sm text-gray-900">{athlete.profile.uniformSize}</p>
              </div>
            )}
          </div>

          {athlete.profile?.warmupRoutine && (
            <div>
              <h3 className="text-sm font-medium text-gray-700 mb-2">Warmup Routine</h3>
              <p className="text-sm text-gray-900 whitespace-pre-wrap">{athlete.profile.warmupRoutine}</p>
            </div>
          )}

          {/* Status Information */}
          <div className="bg-gray-50 rounded-lg p-4">
            <h3 className="text-sm font-medium text-gray-700 mb-3">Status</h3>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div className="flex items-center">
                <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                  athlete.hasPhysicalOnFile
                    ? 'bg-green-100 text-green-800'
                    : 'bg-red-100 text-red-800'
                }`}>
                  {athlete.hasPhysicalOnFile ? 'Physical Complete' : 'Physical Needed'}
                </span>
              </div>
              <div className="flex items-center">
                <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                  athlete.hasWaiverSigned
                    ? 'bg-green-100 text-green-800'
                    : 'bg-red-100 text-red-800'
                }`}>
                  {athlete.hasWaiverSigned ? 'Waiver Signed' : 'Waiver Needed'}
                </span>
              </div>
            </div>
          </div>

          {/* Emergency Contact */}
          {(athlete.emergencyContactName || athlete.emergencyContactPhone) && (
            <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
              <h3 className="text-sm font-medium text-gray-700 mb-2">Emergency Contact</h3>
              {athlete.emergencyContactName && (
                <p className="text-sm text-gray-900">
                  <span className="font-medium">Name:</span> {athlete.emergencyContactName}
                </p>
              )}
              {athlete.emergencyContactPhone && (
                <p className="text-sm text-gray-900">
                  <span className="font-medium">Phone:</span> {athlete.emergencyContactPhone}
                </p>
              )}
            </div>
          )}

          {/* Empty state */}
          {!athlete.profile && (
            <div className="text-center py-8">
              <FontAwesomeIcon icon={faIdCard} className="w-12 h-12 text-gray-400 mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">No Profile Information</h3>
              <p className="text-gray-500 mb-4">
                {canEdit ? 'Click "Edit" to add profile information for this athlete.' : 'No profile information has been added yet.'}
              </p>
              {canEdit && (
                <button
                  onClick={handleEdit}
                  className="bg-blue-600 text-white px-4 py-2 rounded-lg font-medium hover:bg-blue-700 transition-colors"
                >
                  <FontAwesomeIcon icon={faEdit} className="w-4 h-4 mr-2" />
                  Add Profile Information
                </button>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
} 