'use client';

import { useState, useEffect } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faUserEdit, faUser, faCheck, faExclamationTriangle } from '@fortawesome/free-solid-svg-icons';
import FormModal from '@/components/ui/FormModal';
import { GlobalAdminUserDto, UserStatus } from '@/types/user';
import { usersApi } from '@/utils/api';

interface EditUserData {
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  status: UserStatus;
  isActive: boolean;
  emailConfirmed: boolean;
  phoneNumberConfirmed: boolean;
}

interface EditUserModalProps {
  isOpen: boolean;
  onClose: () => void;
  onUserUpdated: () => void;
  user: GlobalAdminUserDto | null;
}

export default function EditUserModal({ isOpen, onClose, onUserUpdated, user }: EditUserModalProps) {
  const [formData, setFormData] = useState<EditUserData>({
    email: '',
    firstName: '',
    lastName: '',
    phoneNumber: '',
    status: UserStatus.Active,
    isActive: true,
    emailConfirmed: true,
    phoneNumberConfirmed: false
  });
  
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [emailChecking, setEmailChecking] = useState(false);
  const [emailAvailable, setEmailAvailable] = useState<boolean | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  // Initialize form data when user changes
  useEffect(() => {
    if (user && isOpen) {
      setFormData({
        email: user.email,
        firstName: user.firstName,
        lastName: user.lastName,
        phoneNumber: user.phoneNumber || '',
        status: user.status,
        isActive: user.isActive,
        emailConfirmed: user.emailConfirmed,
        phoneNumberConfirmed: user.phoneNumberConfirmed
      });
      setEmailAvailable(null);
      setError(null);
      setFieldErrors({});
    }
  }, [user, isOpen]);

  const validateField = (field: keyof EditUserData, value: string | boolean | UserStatus): string | null => {
    switch (field) {
      case 'email':
        if (!value) return 'Email is required';
        if (typeof value === 'string' && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) {
          return 'Please enter a valid email address';
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

  const checkEmailAvailability = async (email: string) => {
    if (!user || !email || email === user.email) {
      setEmailAvailable(null);
      return;
    }

    setEmailChecking(true);
    try {
      const available = await usersApi.checkEmailAvailability(email, user.id);
      setEmailAvailable(available);
      if (!available) {
        setFieldErrors(prev => ({ ...prev, email: 'This email address is already in use' }));
      } else {
        setFieldErrors(prev => {
          const newErrors = { ...prev };
          delete newErrors.email;
          return newErrors;
        });
      }
    } catch (err) {
      console.error('Error checking email availability:', err);
      setEmailAvailable(null);
    } finally {
      setEmailChecking(false);
    }
  };

  const handleInputChange = (field: keyof EditUserData, value: string | boolean | UserStatus) => {
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
    
    // Validate field on change for immediate feedback
    const fieldError = validateField(field, value);
    if (fieldError) {
      setFieldErrors(prev => ({ ...prev, [field]: fieldError }));
    }

    // Check email availability when email changes
    if (field === 'email' && typeof value === 'string') {
      checkEmailAvailability(value);
    }
  };

  const isFormValid = () => {
    if (!user) return false;
    
    // Check for required fields
    if (!formData.email || !formData.firstName || !formData.lastName) return false;
    
    // Check for field errors
    if (Object.keys(fieldErrors).length > 0) return false;
    
    // Check email availability (if email changed)
    if (formData.email !== user.email && emailAvailable !== true) return false;
    
    // Check if any changes were made
    return (
      formData.email !== user.email ||
      formData.firstName !== user.firstName ||
      formData.lastName !== user.lastName ||
      formData.phoneNumber !== (user.phoneNumber || '') ||
      formData.status !== user.status ||
      formData.isActive !== user.isActive ||
      formData.emailConfirmed !== user.emailConfirmed ||
      formData.phoneNumberConfirmed !== user.phoneNumberConfirmed
    );
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!user) return;
    
    // Validate all fields before submission
    const newFieldErrors: Record<string, string> = {};
    
    Object.keys(formData).forEach((key) => {
      const field = key as keyof EditUserData;
      const error = validateField(field, formData[field]);
      if (error) {
        newFieldErrors[field] = error;
      }
    });
    
    // Set field errors if any exist
    if (Object.keys(newFieldErrors).length > 0) {
      setFieldErrors(newFieldErrors);
      setError('Please fix the errors below before submitting');
      return;
    }
    
    if (!isFormValid()) {
      setError('Please make changes to update the user');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      await usersApi.updateUser(user.id, {
        email: formData.email,
        firstName: formData.firstName,
        lastName: formData.lastName,
        phoneNumber: formData.phoneNumber || undefined,
        status: formData.status,
        isActive: formData.isActive,
        emailConfirmed: formData.emailConfirmed,
        phoneNumberConfirmed: formData.phoneNumberConfirmed
      });
      
      onUserUpdated();
      onClose();
    } catch (err) {
      console.error('Failed to update user:', err);
      const message = err instanceof Error ? err.message : 'Failed to update user. Please try again.';
      setError(message);
    } finally {
      setLoading(false);
    }
  };

  const handleClose = () => {
    setError(null);
    setFieldErrors({});
    setEmailAvailable(null);
    onClose();
  };

  if (!user) return null;

  return (
    <FormModal
      isOpen={isOpen}
      onClose={handleClose}
      onSubmit={handleSubmit}
      title="Edit User"
      icon={<FontAwesomeIcon icon={faUserEdit} className="w-5 h-5 text-blue-600" />}
      submitText="Update User"
      loading={loading}
      error={error}
      size="lg"
      isSubmitDisabled={!isFormValid()}
    >
      <div className="space-y-6">
        {/* Basic Information */}
        <div>
          <h3 className="text-lg font-medium text-gray-900 mb-4 flex items-center">
            <FontAwesomeIcon icon={faUser} className="w-4 h-4 mr-2 text-blue-600" />
            Basic Information
          </h3>
          
          <div className="space-y-4">
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
                  maxLength={50}
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
                  maxLength={50}
                />
                {fieldErrors.lastName && (
                  <p className="mt-1 text-sm text-red-600 flex items-center">
                    <FontAwesomeIcon icon={faExclamationTriangle} className="w-4 h-4 mr-1" />
                    {fieldErrors.lastName}
                  </p>
                )}
              </div>
            </div>

            <div>
              <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
                Email Address <span className="text-red-500">*</span>
              </label>
              <div className="relative">
                <input
                  type="email"
                  id="email"
                  value={formData.email}
                  onChange={(e) => handleInputChange('email', e.target.value)}
                  className={`w-full px-3 py-2 pr-10 border rounded-lg focus:outline-none focus:ring-2 focus:border-transparent ${
                    fieldErrors.email
                      ? 'border-red-500 focus:ring-red-500 bg-red-50'
                      : emailAvailable === true
                      ? 'border-green-500 focus:ring-green-500 bg-green-50'
                      : 'border-gray-300 focus:ring-blue-500'
                  }`}
                  placeholder="Enter email address"
                  required
                  disabled={loading}
                  maxLength={256}
                />
                <div className="absolute inset-y-0 right-0 pr-3 flex items-center">
                  {emailChecking ? (
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-600"></div>
                  ) : emailAvailable === true ? (
                    <FontAwesomeIcon icon={faCheck} className="w-4 h-4 text-green-600" />
                  ) : null}
                </div>
              </div>
              {fieldErrors.email && (
                <p className="mt-1 text-sm text-red-600 flex items-center">
                  <FontAwesomeIcon icon={faExclamationTriangle} className="w-4 h-4 mr-1" />
                  {fieldErrors.email}
                </p>
              )}
              {emailAvailable === true && formData.email !== user.email && (
                <p className="mt-1 text-sm text-green-600 flex items-center">
                  <FontAwesomeIcon icon={faCheck} className="w-4 h-4 mr-1" />
                  Email address is available
                </p>
              )}
            </div>

            <div>
              <label htmlFor="phoneNumber" className="block text-sm font-medium text-gray-700 mb-1">
                Phone Number
              </label>
              <input
                type="tel"
                id="phoneNumber"
                value={formData.phoneNumber}
                onChange={(e) => handleInputChange('phoneNumber', e.target.value)}
                className={`w-full px-3 py-2 border rounded-lg focus:outline-none focus:ring-2 focus:border-transparent ${
                  fieldErrors.phoneNumber
                    ? 'border-red-500 focus:ring-red-500 bg-red-50'
                    : 'border-gray-300 focus:ring-blue-500'
                }`}
                placeholder="Enter phone number"
                disabled={loading}
                maxLength={15}
              />
              {fieldErrors.phoneNumber && (
                <p className="mt-1 text-sm text-red-600 flex items-center">
                  <FontAwesomeIcon icon={faExclamationTriangle} className="w-4 h-4 mr-1" />
                  {fieldErrors.phoneNumber}
                </p>
              )}
            </div>
          </div>
        </div>

        {/* Status and Settings */}
        <div>
          <h3 className="text-lg font-medium text-gray-900 mb-4">
            Account Status
          </h3>
          
          <div className="space-y-4">
            <div>
              <label htmlFor="status" className="block text-sm font-medium text-gray-700 mb-1">
                User Status <span className="text-red-500">*</span>
              </label>
              <select
                id="status"
                value={formData.status}
                onChange={(e) => handleInputChange('status', parseInt(e.target.value) as UserStatus)}
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                required
                disabled={loading}
              >
                <option value={UserStatus.Active}>Active</option>
                <option value={UserStatus.Inactive}>Inactive</option>
                <option value={UserStatus.Suspended}>Suspended</option>
              </select>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="space-y-3">
                <label className="flex items-center">
                  <input
                    type="checkbox"
                    checked={formData.isActive}
                    onChange={(e) => handleInputChange('isActive', e.target.checked)}
                    className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                    disabled={loading}
                  />
                  <span className="ml-2 text-sm text-gray-700">
                    Account is Active
                  </span>
                </label>
                
                <label className="flex items-center">
                  <input
                    type="checkbox"
                    checked={formData.emailConfirmed}
                    onChange={(e) => handleInputChange('emailConfirmed', e.target.checked)}
                    className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                    disabled={loading}
                  />
                  <span className="ml-2 text-sm text-gray-700">
                    Email Confirmed
                  </span>
                </label>
              </div>

              <div className="space-y-3">
                <label className="flex items-center">
                  <input
                    type="checkbox"
                    checked={formData.phoneNumberConfirmed}
                    onChange={(e) => handleInputChange('phoneNumberConfirmed', e.target.checked)}
                    className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                    disabled={loading}
                  />
                  <span className="ml-2 text-sm text-gray-700">
                    Phone Number Confirmed
                  </span>
                </label>
              </div>
            </div>
          </div>
        </div>
      </div>
    </FormModal>
  );
} 