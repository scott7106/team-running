'use client';

import { useState } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faKey, faEye, faEyeSlash, faCopy, faCheck } from '@fortawesome/free-solid-svg-icons';
import FormModal from './FormModal';
import { GlobalAdminUserDto, GlobalAdminResetPasswordDto, PasswordResetResultDto } from '@/types/user';
import { usersApi, ApiError } from '@/utils/api';

interface ResetPasswordModalProps {
  isOpen: boolean;
  onClose: () => void;
  onPasswordReset: () => void;
  user: GlobalAdminUserDto | null;
}

export default function ResetPasswordModal({
  isOpen,
  onClose,
  onPasswordReset,
  user
}: ResetPasswordModalProps) {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [resetResult, setResetResult] = useState<PasswordResetResultDto | null>(null);
  const [newPassword, setNewPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [requirePasswordChange, setRequirePasswordChange] = useState(true);
  const [sendPasswordByEmail, setSendPasswordByEmail] = useState(false);
  const [sendPasswordBySms, setSendPasswordBySms] = useState(false);
  const [useCustomPassword, setUseCustomPassword] = useState(false);
  const [copied, setCopied] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!user) return;

    try {
      setLoading(true);
      setError(null);

      const dto: GlobalAdminResetPasswordDto = {
        newPassword: useCustomPassword ? newPassword : undefined,
        requirePasswordChange,
        sendPasswordByEmail,
        sendPasswordBySms
      };

      const result = await usersApi.resetPassword(user.id, dto);
      setResetResult(result);
      
      if (!useCustomPassword && result.temporaryPassword) {
        // Auto-show password for temporary passwords
        setShowPassword(true);
      }
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('Failed to reset password. Please try again.');
      }
      console.error('Error resetting password:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleClose = () => {
    setResetResult(null);
    setNewPassword('');
    setShowPassword(false);
    setRequirePasswordChange(true);
    setSendPasswordByEmail(false);
    setSendPasswordBySms(false);
    setUseCustomPassword(false);
    setError(null);
    setCopied(false);
    onClose();
  };

  const handlePasswordReset = () => {
    onPasswordReset();
    handleClose();
  };

  const copyToClipboard = async (text: string) => {
    try {
      await navigator.clipboard.writeText(text);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch (err) {
      console.error('Failed to copy to clipboard:', err);
    }
  };

  const isSubmitDisabled = useCustomPassword && (!newPassword || newPassword.length < 8);

  if (resetResult) {
    return (
      <FormModal
        isOpen={isOpen}
        onClose={handleClose}
        title="Password Reset Successful"
        icon={<FontAwesomeIcon icon={faKey} className="w-5 h-5 text-green-600" />}
        size="md"
        onSubmit={handlePasswordReset}
        submitText="Done"
        showCancelButton={false}
        loading={false}
      >
        <div className="space-y-4">
          <div className="bg-green-50 border border-green-200 rounded-lg p-4">
            <h3 className="text-lg font-medium text-green-900 mb-2">
              Password reset for {resetResult.firstName} {resetResult.lastName}
            </h3>
            <p className="text-sm text-green-700 mb-3">
              The password has been successfully reset.
            </p>
            
            {resetResult.temporaryPassword && (
              <div className="bg-white border border-green-300 rounded-lg p-3">
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Temporary Password
                </label>
                <div className="flex items-center space-x-2">
                  <div className="flex-1 relative">
                    <input
                      type={showPassword ? 'text' : 'password'}
                      value={resetResult.temporaryPassword}
                      readOnly
                      className="w-full p-2 border border-gray-300 rounded-lg bg-gray-50 font-mono text-sm"
                    />
                    <button
                      type="button"
                      onClick={() => setShowPassword(!showPassword)}
                      className="absolute right-2 top-1/2 transform -translate-y-1/2 text-gray-500 hover:text-gray-700"
                    >
                      <FontAwesomeIcon icon={showPassword ? faEyeSlash : faEye} className="w-4 h-4" />
                    </button>
                  </div>
                  <button
                    type="button"
                    onClick={() => copyToClipboard(resetResult.temporaryPassword!)}
                    className="px-3 py-2 bg-blue-100 text-blue-700 rounded-lg hover:bg-blue-200 transition-colors"
                    title="Copy to clipboard"
                  >
                    <FontAwesomeIcon icon={copied ? faCheck : faCopy} className="w-4 h-4" />
                  </button>
                </div>
                {copied && (
                  <p className="text-xs text-green-600 mt-1">Copied to clipboard!</p>
                )}
              </div>
            )}
            
                         <div className="mt-4 space-y-2 text-sm text-green-700">
               {resetResult.requirePasswordChange && (
                 <p>• User will be required to change password on next login</p>
               )}
               {resetResult.passwordSentByEmail && (
                 <p>• Password has been sent to the user&apos;s email address</p>
               )}
               {resetResult.passwordSentBySms && (
                 <p>• Password has been sent to the user&apos;s phone number</p>
               )}
            </div>
          </div>
        </div>
      </FormModal>
    );
  }

  return (
    <FormModal
      isOpen={isOpen}
      onClose={handleClose}
      title="Reset Password"
      icon={<FontAwesomeIcon icon={faKey} className="w-5 h-5 text-blue-600" />}
      size="md"
      onSubmit={handleSubmit}
      submitText="Reset Password"
      loading={loading}
      error={error}
      isSubmitDisabled={isSubmitDisabled}
    >
      <div className="space-y-6">
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
          <h3 className="text-lg font-medium text-blue-900 mb-1">
            Reset password for {user?.firstName} {user?.lastName}
          </h3>
          <p className="text-sm text-blue-700">
            {user?.email}
          </p>
        </div>

        <div className="space-y-4">
          <div>
            <label className="flex items-center text-sm font-medium text-gray-700 mb-3">
              <input
                type="radio"
                name="passwordType"
                checked={!useCustomPassword}
                onChange={() => setUseCustomPassword(false)}
                className="mr-2"
              />
              Generate temporary password automatically
            </label>
            <label className="flex items-center text-sm font-medium text-gray-700">
              <input
                type="radio"
                name="passwordType"
                checked={useCustomPassword}
                onChange={() => setUseCustomPassword(true)}
                className="mr-2"
              />
              Set custom password
            </label>
          </div>

          {useCustomPassword && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                New Password
              </label>
              <div className="relative">
                <input
                  type={showPassword ? 'text' : 'password'}
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  className="w-full p-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="Enter new password (minimum 8 characters)"
                  minLength={8}
                  required={useCustomPassword}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-1/2 transform -translate-y-1/2 text-gray-500 hover:text-gray-700"
                >
                  <FontAwesomeIcon icon={showPassword ? faEyeSlash : faEye} className="w-4 h-4" />
                </button>
              </div>
              {newPassword && newPassword.length < 8 && (
                <p className="text-xs text-red-600 mt-1">Password must be at least 8 characters long</p>
              )}
            </div>
          )}

          <div className="space-y-3">
            <h4 className="text-sm font-medium text-gray-700">Password Reset Options</h4>
            
            <label className="flex items-center">
              <input
                type="checkbox"
                checked={requirePasswordChange}
                onChange={(e) => setRequirePasswordChange(e.target.checked)}
                className="mr-2"
              />
              <span className="text-sm text-gray-700">
                Require password change on next login
              </span>
            </label>

            <label className="flex items-center">
              <input
                type="checkbox"
                checked={sendPasswordByEmail}
                onChange={(e) => setSendPasswordByEmail(e.target.checked)}
                className="mr-2"
              />
              <span className="text-sm text-gray-700">
                Send password to user via email
              </span>
            </label>

            {user?.phoneNumber && (
              <label className="flex items-center">
                <input
                  type="checkbox"
                  checked={sendPasswordBySms}
                  onChange={(e) => setSendPasswordBySms(e.target.checked)}
                  className="mr-2"
                />
                <span className="text-sm text-gray-700">
                  Send password to user via SMS ({user.phoneNumber})
                </span>
              </label>
            )}
          </div>
        </div>
      </div>
    </FormModal>
  );
} 