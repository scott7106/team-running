'use client';

import { useState, useEffect } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faUserPlus, faUser, faEye, faEyeSlash, faExclamationTriangle } from '@fortawesome/free-solid-svg-icons';
import FormModal from '@/components/ui/form-modal';
import { usersApi } from '@/utils/api';

interface ApplicationRole {
  id: string;
  name: string;
  description?: string;
  userCount: number;
}

interface CreateUserData {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  applicationRoles: string[];
  requirePasswordChange: boolean;
}

interface CreateUserModalProps {
  isOpen: boolean;
  onClose: () => void;
  onUserCreated: () => void;
}

interface FieldErrors {
  email?: string;
  password?: string;
  firstName?: string;
  lastName?: string;
  phoneNumber?: string;
}

export default function CreateUserModal({ isOpen, onClose, onUserCreated }: CreateUserModalProps) {
  const [formData, setFormData] = useState<CreateUserData>({
    email: '',
    password: '',
    firstName: '',
    lastName: '',
    phoneNumber: '',
    applicationRoles: [],
    requirePasswordChange: true
  });
  
  const [availableRoles, setAvailableRoles] = useState<ApplicationRole[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [rolesLoading, setRolesLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});

  // Load available roles when modal opens
  useEffect(() => {
    if (isOpen) {
      loadRoles();
    }
  }, [isOpen]);

  const loadRoles = async () => {
    setRolesLoading(true);
    try {
      const roles = await usersApi.getApplicationRoles();
      setAvailableRoles(roles);
    } catch (err) {
      console.error('Failed to load application roles:', err);
      setError('Failed to load application roles. Please try again.');
    } finally {
      setRolesLoading(false);
    }
  };

  const validateField = (field: keyof CreateUserData, value: string | string[] | boolean): string | null => {
    switch (field) {
      case 'email':
        if (!value || (typeof value === 'string' && value.trim().length === 0)) {
          return 'Email is required';
        }
        if (typeof value === 'string') {
          const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
          if (!emailRegex.test(value)) {
            return 'Please enter a valid email address';
          }
          if (value.length > 256) {
            return 'Email cannot exceed 256 characters';
          }
        }
        return null;
      case 'password':
        if (!value || (typeof value === 'string' && value.length === 0)) {
          return 'Password is required';
        }
        if (typeof value === 'string') {
          if (value.length < 8) {
            return 'Password must be at least 8 characters long';
          }
          if (value.length > 100) {
            return 'Password cannot exceed 100 characters';
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
          if (!/(?=.*[^a-zA-Z\d])/.test(value)) {
            return 'Password must contain at least one special character';
          }
        }
        return null;
      case 'firstName':
        if (!value || (typeof value === 'string' && value.trim().length === 0)) {
          return 'First name is required';
        }
        if (typeof value === 'string' && value.trim().length > 50) {
          return 'First name cannot exceed 50 characters';
        }
        return null;
      case 'lastName':
        if (!value || (typeof value === 'string' && value.trim().length === 0)) {
          return 'Last name is required';
        }
        if (typeof value === 'string' && value.trim().length > 50) {
          return 'Last name cannot exceed 50 characters';
        }
        return null;
      case 'phoneNumber':
        if (typeof value === 'string' && value && value.length > 15) {
          return 'Phone number cannot exceed 15 characters';
        }
        return null;
      default:
        return null;
    }
  };

  const handleInputChange = (field: keyof CreateUserData, value: string | string[] | boolean) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    
    // Validate field and update errors (only for string fields that can have validation errors)
    if (field === 'email' || field === 'password' || field === 'firstName' || field === 'lastName' || field === 'phoneNumber') {
      const error = validateField(field, value);
      setFieldErrors(prev => ({
        ...prev,
        [field]: error
      }));
    }
  };

  const formatPhoneNumber = (value: string): string => {
    // Remove all non-digit characters
    const cleaned = value.replace(/\D/g, '');
    
    // Limit to 10 digits (US phone number)
    const limited = cleaned.slice(0, 10);
    
    if (limited.length === 0) return '';
    if (limited.length <= 3) return `(${limited}`;
    if (limited.length <= 6) return `(${limited.slice(0, 3)}) ${limited.slice(3)}`;
    return `(${limited.slice(0, 3)}) ${limited.slice(3, 6)}-${limited.slice(6)}`;
  };

  const handlePhoneNumberChange = (value: string) => {
    const formatted = formatPhoneNumber(value);
    handleInputChange('phoneNumber', formatted);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    // Validate all fields
    const errors: FieldErrors = {};
    const fields: (keyof FieldErrors)[] = ['email', 'password', 'firstName', 'lastName', 'phoneNumber'];
    
    fields.forEach(field => {
      const error = validateField(field, formData[field]);
      if (error) {
        errors[field] = error;
      }
    });

    setFieldErrors(errors);

    // Check if there are any validation errors
    if (Object.keys(errors).length > 0) {
      setError('Please fix the validation errors below');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      // Clean phone number for API (remove formatting)
      const phoneNumber = formData.phoneNumber ? formData.phoneNumber.replace(/\D/g, '') : undefined;
      
      await usersApi.createUser({
        email: formData.email,
        password: formData.password,
        firstName: formData.firstName,
        lastName: formData.lastName,
        phoneNumber,
        applicationRoles: formData.applicationRoles,
        requirePasswordChange: formData.requirePasswordChange
      });
      
      onUserCreated();
      handleClose();
    } catch (err) {
      console.error('Failed to create user:', err);
      const message = err instanceof Error ? err.message : 'Failed to create user. Please try again.';
      setError(message);
    } finally {
      setLoading(false);
    }
  };

  const handleClose = () => {
    setFormData({
      email: '',
      password: '',
      firstName: '',
      lastName: '',
      phoneNumber: '',
      applicationRoles: [],
      requirePasswordChange: true
    });
    setError(null);
    setFieldErrors({});
    setShowPassword(false);
    onClose();
  };

  const handleRoleToggle = (roleName: string) => {
    setFormData(prev => ({
      ...prev,
      applicationRoles: prev.applicationRoles.includes(roleName)
        ? prev.applicationRoles.filter(role => role !== roleName)
        : [...prev.applicationRoles, roleName]
    }));
  };

  const generatePassword = () => {
    // Define character sets
    const uppercase = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
    const lowercase = 'abcdefghijklmnopqrstuvwxyz';
    const digits = '0123456789';
    const symbols = '!@#$%^&*()_+-=[]{}|;:,.<>?';
    
    // Ensure at least one character from each required set
    let password = '';
    password += uppercase[Math.floor(Math.random() * uppercase.length)];
    password += lowercase[Math.floor(Math.random() * lowercase.length)];
    password += digits[Math.floor(Math.random() * digits.length)];
    password += symbols[Math.floor(Math.random() * symbols.length)];
    
    // Fill the remaining 8 characters randomly from all sets
    const allChars = uppercase + lowercase + digits + symbols;
    for (let i = 4; i < 12; i++) {
      password += allChars[Math.floor(Math.random() * allChars.length)];
    }
    
    // Shuffle the password to randomize character positions
    const shuffled = password.split('').sort(() => Math.random() - 0.5).join('');
    
    handleInputChange('password', shuffled);
  };

  return (
    <FormModal
      isOpen={isOpen}
      onClose={handleClose}
      onSubmit={handleSubmit}
      title="Create New User"
      icon={<FontAwesomeIcon icon={faUserPlus} className="w-5 h-5 text-blue-600" />}
      submitText="Create User"
      loading={loading}
      error={error}
      size="lg"
    >
      <div className="space-y-6">
        {/* Basic Information */}
        <div>
          <h3 className="text-lg font-medium text-gray-900 mb-4 flex items-center">
            <FontAwesomeIcon icon={faUser} className="w-4 h-4 mr-2 text-blue-600" />
            Basic Information
          </h3>
          
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label htmlFor="firstName" className="block text-sm font-medium text-gray-700 mb-1">
                First Name <span className="text-red-500">*</span>
              </label>
              <input
                type="text"
                id="firstName"
                value={formData.firstName}
                onChange={(e) => handleInputChange('firstName', e.target.value)}
                className={`w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:border-transparent ${
                  fieldErrors.firstName
                    ? 'border-red-500 focus:ring-red-500 bg-red-50'
                    : 'border-gray-300 focus:ring-blue-500'
                }`}
                placeholder="Enter first name"
                required
                disabled={loading}
              />
              {fieldErrors.firstName && (
                <p className="mt-1 text-sm text-red-600 flex items-center">
                  <FontAwesomeIcon icon={faExclamationTriangle} className="w-4 h-4 mr-1" />
                  {fieldErrors.firstName}
                </p>
              )}
            </div>
            
            <div>
              <label htmlFor="lastName" className="block text-sm font-medium text-gray-700 mb-1">
                Last Name <span className="text-red-500">*</span>
              </label>
              <input
                type="text"
                id="lastName"
                value={formData.lastName}
                onChange={(e) => handleInputChange('lastName', e.target.value)}
                className={`w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:border-transparent ${
                  fieldErrors.lastName
                    ? 'border-red-500 focus:ring-red-500 bg-red-50'
                    : 'border-gray-300 focus:ring-blue-500'
                }`}
                placeholder="Enter last name"
                required
                disabled={loading}
              />
              {fieldErrors.lastName && (
                <p className="mt-1 text-sm text-red-600 flex items-center">
                  <FontAwesomeIcon icon={faExclamationTriangle} className="w-4 h-4 mr-1" />
                  {fieldErrors.lastName}
                </p>
              )}
            </div>
          </div>

          <div className="mt-4">
            <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
              Email Address <span className="text-red-500">*</span>
            </label>
            <input
              type="email"
              id="email"
              value={formData.email}
              onChange={(e) => handleInputChange('email', e.target.value)}
              className={`w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:border-transparent ${
                fieldErrors.email
                  ? 'border-red-500 focus:ring-red-500 bg-red-50'
                  : 'border-gray-300 focus:ring-blue-500'
              }`}
              placeholder="Enter email address"
              required
              disabled={loading}
            />
            {fieldErrors.email && (
              <p className="mt-1 text-sm text-red-600 flex items-center">
                <FontAwesomeIcon icon={faExclamationTriangle} className="w-4 h-4 mr-1" />
                {fieldErrors.email}
              </p>
            )}
          </div>

          <div className="mt-4">
            <label htmlFor="phoneNumber" className="block text-sm font-medium text-gray-700 mb-1">
              Phone Number
            </label>
            <input
              type="tel"
              id="phoneNumber"
              value={formData.phoneNumber}
              onChange={(e) => handlePhoneNumberChange(e.target.value)}
              className={`w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:border-transparent ${
                fieldErrors.phoneNumber
                  ? 'border-red-500 focus:ring-red-500 bg-red-50'
                  : 'border-gray-300 focus:ring-blue-500'
              }`}
              placeholder="(555) 123-4567"
              disabled={loading}
            />
            {fieldErrors.phoneNumber && (
              <p className="mt-1 text-sm text-red-600 flex items-center">
                <FontAwesomeIcon icon={faExclamationTriangle} className="w-4 h-4 mr-1" />
                {fieldErrors.phoneNumber}
              </p>
            )}
          </div>
        </div>

        {/* Password Section */}
        <div>
          <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-1">
            Password <span className="text-red-500">*</span>
          </label>
          <div className="flex space-x-2">
            <div className="flex-1 relative">
              <input
                type={showPassword ? 'text' : 'password'}
                id="password"
                value={formData.password}
                onChange={(e) => handleInputChange('password', e.target.value)}
                className={`w-full px-3 py-2 pr-10 border rounded-lg focus:outline-none focus:ring-2 focus:border-transparent ${
                  fieldErrors.password
                    ? 'border-red-500 focus:ring-red-500 bg-red-50'
                    : 'border-gray-300 focus:ring-blue-500'
                }`}
                placeholder="Enter password"
                required
                disabled={loading}
              />
              <button
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                className="absolute right-3 top-1/2 transform -translate-y-1/2 text-gray-400 hover:text-gray-600"
                disabled={loading}
              >
                <FontAwesomeIcon icon={showPassword ? faEyeSlash : faEye} className="w-4 h-4" />
              </button>
            </div>
            <button
              type="button"
              onClick={generatePassword}
              className="px-3 py-2 text-sm bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 focus:outline-none focus:ring-2 focus:ring-gray-400 transition-colors"
              disabled={loading}
            >
              Generate
            </button>
          </div>
          {fieldErrors.password && (
            <p className="mt-1 text-sm text-red-600 flex items-center">
              <FontAwesomeIcon icon={faExclamationTriangle} className="w-4 h-4 mr-1" />
              {fieldErrors.password}
            </p>
          )}
          <div className="mt-1 text-xs text-gray-500">
            Password must contain: 8+ characters, uppercase, lowercase, number, and special character
          </div>
          
          <div className="mt-3">
            <label className="flex items-center">
              <input
                type="checkbox"
                checked={formData.requirePasswordChange}
                onChange={(e) => handleInputChange('requirePasswordChange', e.target.checked)}
                className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                disabled={loading}
              />
              <span className="ml-2 text-sm text-gray-700">
                Require password change on first login
              </span>
            </label>
          </div>
        </div>

        {/* Application Roles */}
        <div>
          <h3 className="text-sm font-medium text-gray-700 mb-3">
            Application Roles
          </h3>
          
          {rolesLoading ? (
            <div className="text-center py-4">
              <div className="text-sm text-gray-500">Loading roles...</div>
            </div>
          ) : availableRoles.length === 0 ? (
            <div className="text-center py-4">
              <div className="text-sm text-gray-500">No roles available</div>
            </div>
          ) : (
            <div className="space-y-2 max-h-32 overflow-y-auto border border-gray-200 rounded-lg p-3">
              {availableRoles.map((role) => (
                <label key={role.id} className="flex items-center">
                  <input
                    type="checkbox"
                    checked={formData.applicationRoles.includes(role.name)}
                    onChange={() => handleRoleToggle(role.name)}
                    className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                    disabled={loading}
                  />
                  <div className="ml-2">
                    <div className="text-sm font-medium text-gray-900">{role.name}</div>
                  </div>
                </label>
              ))}
            </div>
          )}
        </div>
      </div>
    </FormModal>
  );
} 