'use client';

import { useEffect, useState } from 'react';
import { getTeamTheme } from '@/components/shared/url-theme-handler';

interface TeamThemeProviderProps {
  children: React.ReactNode;
}

export default function TeamThemeProvider({ children }: TeamThemeProviderProps) {
  const [themeStyles, setThemeStyles] = useState<React.CSSProperties>({});

  useEffect(() => {
    const theme = getTeamTheme();
    
    // Create style object for CSS custom properties
    const styles = {
      '--team-primary': theme.primaryColor,
      '--team-secondary': `${theme.primaryColor}20`, // 20% opacity
      '--team-primary-bg': theme.secondaryColor,
    } as React.CSSProperties;

    setThemeStyles(styles);
  }, []);

  return (
    <div style={themeStyles} className="team-themed">
      {children}
    </div>
  );
} 