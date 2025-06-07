'use client';

import { ReactNode } from 'react';
import UserContextMenu from './user-context-menu';

interface AuthenticatedLayoutProps {
  children: ReactNode;
  /** Page title to display in the header */
  title?: string;
  /** Optional custom header content */
  headerContent?: ReactNode;
  /** Whether to show the page title in the header */
  showTitle?: boolean;
  /** Additional CSS classes for the main content area */
  contentClassName?: string;
}

export default function AuthenticatedLayout({ 
  children, 
  title, 
  headerContent, 
  showTitle = true,
  contentClassName = ''
}: AuthenticatedLayoutProps) {
  return (
    <div className="min-h-screen bg-gray-50">
      {/* Top navigation bar */}
      <div className="bg-white shadow-sm border-b">
        <div className="flex items-center justify-between h-16 px-4 max-w-7xl mx-auto">
          {/* Left side - Title or custom content */}
          <div className="flex items-center">
            {showTitle && title && (
              <h1 className="text-xl font-semibold text-gray-900">{title}</h1>
            )}
            {headerContent}
          </div>
          
          {/* Right side - User Context Menu */}
          <div className="flex items-center">
            <UserContextMenu />
          </div>
        </div>
      </div>

      {/* Main content */}
      <main className={`flex-1 ${contentClassName}`}>
        {children}
      </main>
    </div>
  );
} 