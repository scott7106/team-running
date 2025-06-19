'use client';

import { useState } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faUserEdit } from '@fortawesome/free-solid-svg-icons';
import FormModal from '@/components/ui/form-modal';
import { AthleteDto, UpdateAthleteDto, AthleteRole } from '@/types/athlete';
import { athletesApi, ApiError } from '@/utils/api';

interface EditAthleteModalProps {
  athlete: AthleteDto;
  onClose: () => void;
  onAthleteUpdated: () => void;
}

export default function EditAthleteModal({ athlete, onClose, onAthleteUpdated }: EditAthleteModalProps) {
  const [formData, setFormData] = useState<UpdateAthleteDto>({
    firstName: athlete.firstName,
    lastName: athlete.lastName,
    role: athlete.role,
    emergencyContactName: athlete.emergencyContactName || '',
    emergencyContactPhone: athlete.emergencyContactPhone || '',
    dateOfBirth: athlete.dateOfBirth ? athlete.dateOfBirth.split('T')[0] : '',
    grade: athlete.grade || ''
  });
  
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    try {
      setLoading(true);
      setError(null);

      // Clean up form data - remove empty strings and only include changed values
      const cleanedData: UpdateAthleteDto = {};
      
      if (formData.firstName?.trim() !== athlete.firstName) {
        cleanedData.firstName = formData.firstName?.trim();
      }
      if (formData.lastName?.trim() !== athlete.lastName) {
        cleanedData.lastName = formData.lastName?.trim();
      }
      if (formData.role !== athlete.role) {
        cleanedData.role = formData.role;
      }
      if (formData.emergencyContactName?.trim() !== (athlete.emergencyContactName || '')) {
        cleanedData.emergencyContactName = formData.emergencyContactName?.trim() || undefined;
      }
      if (formData.emergencyContactPhone?.trim() !== (athlete.emergencyContactPhone || '')) {
        cleanedData.emergencyContactPhone = formData.emergencyContactPhone?.trim() || undefined;
      }
      if (formData.dateOfBirth !== (athlete.dateOfBirth ? athlete.dateOfBirth.split('T')[0] : '')) {
        cleanedData.dateOfBirth = formData.dateOfBirth || undefined;
      }
      if (formData.grade?.trim() !== (athlete.grade || '')) {
        cleanedData.grade = formData.grade?.trim() || undefined;
      }

      // Only make request if there are changes
      if (Object.keys(cleanedData).length > 0) {
        await athletesApi.updateAthlete(athlete.id, cleanedData);
      }
      
      onAthleteUpdated();
      onClose();
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('Failed to update athlete. Please try again.');
      }
      console.error('Error updating athlete:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (field: keyof UpdateAthleteDto, value: string | AthleteRole) => {
    setFormData(prev => ({
      ...prev,
      [field]: value
    }));
  };

  return (
    <FormModal
      isOpen={true}
      onClose={onClose}
      title="Edit Athlete"
      onSubmit={handleSubmit}
      submitText="Update Athlete"
      loading={loading}
      error={error}
      icon={<FontAwesomeIcon icon={faUserEdit} />}
    >
      <div className="space-y-4">
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label htmlFor="firstName" className="block text-sm font-medium text-gray-700 mb-1">
              First Name *
            </label>
            <input
              type="text"
              id="firstName"
              value={formData.firstName || ''}
              onChange={(e) => handleChange('firstName', e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              required
              maxLength={100}
            />
          </div>
          
          <div>
            <label htmlFor="lastName" className="block text-sm font-medium text-gray-700 mb-1">
              Last Name *
            </label>
            <input
              type="text"
              id="lastName"
              value={formData.lastName || ''}
              onChange={(e) => handleChange('lastName', e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              required
              maxLength={100}
            />
          </div>
        </div>

        {athlete.email && (
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Email Address
            </label>
            <input
              type="email"
              value={athlete.email}
              disabled
              className="w-full border border-gray-300 rounded-lg px-3 py-2 bg-gray-50 text-gray-500 cursor-not-allowed"
            />
            <p className="text-xs text-gray-500 mt-1">
              Email cannot be changed after athlete creation
            </p>
          </div>
        )}

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label htmlFor="role" className="block text-sm font-medium text-gray-700 mb-1">
              Role
            </label>
            <select
              id="role"
              value={formData.role}
              onChange={(e) => handleChange('role', e.target.value as AthleteRole)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            >
              <option value={AthleteRole.Athlete}>Athlete</option>
              <option value={AthleteRole.Captain}>Captain</option>
            </select>
          </div>
          
          <div>
            <label htmlFor="grade" className="block text-sm font-medium text-gray-700 mb-1">
              Grade Level
            </label>
            <input
              type="text"
              id="grade"
              value={formData.grade || ''}
              onChange={(e) => handleChange('grade', e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              placeholder="e.g., 9th, 10th, Senior"
            />
          </div>
        </div>

        <div>
          <label htmlFor="dateOfBirth" className="block text-sm font-medium text-gray-700 mb-1">
            Date of Birth
          </label>
          <input
            type="date"
            id="dateOfBirth"
            value={formData.dateOfBirth || ''}
            onChange={(e) => handleChange('dateOfBirth', e.target.value)}
            className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          />
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label htmlFor="emergencyContactName" className="block text-sm font-medium text-gray-700 mb-1">
              Emergency Contact Name
            </label>
            <input
              type="text"
              id="emergencyContactName"
              value={formData.emergencyContactName || ''}
              onChange={(e) => handleChange('emergencyContactName', e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            />
          </div>
          
          <div>
            <label htmlFor="emergencyContactPhone" className="block text-sm font-medium text-gray-700 mb-1">
              Emergency Contact Phone
            </label>
            <input
              type="tel"
              id="emergencyContactPhone"
              value={formData.emergencyContactPhone || ''}
              onChange={(e) => handleChange('emergencyContactPhone', e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            />
          </div>
        </div>

        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
          <div className="flex items-center space-x-4">
            <div className="flex items-center">
              <input
                type="checkbox"
                id="hasPhysicalOnFile"
                checked={athlete.hasPhysicalOnFile}
                disabled
                className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded disabled:opacity-50"
              />
              <label htmlFor="hasPhysicalOnFile" className="ml-2 text-sm text-gray-700">
                Physical on file
              </label>
            </div>
            
            <div className="flex items-center">
              <input
                type="checkbox"
                id="hasWaiverSigned"
                checked={athlete.hasWaiverSigned}
                disabled
                className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded disabled:opacity-50"
              />
              <label htmlFor="hasWaiverSigned" className="ml-2 text-sm text-gray-700">
                Waiver signed
              </label>
            </div>
          </div>
          <p className="text-xs text-blue-600 mt-2">
            Use the status toggle options in the athlete menu to update physical and waiver status
          </p>
        </div>
      </div>
    </FormModal>
  );
} 