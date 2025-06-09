'use client';

import { useEffect } from 'react';
import { useSearchParams } from 'next/navigation';

interface ThemeData {
  primaryColor: string;
  secondaryColor: string;
}

const DEFAULT_THEME: ThemeData = {
  primaryColor: '#10B981',
  secondaryColor: '#F0FDF4'
};

export default function UrlThemeHandler() {
  const searchParams = useSearchParams();

  useEffect(() => {
    const primaryColor = searchParams.get('primaryColor');
    const secondaryColor = searchParams.get('secondaryColor');

    if (primaryColor && secondaryColor) {
      const themeData: ThemeData = {
        primaryColor,
        secondaryColor
      };

      // Store theme data in localStorage
      localStorage.setItem('teamTheme', JSON.stringify(themeData));

      // Clean up URL by removing theme parameters
      const url = new URL(window.location.href);
      url.searchParams.delete('primaryColor');
      url.searchParams.delete('secondaryColor');
      
      // Replace the current URL without the theme parameters
      window.history.replaceState(null, '', url.toString());
      
      console.log('[UrlThemeHandler] Theme data received and stored from URL parameters');
    }
  }, [searchParams]);

  return null; // This component doesn't render anything
}

export function getTeamTheme(): ThemeData {
  if (typeof window === 'undefined') return DEFAULT_THEME;
  
  try {
    const stored = localStorage.getItem('teamTheme');
    if (stored) {
      return JSON.parse(stored);
    }
  } catch (error) {
    console.error('Error reading team theme from localStorage:', error);
  }
  
  return DEFAULT_THEME;
} 