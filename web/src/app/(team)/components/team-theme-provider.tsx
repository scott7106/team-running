'use client';

import { useEffect, useState } from 'react';
import { getCurrentTeamThemeData } from '@/utils/team-theme';
import { SubdomainThemeDto } from '@/types/team';

interface TeamThemeProviderProps {
  children: React.ReactNode;
}

export default function TeamThemeProvider({ children }: TeamThemeProviderProps) {
  const [themeStyles, setThemeStyles] = useState<React.CSSProperties>({});
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const loadTheme = async () => {
      try {
        const themeData: SubdomainThemeDto = await getCurrentTeamThemeData();
        
        // Create style object for CSS custom properties
        const styles = {
          '--team-primary': themeData.primaryColor,
          '--team-secondary': `${themeData.primaryColor}20`, // 20% opacity
          '--team-primary-bg': themeData.secondaryColor,
          '--team-logo-url': themeData.logoUrl ? `url(${themeData.logoUrl})` : 'none',
        } as React.CSSProperties;

        setThemeStyles(styles);
        console.log('[TeamThemeProvider] Theme loaded:', themeData);
      } catch (error) {
        console.error('[TeamThemeProvider] Failed to load theme:', error);
        // Fallback to default theme
        const styles = {
          '--team-primary': '#3B82F6',
          '--team-secondary': '#3B82F620',
          '--team-primary-bg': '#F0FDF4',
          '--team-logo-url': 'none',
        } as React.CSSProperties;
        setThemeStyles(styles);
      } finally {
        setIsLoading(false);
      }
    };

    loadTheme();
  }, []);

  if (isLoading) {
    // Show a minimal loading state while theme loads
    return (
      <div className="team-themed">
        {children}
      </div>
    );
  }

  return (
    <div style={themeStyles} className="team-themed">
      {children}
    </div>
  );
} 