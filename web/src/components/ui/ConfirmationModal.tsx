'use client';

import { ReactNode } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faSpinner } from '@fortawesome/free-solid-svg-icons';
import Modal from './Modal';

interface ConfirmationModalProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
  title: string;
  message: string;
  icon?: ReactNode;
  size?: 'sm' | 'md' | 'lg' | 'xl' | '2xl';
  confirmText?: string;
  cancelText?: string;
  confirmButtonClass?: string;
  loading?: boolean;
}

export default function ConfirmationModal({
  isOpen,
  onClose,
  onConfirm,
  title,
  message,
  icon,
  size = 'md',
  confirmText = 'Confirm',
  cancelText = 'Cancel',
  confirmButtonClass = 'bg-red-600 text-white hover:bg-red-700 focus:ring-red-500',
  loading = false
}: ConfirmationModalProps) {
  const handleConfirm = () => {
    if (!loading) {
      onConfirm();
    }
  };

  const handleCancel = () => {
    if (!loading) {
      onClose();
    }
  };

  const footer = (
    <div className="flex items-center justify-end space-x-3">
      <button
        type="button"
        onClick={handleCancel}
        className="px-4 py-2.5 text-gray-700 bg-gray-100 rounded-xl hover:bg-gray-200 transition-all duration-200 font-medium focus:outline-none focus:ring-2 focus:ring-gray-400 focus:ring-offset-2"
        disabled={loading}
      >
        {cancelText}
      </button>
      <button
        type="button"
        onClick={handleConfirm}
        disabled={loading}
        className={`px-6 py-2.5 rounded-xl transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed flex items-center font-medium focus:outline-none focus:ring-2 focus:ring-offset-2 shadow-sm ${confirmButtonClass}`}
      >
        {loading ? (
          <>
            <FontAwesomeIcon icon={faSpinner} className="w-4 h-4 mr-2 animate-spin" />
            Processing...
          </>
        ) : (
          confirmText
        )}
      </button>
    </div>
  );

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleCancel}
      title={title}
      icon={icon}
      size={size}
      footer={footer}
      loading={loading}
      closeOnBackdropClick={!loading}
    >
      <div className="p-6">
        <p className="text-gray-700 leading-relaxed">{message}</p>
      </div>
    </Modal>
  );
} 