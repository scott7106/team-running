'use client';

import { SubdomainThemeDto } from '@/types/team';

const CACHE_DURATION = 60 * 60 * 1000; // 1 hour
const API_BASE_URL = process.env.NODE_ENV === 'development' 
  ? 'http://localhost:5295'
  : process.env.NEXT_PUBLIC_API_BASE_URL || 'https://api.teamstride.net';

interface CachedThemeData {
  data: SubdomainThemeDto;
  timestamp: number;
}

const DEFAULT_THEME: SubdomainThemeDto = {
  teamId: '',
  teamName: '',
  subdomain: '',
  primaryColor: '#3B82F6',
  secondaryColor: '#F0FDF4',
  logoUrl: undefined
};

/**
 * Gets the cache key for a specific subdomain
 */
function getCacheKey(subdomain: string): string {
  return `teamTheme_${subdomain}`;
}

/**
 * Extracts subdomain from current hostname
 */
export function getCurrentSubdomain(): string {
  if (typeof window === 'undefined') return '';
  
  const hostname = window.location.hostname;
  
  if (hostname.includes('localhost')) {
    // Local development: extract subdomain from hostname like 'wildcats.localhost'
    const parts = hostname.split('.');
    if (parts.length > 1) {
      return parts[0]; // wildcats.localhost -> 'wildcats'
    }
    return ''; // just 'localhost' -> no subdomain
  } else {
    // Production: extract subdomain from hostname like 'wildcats.teamstride.net'
    const parts = hostname.split('.');
    if (parts.length > 2) {
      return parts[0]; // wildcats.teamstride.net -> 'wildcats'
    }
    return ''; // no subdomain
  }
}

/**
 * Checks if cached theme data is still valid
 */
function isCacheValid(cachedData: CachedThemeData): boolean {
  const now = Date.now();
  return now - cachedData.timestamp < CACHE_DURATION;
}

/**
 * Retrieves theme data from localStorage cache
 */
function getCachedThemeData(subdomain: string): SubdomainThemeDto | null {
  if (typeof window === 'undefined') return null;
  
  try {
    const cacheKey = getCacheKey(subdomain);
    const cached = localStorage.getItem(cacheKey);
    
    if (!cached) return null;
    
    const cachedData: CachedThemeData = JSON.parse(cached);
    
    if (isCacheValid(cachedData)) {
      console.log(`[TeamTheme] Using cached theme data for ${subdomain}`);
      return cachedData.data;
    } else {
      // Cache expired, remove it
      localStorage.removeItem(cacheKey);
      console.log(`[TeamTheme] Cache expired for ${subdomain}`);
      return null;
    }
  } catch (error) {
    console.error('Error reading cached theme data:', error);
    return null;
  }
}

/**
 * Stores theme data in localStorage cache
 */
function setCachedThemeData(subdomain: string, data: SubdomainThemeDto): void {
  if (typeof window === 'undefined') return;
  
  try {
    const cacheKey = getCacheKey(subdomain);
    const cachedData: CachedThemeData = {
      data,
      timestamp: Date.now()
    };
    
    localStorage.setItem(cacheKey, JSON.stringify(cachedData));
    console.log(`[TeamTheme] Cached theme data for ${subdomain}`);
  } catch (error) {
    console.error('Error caching theme data:', error);
  }
}

/**
 * Fetches theme data from the API
 */
async function fetchThemeDataFromAPI(subdomain: string): Promise<SubdomainThemeDto> {
  const url = `${API_BASE_URL}/api/tenant-switcher/${encodeURIComponent(subdomain)}/theme/`;
  
  console.log(`[TeamTheme] Fetching theme data from API: ${url}`);
  
  const response = await fetch(url, {
    method: 'GET',
    headers: {
      'Content-Type': 'application/json',
    },
  });
  
  if (!response.ok) {
    throw new Error(`Failed to fetch theme data: ${response.status} ${response.statusText}`);
  }
  
  const data: SubdomainThemeDto = await response.json();
  console.log(`[TeamTheme] Successfully fetched theme data for ${subdomain}`, data);
  
  return data;
}

/**
 * Gets theme data for a specific subdomain, using cache when available
 */
export async function getTeamThemeData(subdomain: string): Promise<SubdomainThemeDto> {
  if (!subdomain) {
    console.log('[TeamTheme] No subdomain provided, returning default theme');
    return DEFAULT_THEME;
  }
  
  // Check cache first
  const cachedData = getCachedThemeData(subdomain);
  if (cachedData) {
    return cachedData;
  }
  
  // Fetch from API
  try {
    const data = await fetchThemeDataFromAPI(subdomain);
    
    // Cache the result
    setCachedThemeData(subdomain, data);
    
    return data;
  } catch (error) {
    console.error(`[TeamTheme] Failed to fetch theme data for ${subdomain}:`, error);
    
    // Try to return any expired cached data as fallback
    try {
      const cacheKey = getCacheKey(subdomain);
      const cached = localStorage.getItem(cacheKey);
      if (cached) {
        const cachedData: CachedThemeData = JSON.parse(cached);
        console.log(`[TeamTheme] Using expired cache as fallback for ${subdomain}`);
        return cachedData.data;
      }
    } catch (cacheError) {
      console.error('Error reading fallback cache:', cacheError);
    }
    
    // Return default theme as last resort
    return DEFAULT_THEME;
  }
}

/**
 * Gets theme data for the current subdomain
 */
export async function getCurrentTeamThemeData(): Promise<SubdomainThemeDto> {
  const subdomain = getCurrentSubdomain();
  return getTeamThemeData(subdomain);
}

/**
 * Clears cached theme data for a specific subdomain
 */
export function clearCachedThemeData(subdomain: string): void {
  if (typeof window === 'undefined') return;
  
  try {
    const cacheKey = getCacheKey(subdomain);
    localStorage.removeItem(cacheKey);
    console.log(`[TeamTheme] Cleared cached theme data for ${subdomain}`);
  } catch (error) {
    console.error('Error clearing cached theme data:', error);
  }
}

/**
 * Clears all cached theme data
 */
export function clearAllCachedThemeData(): void {
  if (typeof window === 'undefined') return;
  
  try {
    const keys = Object.keys(localStorage);
    const themeKeys = keys.filter(key => key.startsWith('teamTheme_'));
    
    themeKeys.forEach(key => localStorage.removeItem(key));
    console.log(`[TeamTheme] Cleared ${themeKeys.length} cached theme entries`);
  } catch (error) {
    console.error('Error clearing all cached theme data:', error);
  }
} 