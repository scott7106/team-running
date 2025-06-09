'use client';

import { useEffect } from 'react';
import { useSearchParams } from 'next/navigation';
import { getCurrentSubdomain } from '@/utils/team-theme';
import { SubdomainThemeDto } from '@/types/team';

const DEFAULT_THEME: Pick<SubdomainThemeDto, 'primaryColor' | 'secondaryColor'> = {
  primaryColor: '#10B981',
  secondaryColor: '#F0FDF4'
};

export default function UrlThemeHandler() {
  const searchParams = useSearchParams();

  useEffect(() => {
    const handleThemeFromUrl = () => {
      const primaryColor = searchParams.get('primaryColor');
      const secondaryColor = searchParams.get('secondaryColor');

      if (primaryColor && secondaryColor) {
        // Legacy URL parameter support
        const themeData: Pick<SubdomainThemeDto, 'primaryColor' | 'secondaryColor'> = {
          primaryColor,
          secondaryColor
        };

        // Store theme data in localStorage using old format for backward compatibility
        localStorage.setItem('teamTheme', JSON.stringify(themeData));

        // Clean up URL by removing theme parameters
        const url = new URL(window.location.href);
        url.searchParams.delete('primaryColor');
        url.searchParams.delete('secondaryColor');
        
        // Replace the current URL without the theme parameters
        window.history.replaceState(null, '', url.toString());
        
        console.log('[UrlThemeHandler] Legacy theme data received and stored from URL parameters');
      }
    };

    handleThemeFromUrl();
  }, [searchParams]);

  return null; // This component doesn't render anything
}

/**
 * Gets team theme - checks new cache format first, then falls back to legacy format
 * @deprecated Use getTeamThemeData from team-theme service instead
 */
export function getTeamTheme(): Pick<SubdomainThemeDto, 'primaryColor' | 'secondaryColor'> {
  if (typeof window === 'undefined') return DEFAULT_THEME;
  
  try {
    // First try to get from new cache format
    const subdomain = getCurrentSubdomain();
    if (subdomain) {
      const newCacheKey = `teamTheme_${subdomain}`;
      const newCached = localStorage.getItem(newCacheKey);
      if (newCached) {
        const parsed = JSON.parse(newCached);
        if (parsed.data) {
          return {
            primaryColor: parsed.data.primaryColor,
            secondaryColor: parsed.data.secondaryColor
          };
        }
      }
    }

    // Fall back to legacy format
    const stored = localStorage.getItem('teamTheme');
    if (stored) {
      return JSON.parse(stored);
    }
  } catch (error) {
    console.error('Error reading team theme from localStorage:', error);
  }
  
  return DEFAULT_THEME;
} 