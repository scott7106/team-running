'use client';

import { useState, useEffect } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faUser } from '@fortawesome/free-solid-svg-icons';
import FormModal from '@/components/ui/form-modal';
import { CreateAthleteDto, AthleteRole, Gender, GradeLevel } from '@/types/athlete';
import { athletesApi, teamsApi, ApiError } from '@/utils/api';

interface CreateAthleteModalProps {
  onClose: () => void;
  onAthleteCreated: () => void;
}

export default function CreateAthleteModal({ onClose, onAthleteCreated }: CreateAthleteModalProps) {
  const [formData, setFormData] = useState<CreateAthleteDto>({
    firstName: '',
    lastName: '',
    email: '',
    role: AthleteRole.Athlete,
    gender: Gender.NS,
    gradeLevel: GradeLevel.Ninth,
    emergencyContactName: '',
    emergencyContactPhone: '',
    dateOfBirth: ''
  });
  
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [gradeLevels, setGradeLevels] = useState<Array<{value: number, name: string, displayName: string}>>([]);
  const [gradeLevelsLoading, setGradeLevelsLoading] = useState(true);

  // Load grade levels on component mount
  useEffect(() => {
    const loadGradeLevels = async () => {
      try {
        setGradeLevelsLoading(true);
        const levels = await teamsApi.getGradeLevels();
        setGradeLevels(levels);
      } catch (error) {
        console.error('Failed to load grade levels:', error);
        // Fall back to empty array, form will still work with hardcoded options
        setGradeLevels([]);
      } finally {
        setGradeLevelsLoading(false);
      }
    };

    loadGradeLevels();
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    try {
      setLoading(true);
      setError(null);

      // Clean up form data - remove empty strings
      const cleanedData: CreateAthleteDto = {
        firstName: formData.firstName.trim(),
        lastName: formData.lastName.trim(),
        email: formData.email?.trim() || undefined,
        role: formData.role,
        gender: formData.gender,
        gradeLevel: formData.gradeLevel,
        emergencyContactName: formData.emergencyContactName?.trim() || undefined,
        emergencyContactPhone: formData.emergencyContactPhone?.trim() || undefined,
        dateOfBirth: formData.dateOfBirth
      };

      await athletesApi.createAthlete(cleanedData);
      
      onAthleteCreated();
      onClose();
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('Failed to create athlete. Please try again.');
      }
      console.error('Error creating athlete:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (field: keyof CreateAthleteDto, value: string | AthleteRole | Gender | GradeLevel) => {
    setFormData(prev => ({
      ...prev,
      [field]: value
    }));
  };

  return (
    <FormModal
      isOpen={true}
      onClose={onClose}
      title="Add New Athlete"
      onSubmit={handleSubmit}
      submitText="Add Athlete"
      loading={loading}
      error={error}
      icon={<FontAwesomeIcon icon={faUser} />}
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
              value={formData.firstName}
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
              value={formData.lastName}
              onChange={(e) => handleChange('lastName', e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              required
              maxLength={100}
            />
          </div>
        </div>

        <div>
          <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
            Email Address
          </label>
          <input
            type="email"
            id="email"
            value={formData.email || ''}
            onChange={(e) => handleChange('email', e.target.value)}
            className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            placeholder="Optional - for creating user account"
          />
          <p className="text-xs text-gray-500 mt-1">
            If provided, a user account will be created for this athlete
          </p>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label htmlFor="role" className="block text-sm font-medium text-gray-700 mb-1">
              Role
            </label>
            <select
              id="role"
              value={formData.role}
              onChange={(e) => handleChange('role', parseInt(e.target.value) as AthleteRole)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            >
              <option value={AthleteRole.Athlete}>Athlete</option>
              <option value={AthleteRole.Captain}>Captain</option>
            </select>
          </div>
          
          <div>
            <label htmlFor="gender" className="block text-sm font-medium text-gray-700 mb-1">
              Gender
            </label>
            <select
              id="gender"
              value={formData.gender}
              onChange={(e) => handleChange('gender', parseInt(e.target.value) as Gender)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            >
              <option value={Gender.Female}>Female</option>
              <option value={Gender.Male}>Male</option>
              <option value={Gender.NS}>Not Specified</option>
            </select>
          </div>
        </div>

        <div>
          <label htmlFor="gradeLevel" className="block text-sm font-medium text-gray-700 mb-1">
            Grade Level
          </label>
          <select
            id="gradeLevel"
            value={formData.gradeLevel}
            onChange={(e) => handleChange('gradeLevel', parseInt(e.target.value) as GradeLevel)}
            className="w-full border border-gray-300 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            disabled={gradeLevelsLoading}
          >
            {gradeLevelsLoading ? (
              <option>Loading grade levels...</option>
            ) : gradeLevels.length > 0 ? (
              gradeLevels.map((level) => (
                <option key={level.value} value={level.value}>
                  {level.displayName}
                </option>
              ))
            ) : (
              // Fallback to hardcoded options if API fails
              <>
                <option value={GradeLevel.K}>Kindergarten</option>
                <option value={GradeLevel.First}>1st Grade</option>
                <option value={GradeLevel.Second}>2nd Grade</option>
                <option value={GradeLevel.Third}>3rd Grade</option>
                <option value={GradeLevel.Fourth}>4th Grade</option>
                <option value={GradeLevel.Fifth}>5th Grade</option>
                <option value={GradeLevel.Sixth}>6th Grade</option>
                <option value={GradeLevel.Seventh}>7th Grade</option>
                <option value={GradeLevel.Eighth}>8th Grade</option>
                <option value={GradeLevel.Ninth}>9th Grade</option>
                <option value={GradeLevel.Tenth}>10th Grade</option>
                <option value={GradeLevel.Eleventh}>11th Grade</option>
                <option value={GradeLevel.Twelfth}>12th Grade</option>
                <option value={GradeLevel.Other}>Other</option>
                <option value={GradeLevel.Freshman}>Freshman</option>
                <option value={GradeLevel.Sophomore}>Sophomore</option>
                <option value={GradeLevel.Junior}>Junior</option>
                <option value={GradeLevel.Senior}>Senior</option>
              </>
            )}
          </select>
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
      </div>
    </FormModal>
  );
} 