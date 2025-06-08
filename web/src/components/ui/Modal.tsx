'use client';

import { useEffect, ReactNode } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faTimes } from '@fortawesome/free-solid-svg-icons';

interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  icon?: ReactNode;
  size?: 'sm' | 'md' | 'lg' | 'xl' | '2xl';
  children: ReactNode;
  footer?: ReactNode;
  loading?: boolean;
  closeOnBackdropClick?: boolean;
  showCloseButton?: boolean;
}

export default function Modal({
  isOpen,
  onClose,
  title,
  icon,
  size = 'lg',
  children,
  footer,
  loading = false,
  closeOnBackdropClick = true,
  showCloseButton = true
}: ModalProps) {
  // Handle escape key to close modal
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && isOpen && !loading) {
        onClose();
      }
    };

    if (isOpen) {
      document.addEventListener('keydown', handleKeyDown);
      // Prevent body scroll when modal is open
      document.body.style.overflow = 'hidden';
    }

    return () => {
      document.removeEventListener('keydown', handleKeyDown);
      document.body.style.overflow = 'unset';
    };
  }, [isOpen, onClose, loading]);

  if (!isOpen) return null;

  const sizeClasses = {
    sm: 'max-w-sm',
    md: 'max-w-md',
    lg: 'max-w-2xl',
    xl: 'max-w-4xl',
    '2xl': 'max-w-6xl'
  };

  const handleBackdropClick = (e: React.MouseEvent) => {
    if (closeOnBackdropClick && !loading && e.target === e.currentTarget) {
      onClose();
    }
  };

  return (
    <>
      {/* Modern backdrop with subtle blur */}
      <div 
        className="fixed inset-0 bg-gray-900/20 backdrop-blur-sm z-50 overflow-y-auto"
        onClick={handleBackdropClick}
      >
        {/* Modal container with proper centering and mobile optimization */}
        <div className="min-h-full flex items-center justify-center p-4 sm:p-6 lg:p-8">
          <div 
            className={`bg-white/95 backdrop-blur-md rounded-2xl shadow-2xl w-full ${sizeClasses[size]} max-h-[calc(100vh-2rem)] sm:max-h-[calc(100vh-3rem)] lg:max-h-[90vh] border border-white/20 transform animate-modal-appear flex flex-col my-4 sm:my-6 lg:my-8`}
            onClick={(e) => e.stopPropagation()}
          >
            {/* Header - Fixed at top */}
            <div className="flex items-center justify-between p-4 sm:p-6 border-b border-gray-200/50 flex-shrink-0">
              <h2 className="text-lg sm:text-xl font-semibold text-gray-900 flex items-center">
                {icon && <span className="mr-3">{icon}</span>}
                {title}
              </h2>
              {showCloseButton && (
                <button
                  onClick={onClose}
                  className="p-2 hover:bg-gray-100/80 rounded-xl transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-gray-400 focus:ring-offset-2"
                  disabled={loading}
                >
                  <FontAwesomeIcon icon={faTimes} className="w-5 h-5 text-gray-500" />
                </button>
              )}
            </div>

            {/* Content - Scrollable */}
            <div className="flex-1 overflow-y-auto min-h-0">
              {children}
            </div>

            {/* Footer - Fixed at bottom */}
            {footer && (
              <div className="p-4 sm:p-6 border-t border-gray-200/50 bg-gray-50/30 flex-shrink-0">
                {footer}
              </div>
            )}
          </div>
        </div>
      </div>

      <style jsx>{`
        @keyframes modal-appear {
          from {
            opacity: 0;
            transform: scale(0.95) translateY(-10px);
          }
          to {
            opacity: 1;
            transform: scale(1) translateY(0);
          }
        }
        
        .animate-modal-appear {
          animation: modal-appear 0.2s cubic-bezier(0.16, 1, 0.3, 1);
        }
      `}</style>
    </>
  );
} 