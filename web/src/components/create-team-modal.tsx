'use client';

import { useState, useEffect } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faBuilding,
  faSpinner,
  faExclamationTriangle,
  faCheck,
  faEye,
  faEyeSlash
} from '@fortawesome/free-solid-svg-icons';
import { CreateTeamWithNewOwnerDto, CreateTeamWithExistingOwnerDto, TeamTier, GlobalAdminTeamDto } from '@/types/team';
import { teamsApi, usersApi } from '@/utils/api';
import { UsersApiResponse } from '@/types/user';
import FormModal from './FormModal';

interface CreateTeamModalProps {
  isOpen: boolean;
  onClose: () => void;
  onTeamCreated: (team: GlobalAdminTeamDto) => void;
}

type OwnerType = 'new' | 'existing';

interface FormData {
  name: string;
  subdomain: string;
  tier: TeamTier;
  primaryColor: string;
  secondaryColor: string;
  expiresOn: string;
  ownerType: OwnerType;
  // New owner fields
  ownerEmail: string;
  ownerFirstName: string;
  ownerLastName: string;
  ownerPassword: string;
  // Existing owner fields
  ownerId: string;
}

export default function CreateTeamModal({ isOpen, onClose, onTeamCreated }: CreateTeamModalProps) {
  const [formData, setFormData] = useState<FormData>({
    name: '',
    subdomain: '',
    tier: TeamTier.Free,
    primaryColor: '#3B82F6',
    secondaryColor: '#FFFFFF',
    expiresOn: '',
    ownerType: 'new',
    ownerEmail: '',
    ownerFirstName: '',
    ownerLastName: '',
    ownerPassword: '',
    ownerId: '',
  });

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [subdomainChecking, setSubdomainChecking] = useState(false);
  const [subdomainAvailable, setSubdomainAvailable] = useState<boolean | null>(null);
  const [showPassword, setShowPassword] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  
  // For existing owner search
  const [ownerSearchQuery, setOwnerSearchQuery] = useState('');
  const [ownerSearchResults, setOwnerSearchResults] = useState<UsersApiResponse | null>(null);
  const [ownerSearchLoading, setOwnerSearchLoading] = useState(false);
  const [selectedOwnerEmail, setSelectedOwnerEmail] = useState('');
  const [showCustomPrimaryColor, setShowCustomPrimaryColor] = useState(false);
  const [showCustomSecondaryColor, setShowCustomSecondaryColor] = useState(false);

  // Predefined color options
  const colorOptions = [
    { name: 'Blue', value: '#3B82F6' },
    { name: 'Purple', value: '#8B5CF6' },
    { name: 'Green', value: '#10B981' },
    { name: 'Red', value: '#EF4444' },
    { name: 'Orange', value: '#F59E0B' },
    { name: 'Pink', value: '#EC4899' },
    { name: 'Indigo', value: '#6366F1' },
    { name: 'Cyan', value: '#06B6D4' },
    { name: 'Emerald', value: '#059669' },
    { name: 'Rose', value: '#F43F5E' },
    { name: 'Slate', value: '#64748B' },
    { name: 'Black', value: '#000000' },
  ];

  const secondaryColorOptions = [
    { name: 'White', value: '#FFFFFF' },
    { name: 'Light Gray', value: '#F8FAFC' },
    { name: 'Gray', value: '#E2E8F0' },
    { name: 'Light Blue', value: '#EFF6FF' },
    { name: 'Light Purple', value: '#F3E8FF' },
    { name: 'Light Green', value: '#ECFDF5' },
    { name: 'Light Red', value: '#FEF2F2' },
    { name: 'Light Orange', value: '#FFF7ED' },
    { name: 'Light Pink', value: '#FDF2F8' },
    { name: 'Light Indigo', value: '#EEF2FF' },
    { name: 'Light Cyan', value: '#ECFEFF' },
    { name: 'Light Rose', value: '#FFF1F2' },
  ];

  // Reset form when modal closes
  useEffect(() => {
    if (!isOpen) {
      setFormData({
        name: '',
        subdomain: '',
        tier: TeamTier.Free,
        primaryColor: '#3B82F6',
        secondaryColor: '#FFFFFF',
        expiresOn: '',
        ownerType: 'new',
        ownerEmail: '',
        ownerFirstName: '',
        ownerLastName: '',
        ownerPassword: '',
        ownerId: '',
      });
      setError(null);
      setSubdomainAvailable(null);
      setOwnerSearchQuery('');
      setOwnerSearchResults(null);
      setSelectedOwnerEmail('');
      setShowCustomPrimaryColor(false);
      setShowCustomSecondaryColor(false);
      setFieldErrors({});
    }
  }, [isOpen]);

  // Check subdomain availability with debounce
  useEffect(() => {
    if (!formData.subdomain) {
      setSubdomainAvailable(null);
      return;
    }

    const timer = setTimeout(async () => {
      try {
        setSubdomainChecking(true);
        const available = await teamsApi.checkSubdomainAvailability(formData.subdomain);
        setSubdomainAvailable(available);
      } catch (err) {
        console.error('Error checking subdomain:', err);
        setSubdomainAvailable(null);
      } finally {
        setSubdomainChecking(false);
      }
    }, 500);

    return () => clearTimeout(timer);
  }, [formData.subdomain]);

  // Search for existing owners with debounce
  useEffect(() => {
    if (formData.ownerType !== 'existing' || !ownerSearchQuery) {
      setOwnerSearchResults(null);
      return;
    }

    const timer = setTimeout(async () => {
      try {
        setOwnerSearchLoading(true);
        const results = await usersApi.getUsers({
          pageNumber: 1,
          pageSize: 10,
          searchQuery: ownerSearchQuery,
          isDeleted: false
        });
        setOwnerSearchResults(results);
      } catch (err) {
        console.error('Error searching users:', err);
        setOwnerSearchResults(null);
      } finally {
        setOwnerSearchLoading(false);
      }
    }, 300);

    return () => clearTimeout(timer);
  }, [ownerSearchQuery, formData.ownerType]);

  const validateField = (field: keyof FormData, value: string | TeamTier): string | null => {
    switch (field) {
      case 'ownerPassword':
        if (typeof value === 'string') {
          if (value.length < 8) {
            return 'Password must be at least 8 characters long';
          }
          if (!/(?=.*[a-z])/.test(value)) {
            return 'Password must contain at least one lowercase letter';
          }
          if (!/(?=.*[A-Z])/.test(value)) {
            return 'Password must contain at least one uppercase letter';
          }
          if (!/(?=.*\d)/.test(value)) {
            return 'Password must contain at least one number';
          }
        }
        return null;
      case 'ownerEmail':
        if (typeof value === 'string') {
          const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
          if (!emailRegex.test(value)) {
            return 'Please enter a valid email address';
          }
        }
        return null;
      case 'name':
        if (typeof value === 'string' && value.trim().length < 2) {
          return 'Team name must be at least 2 characters long';
        }
        return null;
      case 'subdomain':
        if (typeof value === 'string') {
          if (value.length < 3) {
            return 'Subdomain must be at least 3 characters long';
          }
          if (!/^[a-z0-9-]+$/.test(value)) {
            return 'Subdomain can only contain lowercase letters, numbers, and hyphens';
          }
        }
        return null;
      case 'ownerFirstName':
      case 'ownerLastName':
        if (typeof value === 'string' && value.trim().length < 1) {
          return 'This field is required';
        }
        return null;
      default:
        return null;
    }
  };

  const handleInputChange = (field: keyof FormData, value: string | TeamTier) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    setError(null);
    
    // Clear field error when user starts typing
    if (fieldErrors[field]) {
      setFieldErrors(prev => {
        const newErrors = { ...prev };
        delete newErrors[field];
        return newErrors;
      });
    }
    
    // Validate field on blur/change for immediate feedback
    if (typeof value === 'string' && value.length > 0) {
      const fieldError = validateField(field, value);
      if (fieldError) {
        setFieldErrors(prev => ({ ...prev, [field]: fieldError }));
      }
    }
  };

  const formatSubdomain = (value: string) => {
    return value.toLowerCase().replace(/[^a-z0-9-]/g, '');
  };

  const handleSubdomainChange = (value: string) => {
    const formatted = formatSubdomain(value);
    handleInputChange('subdomain', formatted);
  };

  const handleOwnerSelect = (userId: string, email: string, firstName: string, lastName: string) => {
    setFormData(prev => ({ ...prev, ownerId: userId }));
    setSelectedOwnerEmail(email);
    setOwnerSearchQuery(`${firstName} ${lastName} (${email})`);
    setOwnerSearchResults(null);
  };

  const isFormValid = () => {
    if (!formData.name || !formData.subdomain) return false;
    if (subdomainAvailable !== true) return false;
    
    if (formData.ownerType === 'new') {
      return formData.ownerEmail && formData.ownerFirstName && formData.ownerLastName && formData.ownerPassword;
    } else {
      return formData.ownerId;
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    // Validate all fields before submission
    const newFieldErrors: Record<string, string> = {};
    
    // Validate team information
    const nameError = validateField('name', formData.name);
    if (nameError) newFieldErrors.name = nameError;
    
    const subdomainError = validateField('subdomain', formData.subdomain);
    if (subdomainError) newFieldErrors.subdomain = subdomainError;
    
    // Validate owner information based on type
    if (formData.ownerType === 'new') {
      const emailError = validateField('ownerEmail', formData.ownerEmail);
      if (emailError) newFieldErrors.ownerEmail = emailError;
      
      const passwordError = validateField('ownerPassword', formData.ownerPassword);
      if (passwordError) newFieldErrors.ownerPassword = passwordError;
      
      const firstNameError = validateField('ownerFirstName', formData.ownerFirstName);
      if (firstNameError) newFieldErrors.ownerFirstName = firstNameError;
      
      const lastNameError = validateField('ownerLastName', formData.ownerLastName);
      if (lastNameError) newFieldErrors.ownerLastName = lastNameError;
    }
    
    // Set field errors if any exist
    if (Object.keys(newFieldErrors).length > 0) {
      setFieldErrors(newFieldErrors);
      setError('Please fix the errors below before submitting');
      return;
    }
    
    if (!isFormValid()) {
      setError('Please fill in all required fields');
      return;
    }

    try {
      setLoading(true);
      setError(null);
      setFieldErrors({});

      let newTeam: GlobalAdminTeamDto;

      if (formData.ownerType === 'new') {
        const dto: CreateTeamWithNewOwnerDto = {
          name: formData.name,
          subdomain: formData.subdomain,
          ownerEmail: formData.ownerEmail,
          ownerFirstName: formData.ownerFirstName,
          ownerLastName: formData.ownerLastName,
          ownerPassword: formData.ownerPassword,
          tier: formData.tier,
          primaryColor: formData.primaryColor,
          secondaryColor: formData.secondaryColor,
          expiresOn: formData.expiresOn || undefined,
        };

        newTeam = await teamsApi.createTeamWithNewOwner(dto);
      } else {
        const dto: CreateTeamWithExistingOwnerDto = {
          name: formData.name,
          subdomain: formData.subdomain,
          ownerId: formData.ownerId,
          tier: formData.tier,
          primaryColor: formData.primaryColor,
          secondaryColor: formData.secondaryColor,
          expiresOn: formData.expiresOn || undefined,
        };

        newTeam = await teamsApi.createTeamWithExistingOwner(dto);
      }

      onTeamCreated(newTeam);
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create team. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <FormModal
      isOpen={isOpen}
      onClose={onClose}
      title="Create New Team"
      icon={<FontAwesomeIcon icon={faBuilding} className="w-5 h-5 text-blue-600" />}
      size="xl"
      onSubmit={handleSubmit}
      submitText="Create Team"
      loading={loading}
      error={error}
      isSubmitDisabled={!isFormValid()}
    >
      {/* Team Information */}
      <div className="mb-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Team Information</h3>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Team Name *
            </label>
            <input
              type="text"
              value={formData.name}
              onChange={(e) => handleInputChange('name', e.target.value)}
              className={`w-full px-3 py-2.5 border rounded-xl focus:outline-none focus:ring-2 focus:border-transparent transition-all duration-200 ${
                fieldErrors.name
                  ? 'border-red-500 focus:ring-red-500 bg-red-50'
                  : 'border-gray-300 focus:ring-blue-500'
              }`}
              placeholder="Enter team name"
              disabled={loading}
              required
            />
            {fieldErrors.name && (
              <p className="mt-1 text-sm text-red-600 flex items-center">
                <FontAwesomeIcon icon={faExclamationTriangle} className="w-4 h-4 mr-1" />
                {fieldErrors.name}
              </p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Subdomain *
            </label>
            <div className="relative">
              <input
                type="text"
                value={formData.subdomain}
                onChange={(e) => handleSubdomainChange(e.target.value)}
                className={`w-full px-3 py-2.5 border rounded-xl focus:outline-none focus:ring-2 focus:border-transparent transition-all duration-200 pr-10 ${
                  fieldErrors.subdomain || subdomainAvailable === false
                    ? 'border-red-500 focus:ring-red-500 bg-red-50'
                    : 'border-gray-300 focus:ring-blue-500'
                }`}
                placeholder="teamname"
                disabled={loading}
                required
              />
              <div className="absolute inset-y-0 right-0 pr-3 flex items-center">
                {subdomainChecking ? (
                  <FontAwesomeIcon icon={faSpinner} className="w-4 h-4 text-gray-400 animate-spin" />
                ) : subdomainAvailable === true ? (
                  <FontAwesomeIcon icon={faCheck} className="w-4 h-4 text-green-500" />
                ) : subdomainAvailable === false ? (
                  <FontAwesomeIcon icon={faExclamationTriangle} className="w-4 h-4 text-red-500" />
                ) : null}
              </div>
            </div>
            <p className="text-sm text-gray-500 mt-1">
              Will be available at: {formData.subdomain || 'subdomain'}.teamstride.com
            </p>
            {fieldErrors.subdomain && (
              <p className="mt-1 text-sm text-red-600 flex items-center">
                <FontAwesomeIcon icon={faExclamationTriangle} className="w-4 h-4 mr-1" />
                {fieldErrors.subdomain}
              </p>
            )}
            {subdomainAvailable === false && (
              <p className="text-sm text-red-600 mt-1 flex items-center">
                <FontAwesomeIcon icon={faExclamationTriangle} className="w-4 h-4 mr-1" />
                This subdomain is already taken
              </p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Tier
            </label>
            <select
              value={formData.tier}
              onChange={(e) => handleInputChange('tier', parseInt(e.target.value) as TeamTier)}
              className="w-full px-3 py-2.5 border border-gray-300 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all duration-200"
              disabled={loading}
            >
              <option value={TeamTier.Free}>Free</option>
              <option value={TeamTier.Standard}>Standard</option>
              <option value={TeamTier.Premium}>Premium</option>
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Expires On (optional)
            </label>
            <input
              type="date"
              value={formData.expiresOn}
              onChange={(e) => handleInputChange('expiresOn', e.target.value)}
              className="w-full px-3 py-2.5 border border-gray-300 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all duration-200"
              disabled={loading}
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Primary Color
            </label>
            <div className="space-y-3">
              <div className="grid grid-cols-6 gap-2">
                {colorOptions.map((color) => (
                  <button
                    key={color.value}
                    type="button"
                    onClick={() => handleInputChange('primaryColor', color.value)}
                    className={`w-10 h-10 rounded-lg border-2 transition-all duration-200 hover:scale-105 ${
                      formData.primaryColor === color.value
                        ? 'border-gray-800 ring-2 ring-offset-2 ring-gray-400'
                        : 'border-gray-300 hover:border-gray-400'
                    }`}
                    style={{ backgroundColor: color.value }}
                    title={color.name}
                    disabled={loading}
                  />
                ))}
              </div>
              <div className="flex items-center space-x-2">
                <button
                  type="button"
                  onClick={() => setShowCustomPrimaryColor(!showCustomPrimaryColor)}
                  className="text-sm text-blue-600 hover:text-blue-700 font-medium transition-colors duration-200"
                  disabled={loading}
                >
                  {showCustomPrimaryColor ? 'Hide' : 'Custom Color'}
                </button>
                {showCustomPrimaryColor && (
                  <input
                    type="color"
                    value={formData.primaryColor}
                    onChange={(e) => handleInputChange('primaryColor', e.target.value)}
                    className="w-10 h-10 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all duration-200"
                    disabled={loading}
                  />
                )}
              </div>
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Secondary Color
            </label>
            <div className="space-y-3">
              <div className="grid grid-cols-6 gap-2">
                {secondaryColorOptions.map((color) => (
                  <button
                    key={color.value}
                    type="button"
                    onClick={() => handleInputChange('secondaryColor', color.value)}
                    className={`w-10 h-10 rounded-lg border-2 transition-all duration-200 hover:scale-105 ${
                      formData.secondaryColor === color.value
                        ? 'border-gray-800 ring-2 ring-offset-2 ring-gray-400'
                        : 'border-gray-300 hover:border-gray-400'
                    }`}
                    style={{ backgroundColor: color.value }}
                    title={color.name}
                    disabled={loading}
                  />
                ))}
              </div>
              <div className="flex items-center space-x-2">
                <button
                  type="button"
                  onClick={() => setShowCustomSecondaryColor(!showCustomSecondaryColor)}
                  className="text-sm text-blue-600 hover:text-blue-700 font-medium transition-colors duration-200"
                  disabled={loading}
                >
                  {showCustomSecondaryColor ? 'Hide' : 'Custom Color'}
                </button>
                {showCustomSecondaryColor && (
                  <input
                    type="color"
                    value={formData.secondaryColor}
                    onChange={(e) => handleInputChange('secondaryColor', e.target.value)}
                    className="w-10 h-10 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all duration-200"
                    disabled={loading}
                  />
                )}
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Owner Selection */}
      <div className="mb-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Team Owner</h3>
        
        <div className="grid grid-cols-2 gap-4 mb-4">
          <label className="cursor-pointer">
            <input
              type="radio"
              name="ownerType"
              value="new"
              checked={formData.ownerType === 'new'}
              onChange={(e) => handleInputChange('ownerType', e.target.value as OwnerType)}
              className="sr-only"
              disabled={loading}
            />
            <div className={`p-4 rounded-xl border-2 transition-all duration-200 hover:shadow-md ${
              formData.ownerType === 'new'
                ? 'border-blue-500 bg-blue-50 shadow-sm'
                : 'border-gray-200 bg-white hover:border-gray-300'
            }`}>
              <div className="flex items-center space-x-3">
                <div className={`w-4 h-4 rounded-full border-2 flex items-center justify-center transition-all duration-200 ${
                  formData.ownerType === 'new'
                    ? 'border-blue-500 bg-blue-500'
                    : 'border-gray-300'
                }`}>
                  {formData.ownerType === 'new' && (
                    <div className="w-2 h-2 rounded-full bg-white"></div>
                  )}
                </div>
                <div>
                  <div className="font-medium text-gray-900">Create New User</div>
                  <div className="text-sm text-gray-500">Set up a new team owner account</div>
                </div>
              </div>
            </div>
          </label>
          
          <label className="cursor-pointer">
            <input
              type="radio"
              name="ownerType"
              value="existing"
              checked={formData.ownerType === 'existing'}
              onChange={(e) => handleInputChange('ownerType', e.target.value as OwnerType)}
              className="sr-only"
              disabled={loading}
            />
            <div className={`p-4 rounded-xl border-2 transition-all duration-200 hover:shadow-md ${
              formData.ownerType === 'existing'
                ? 'border-blue-500 bg-blue-50 shadow-sm'
                : 'border-gray-200 bg-white hover:border-gray-300'
            }`}>
              <div className="flex items-center space-x-3">
                <div className={`w-4 h-4 rounded-full border-2 flex items-center justify-center transition-all duration-200 ${
                  formData.ownerType === 'existing'
                    ? 'border-blue-500 bg-blue-500'
                    : 'border-gray-300'
                }`}>
                  {formData.ownerType === 'existing' && (
                    <div className="w-2 h-2 rounded-full bg-white"></div>
                  )}
                </div>
                <div>
                  <div className="font-medium text-gray-900">Use Existing User</div>
                  <div className="text-sm text-gray-500">Select from current users</div>
                </div>
              </div>
            </div>
          </label>
        </div>

        {formData.ownerType === 'new' ? (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Email Address *
              </label>
              <input
                type="email"
                value={formData.ownerEmail}
                onChange={(e) => handleInputChange('ownerEmail', e.target.value)}
                className={`w-full px-3 py-2.5 border rounded-xl focus:outline-none focus:ring-2 focus:border-transparent transition-all duration-200 ${
                  fieldErrors.ownerEmail
                    ? 'border-red-500 focus:ring-red-500 bg-red-50'
                    : 'border-gray-300 focus:ring-blue-500'
                }`}
                placeholder="owner@example.com"
                disabled={loading}
                required
              />
              {fieldErrors.ownerEmail && (
                <p className="mt-1 text-sm text-red-600 flex items-center">
                  <FontAwesomeIcon icon={faExclamationTriangle} className="w-4 h-4 mr-1" />
                  {fieldErrors.ownerEmail}
                </p>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Password *
              </label>
              <div className="relative">
                <input
                  type={showPassword ? 'text' : 'password'}
                  value={formData.ownerPassword}
                  onChange={(e) => handleInputChange('ownerPassword', e.target.value)}
                  className={`w-full px-3 py-2.5 border rounded-xl focus:outline-none focus:ring-2 focus:border-transparent transition-all duration-200 pr-10 ${
                    fieldErrors.ownerPassword
                      ? 'border-red-500 focus:ring-red-500 bg-red-50'
                      : 'border-gray-300 focus:ring-blue-500'
                  }`}
                  placeholder="Minimum 8 characters"
                  disabled={loading}
                  required
                  minLength={8}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute inset-y-0 right-0 pr-3 flex items-center hover:bg-gray-50 rounded-r-xl transition-colors duration-200"
                >
                  <FontAwesomeIcon
                    icon={showPassword ? faEyeSlash : faEye}
                    className="w-4 h-4 text-gray-400"
                  />
                </button>
              </div>
              {fieldErrors.ownerPassword && (
                <p className="mt-1 text-sm text-red-600 flex items-center">
                  <FontAwesomeIcon icon={faExclamationTriangle} className="w-4 h-4 mr-1" />
                  {fieldErrors.ownerPassword}
                </p>
              )}
              <div className="mt-1 text-xs text-gray-500">
                Password must contain: 8+ characters, uppercase, lowercase, and number
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                First Name *
              </label>
              <input
                type="text"
                value={formData.ownerFirstName}
                onChange={(e) => handleInputChange('ownerFirstName', e.target.value)}
                className={`w-full px-3 py-2.5 border rounded-xl focus:outline-none focus:ring-2 focus:border-transparent transition-all duration-200 ${
                  fieldErrors.ownerFirstName
                    ? 'border-red-500 focus:ring-red-500 bg-red-50'
                    : 'border-gray-300 focus:ring-blue-500'
                }`}
                placeholder="First name"
                disabled={loading}
                required
              />
              {fieldErrors.ownerFirstName && (
                <p className="mt-1 text-sm text-red-600 flex items-center">
                  <FontAwesomeIcon icon={faExclamationTriangle} className="w-4 h-4 mr-1" />
                  {fieldErrors.ownerFirstName}
                </p>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Last Name *
              </label>
              <input
                type="text"
                value={formData.ownerLastName}
                onChange={(e) => handleInputChange('ownerLastName', e.target.value)}
                className={`w-full px-3 py-2.5 border rounded-xl focus:outline-none focus:ring-2 focus:border-transparent transition-all duration-200 ${
                  fieldErrors.ownerLastName
                    ? 'border-red-500 focus:ring-red-500 bg-red-50'
                    : 'border-gray-300 focus:ring-blue-500'
                }`}
                placeholder="Last name"
                disabled={loading}
                required
              />
              {fieldErrors.ownerLastName && (
                <p className="mt-1 text-sm text-red-600 flex items-center">
                  <FontAwesomeIcon icon={faExclamationTriangle} className="w-4 h-4 mr-1" />
                  {fieldErrors.ownerLastName}
                </p>
              )}
            </div>
          </div>
        ) : (
          <div className="relative">
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Search for User *
            </label>
            <input
              type="text"
              value={ownerSearchQuery}
              onChange={(e) => setOwnerSearchQuery(e.target.value)}
              className="w-full px-3 py-2.5 border border-gray-300 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all duration-200"
              placeholder="Search by name or email..."
              disabled={loading}
            />
            
            {ownerSearchLoading && (
              <div className="absolute right-3 top-9 pointer-events-none">
                <FontAwesomeIcon icon={faSpinner} className="w-4 h-4 text-gray-400 animate-spin" />
              </div>
            )}

            {ownerSearchResults && ownerSearchResults.items.length > 0 && (
              <div className="absolute z-10 w-full mt-1 bg-white border border-gray-200 rounded-xl shadow-lg max-h-60 overflow-y-auto">
                {ownerSearchResults.items.map((user) => (
                  <button
                    key={user.id}
                    type="button"
                    onClick={() => handleOwnerSelect(user.id, user.email, user.firstName, user.lastName)}
                    className="w-full text-left px-4 py-3 hover:bg-gray-50 border-b border-gray-100 last:border-b-0 transition-colors duration-200 first:rounded-t-xl last:rounded-b-xl"
                  >
                    <div className="font-medium text-gray-900">
                      {user.firstName} {user.lastName}
                    </div>
                    <div className="text-sm text-gray-500">{user.email}</div>
                  </button>
                ))}
              </div>
            )}

            {selectedOwnerEmail && (
              <p className="text-sm text-green-600 mt-2 font-medium">
                Selected: {selectedOwnerEmail}
              </p>
            )}
          </div>
        )}
      </div>
    </FormModal>
  );
} 