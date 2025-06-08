'use client';

import { ReactNode } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faSpinner, faExclamationTriangle } from '@fortawesome/free-solid-svg-icons';
import Modal from './Modal';

interface FormModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  icon?: ReactNode;
  size?: 'sm' | 'md' | 'lg' | 'xl' | '2xl';
  children: ReactNode;
  onSubmit: (e: React.FormEvent) => void;
  submitText?: string;
  cancelText?: string;
  loading?: boolean;
  error?: string | null;
  isSubmitDisabled?: boolean;
  showCancelButton?: boolean;
}

export default function FormModal({
  isOpen,
  onClose,
  title,
  icon,
  size = 'lg',
  children,
  onSubmit,
  submitText = 'Submit',
  cancelText = 'Cancel',
  loading = false,
  error,
  isSubmitDisabled = false,
  showCancelButton = true
}: FormModalProps) {
  const footer = (
    <div className="flex items-center justify-end space-x-3">
      {showCancelButton && (
        <button
          type="button"
          onClick={onClose}
          className="px-4 py-2.5 text-gray-700 bg-gray-100 rounded-xl hover:bg-gray-200 transition-all duration-200 font-medium focus:outline-none focus:ring-2 focus:ring-gray-400 focus:ring-offset-2"
          disabled={loading}
        >
          {cancelText}
        </button>
      )}
      <button
        type="submit"
        form="modal-form"
        disabled={loading || isSubmitDisabled}
        className="px-6 py-2.5 bg-blue-600 text-white rounded-xl hover:bg-blue-700 transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed flex items-center font-medium focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 shadow-sm"
      >
        {loading ? (
          <>
            <FontAwesomeIcon icon={faSpinner} className="w-4 h-4 mr-2 animate-spin" />
            Loading...
          </>
        ) : (
          submitText
        )}
      </button>
    </div>
  );

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={title}
      icon={icon}
      size={size}
      footer={footer}
      loading={loading}
      closeOnBackdropClick={!loading}
    >
      <form id="modal-form" onSubmit={onSubmit} className="p-6">
        {error && (
          <div className="mb-6 p-4 bg-red-50/80 border border-red-200/60 rounded-xl flex items-start backdrop-blur-sm">
            <FontAwesomeIcon icon={faExclamationTriangle} className="w-5 h-5 text-red-600 mr-3 mt-0.5 flex-shrink-0" />
            <p className="text-red-700 text-sm leading-relaxed">{error}</p>
          </div>
        )}
        {children}
      </form>
    </Modal>
  );
} 