'use client';

import { useState, useEffect, useMemo } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faBuilding,
  faSpinner,
  faExclamationTriangle,
  faCheck
} from '@fortawesome/free-solid-svg-icons';
import { GlobalAdminTeamDto, GlobalAdminUpdateTeamDto, TeamTier, TeamStatus } from '@/types/team';
import { teamsApi } from '@/utils/api';
import FormModal from './FormModal';

interface EditTeamModalProps {
  isOpen: boolean;
  onClose: () => void;
  onTeamUpdated: (team: GlobalAdminTeamDto) => void;
  team: GlobalAdminTeamDto | null;
}

interface FormData {
  name: string;
  subdomain: string;
  status: TeamStatus;
  tier: TeamTier;
  primaryColor: string;
  secondaryColor: string;
  expiresOn: string;
  logoUrl: string;
}

export default function EditTeamModal({ isOpen, onClose, onTeamUpdated, team }: EditTeamModalProps) {
  const [formData, setFormData] = useState<FormData>({
    name: '',
    subdomain: '',
    status: TeamStatus.Active,
    tier: TeamTier.Free,
    primaryColor: '#3B82F6',
    secondaryColor: '#FFFFFF',
    expiresOn: '',
    logoUrl: '',
  });

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [subdomainChecking, setSubdomainChecking] = useState(false);
  const [subdomainAvailable, setSubdomainAvailable] = useState<boolean | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [showCustomPrimaryColor, setShowCustomPrimaryColor] = useState(false);
  const [showCustomSecondaryColor, setShowCustomSecondaryColor] = useState(false);

  // Predefined color options
  const colorOptions = useMemo(() => [
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
  ], []);

  const secondaryColorOptions = useMemo(() => [
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
  ], []);

  // Initialize form when team changes
  useEffect(() => {
    if (isOpen && team) {
      setFormData({
        name: team.name,
        subdomain: team.subdomain,
        status: team.status,
        tier: team.tier,
        primaryColor: team.primaryColor,
        secondaryColor: team.secondaryColor,
        expiresOn: team.expiresOn ? team.expiresOn.split('T')[0] : '',
        logoUrl: team.logoUrl || '',
      });
      setError(null);
      setSubdomainAvailable(null);
      setFieldErrors({});
      setShowCustomPrimaryColor(!colorOptions.some(c => c.value === team.primaryColor));
      setShowCustomSecondaryColor(!secondaryColorOptions.some(c => c.value === team.secondaryColor));
    }
  }, [isOpen, team, colorOptions, secondaryColorOptions]);

  // Reset form when modal closes
  useEffect(() => {
    if (!isOpen) {
      setError(null);
      setSubdomainAvailable(null);
      setFieldErrors({});
      setShowCustomPrimaryColor(false);
      setShowCustomSecondaryColor(false);
    }
  }, [isOpen]);

  // Check subdomain availability with debounce
  useEffect(() => {
    if (!team || !formData.subdomain || formData.subdomain === team.subdomain) {
      setSubdomainAvailable(null);
      return;
    }

    const timer = setTimeout(async () => {
      try {
        setSubdomainChecking(true);
        const available = await teamsApi.checkSubdomainAvailability(formData.subdomain, team.id);
        setSubdomainAvailable(available);
      } catch (err) {
        console.error('Error checking subdomain:', err);
        setSubdomainAvailable(null);
      } finally {
        setSubdomainChecking(false);
      }
    }, 500);

    return () => clearTimeout(timer);
  }, [formData.subdomain, team]);

  const validateField = (field: keyof FormData, value: string | TeamTier | TeamStatus): string | null => {
    switch (field) {
      case 'name':
        if (typeof value === 'string' && value.trim().length < 2) {
          return 'Team name must be at least 2 characters long';
        }
        break;
      case 'subdomain':
        if (typeof value === 'string') {
          if (value.length < 3) {
            return 'Subdomain must be at least 3 characters long';
          }
          if (!/^[a-z0-9-]+$/.test(value)) {
            return 'Subdomain can only contain lowercase letters, numbers, and hyphens';
          }
        }
        break;
      case 'primaryColor':
      case 'secondaryColor':
        if (typeof value === 'string' && value && !/^#[0-9A-Fa-f]{6}$/.test(value)) {
          return 'Color must be a valid hex color code (e.g., #FF0000)';
        }
        break;
      case 'logoUrl':
        if (typeof value === 'string' && value) {
          try {
            new URL(value);
          } catch {
            return 'Logo URL must be a valid URL';
          }
        }
        break;
    }
    return null;
  };

  const handleInputChange = (field: keyof FormData, value: string | TeamTier | TeamStatus) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    
    // Clear field error when user starts typing
    if (fieldErrors[field]) {
      setFieldErrors(prev => {
        const newErrors = { ...prev };
        delete newErrors[field];
        return newErrors;
      });
    }
  };

  const formatSubdomain = (value: string) => {
    return value.toLowerCase().replace(/[^a-z0-9-]/g, '');
  };

  const handleSubdomainChange = (value: string) => {
    const formatted = formatSubdomain(value);
    handleInputChange('subdomain', formatted);
  };

  const isFormValid = () => {
    if (!team) return false;
    
    return formData.name.trim().length >= 2 &&
           formData.subdomain.length >= 3 &&
           /^[a-z0-9-]+$/.test(formData.subdomain) &&
           (subdomainAvailable !== false) &&
           (!formData.primaryColor || /^#[0-9A-Fa-f]{6}$/.test(formData.primaryColor)) &&
           (!formData.secondaryColor || /^#[0-9A-Fa-f]{6}$/.test(formData.secondaryColor));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!team) return;
    
    // Validate all fields before submission
    const newFieldErrors: Record<string, string> = {};
    
    const nameError = validateField('name', formData.name);
    if (nameError) newFieldErrors.name = nameError;
    
    const subdomainError = validateField('subdomain', formData.subdomain);
    if (subdomainError) newFieldErrors.subdomain = subdomainError;
    
    const primaryColorError = validateField('primaryColor', formData.primaryColor);
    if (primaryColorError) newFieldErrors.primaryColor = primaryColorError;
    
    const secondaryColorError = validateField('secondaryColor', formData.secondaryColor);
    if (secondaryColorError) newFieldErrors.secondaryColor = secondaryColorError;
    
    const logoUrlError = validateField('logoUrl', formData.logoUrl);
    if (logoUrlError) newFieldErrors.logoUrl = logoUrlError;
    
    // Set field errors if any exist
    if (Object.keys(newFieldErrors).length > 0) {
      setFieldErrors(newFieldErrors);
      setError('Please fix the errors below before submitting');
      return;
    }
    
    if (!isFormValid()) {
      setError('Please fill in all required fields correctly');
      return;
    }

    try {
      setLoading(true);
      setError(null);
      setFieldErrors({});

      // Only include changed fields in the update DTO
      const dto: GlobalAdminUpdateTeamDto = {};
      
      if (formData.name !== team.name) {
        dto.name = formData.name;
      }
      
      if (formData.subdomain !== team.subdomain) {
        dto.subdomain = formData.subdomain;
      }
      
      if (formData.status !== team.status) {
        dto.status = formData.status;
      }
      
      if (formData.tier !== team.tier) {
        dto.tier = formData.tier;
      }
      
      if (formData.primaryColor !== team.primaryColor) {
        dto.primaryColor = formData.primaryColor;
      }
      
      if (formData.secondaryColor !== team.secondaryColor) {
        dto.secondaryColor = formData.secondaryColor;
      }

      const originalExpiresOn = team.expiresOn ? team.expiresOn.split('T')[0] : '';
      if (formData.expiresOn !== originalExpiresOn) {
        dto.expiresOn = formData.expiresOn || undefined;
      }
      
      const originalLogoUrl = team.logoUrl || '';
      if (formData.logoUrl !== originalLogoUrl) {
        dto.logoUrl = formData.logoUrl || undefined;
      }

      const updatedTeam = await teamsApi.updateTeam(team.id, dto);
      onTeamUpdated(updatedTeam);
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update team. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  if (!team) return null;

  return (
    <FormModal
      isOpen={isOpen}
      onClose={onClose}
      title="Edit Team"
      icon={<FontAwesomeIcon icon={faBuilding} className="w-5 h-5 text-blue-600" />}
      size="xl"
      onSubmit={handleSubmit}
      submitText="Update Team"
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
              Status
            </label>
            <select
              value={formData.status}
              onChange={(e) => handleInputChange('status', parseInt(e.target.value) as TeamStatus)}
              className="w-full px-3 py-2.5 border border-gray-300 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all duration-200"
              disabled={loading}
            >
              <option value={TeamStatus.Active}>Active</option>
              <option value={TeamStatus.Suspended}>Suspended</option>
              <option value={TeamStatus.Expired}>Expired</option>
              <option value={TeamStatus.PendingSetup}>Pending Setup</option>
            </select>
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
              Logo URL (optional)
            </label>
            <input
              type="url"
              value={formData.logoUrl}
              onChange={(e) => handleInputChange('logoUrl', e.target.value)}
              className={`w-full px-3 py-2.5 border rounded-xl focus:outline-none focus:ring-2 focus:border-transparent transition-all duration-200 ${
                fieldErrors.logoUrl
                  ? 'border-red-500 focus:ring-red-500 bg-red-50'
                  : 'border-gray-300 focus:ring-blue-500'
              }`}
              placeholder="https://example.com/logo.png"
              disabled={loading}
            />
            {fieldErrors.logoUrl && (
              <p className="mt-1 text-sm text-red-600 flex items-center">
                <FontAwesomeIcon icon={faExclamationTriangle} className="w-4 h-4 mr-1" />
                {fieldErrors.logoUrl}
              </p>
            )}
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
              {fieldErrors.primaryColor && (
                <p className="mt-1 text-sm text-red-600 flex items-center">
                  <FontAwesomeIcon icon={faExclamationTriangle} className="w-4 h-4 mr-1" />
                  {fieldErrors.primaryColor}
                </p>
              )}
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
              {fieldErrors.secondaryColor && (
                <p className="mt-1 text-sm text-red-600 flex items-center">
                  <FontAwesomeIcon icon={faExclamationTriangle} className="w-4 h-4 mr-1" />
                  {fieldErrors.secondaryColor}
                </p>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Owner Information (read-only) */}
      <div className="mb-6 p-4 bg-gray-50 rounded-lg">
        <h3 className="text-lg font-semibold text-gray-900 mb-3">Team Owner</h3>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Name</label>
            <p className="text-sm text-gray-900">{team.ownerDisplayName}</p>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Email</label>
            <p className="text-sm text-gray-900">{team.ownerEmail}</p>
          </div>
        </div>
        <p className="text-xs text-gray-500 mt-2">
          To change the team owner, use the Transfer Ownership action from the team actions menu.
        </p>
      </div>
    </FormModal>
  );
} 