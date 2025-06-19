'use client';

import { useState, ReactNode } from 'react';
import { useUser } from '@/contexts/auth-context';
import UnifiedHeader from './unified-header';
import NavigationSidebar from './navigation-sidebar';
import { NavigationItem } from './navigation-config';

interface BaseLayoutProps {
  children: ReactNode;
  pageTitle: string;
  currentSection?: string;
  variant: 'admin' | 'team';
  navigationItems: NavigationItem[];
  // Branding props
  siteName: string;
  logoUrl?: string;
  showTeamTheme?: boolean;
}

export default function BaseLayout({
  children,
  pageTitle,
  currentSection,
  variant,
  navigationItems,
  siteName,
  logoUrl,
  showTeamTheme = false
}: BaseLayoutProps) {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const { user, isAuthenticated } = useUser();

  // For authenticated pages, show loading state if user is not loaded
  if (!isAuthenticated || !user) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-lg text-gray-600">Loading...</div>
      </div>
    );
  }

  const mainBgClass = variant === 'team' && showTeamTheme 
    ? 'team-main-bg' 
    : 'bg-gray-50';

  return (
    <div className={`min-h-screen lg:flex ${mainBgClass}`}>
      {/* Navigation Sidebar */}
      <NavigationSidebar
        items={navigationItems}
        currentSection={currentSection}
        variant={variant}
        isOpen={isSidebarOpen}
        onClose={() => setIsSidebarOpen(false)}
        siteName={siteName}
        logoUrl={logoUrl}
        showTeamTheme={showTeamTheme}
      />

      {/* Main Content Area */}
      <div className="flex-1 lg:flex lg:flex-col lg:overflow-hidden">
        {/* Header */}
        <UnifiedHeader
          variant={variant}
          siteName={siteName}
          logoUrl={logoUrl}
          onMobileMenuToggle={() => setIsSidebarOpen(true)}
          showTeamTheme={showTeamTheme}
        />

        {/* Page Content */}
        <main className="flex-1 p-8">
          <div className="max-w-7xl mx-auto">
            {/* Page header */}
            <div className="mb-8">
              <h1 className="text-3xl font-bold text-gray-900">{pageTitle}</h1>
            </div>
            
            {/* Page content */}
            {children}
          </div>
        </main>
      </div>

      {/* Mobile overlay */}
      {isSidebarOpen && (
        <div 
          className="fixed inset-0 bg-black bg-opacity-50 z-40 lg:hidden"
          onClick={() => setIsSidebarOpen(false)}
        />
      )}
    </div>
  );
} 