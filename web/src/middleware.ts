import { NextRequest, NextResponse } from 'next/server';
import { shouldRefreshTokenForSubdomain } from './utils/auth';

interface TeamData {
  id: string;
  name: string;
  subdomain: string;
  theme: {
    primaryColor: string;
    secondaryColor: string;
    logoUrl?: string;
  };
}

// Cache for tenant subdomains to avoid repeated API calls
let tenantsCache: { teamId: string; subdomain: string }[] | null = null;
let cacheExpiry: number = 0;
const CACHE_DURATION = 5 * 60 * 1000; // 5 minutes

async function resolveTeamBySubdomain(subdomain: string): Promise<TeamData | null> {
  try {
    // Check if cache is valid
    const now = Date.now();
    if (!tenantsCache || now > cacheExpiry) {
      // Fetch all tenants from the public API
      const apiUrl = process.env.NODE_ENV === 'development' 
        ? 'http://localhost:5295'
        : process.env.API_BASE_URL || 'https://api.teamstride.net';
      
      const response = await fetch(`${apiUrl}/api/tenant-switcher/tenants/all`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        console.warn(`[Middleware] API error fetching tenants: ${response.status} ${response.statusText}`);
        return null;
      }

      const tenants = await response.json() as { teamId: string; subdomain: string }[];
      tenantsCache = tenants;
      cacheExpiry = now + CACHE_DURATION;
      console.log(`[Middleware] Cached ${tenants.length} tenants`);
    }

    // Find the team by subdomain
    const tenant = tenantsCache.find(t => t.subdomain === subdomain);
    if (!tenant) {
      console.log(`[Middleware] Team not found for subdomain: ${subdomain}`);
      return null;
    }

    // Return minimal team data for middleware use
    return {
      id: tenant.teamId,
      name: `Team ${subdomain}`, // We don't have the name, so use a placeholder
      subdomain: tenant.subdomain,
      theme: {
        primaryColor: '#1E40AF', // Default colors since we don't fetch full team data
        secondaryColor: '#FFFFFF',
        logoUrl: undefined,
      }
    };
  } catch (error) {
    console.error(`[Middleware] Error resolving team for subdomain ${subdomain}:`, error);
    return null;
  }
}

export async function middleware(request: NextRequest) {
  const hostname = request.headers.get('host') || '';
  const url = request.nextUrl;
  
  // Remove port number from hostname for proper subdomain extraction
  const hostnameWithoutPort = hostname.split(':')[0];
  
  // Extract subdomain from hostname
  // Handle both production (teamstride.net) and local development (localhost)
  let subdomain = '';
  
  if (hostnameWithoutPort.includes('localhost')) {
    // Local development: extract subdomain from hostname like 'wildcats.localhost' or 'localhost'
    const parts = hostnameWithoutPort.split('.');
    if (parts.length > 1) {
      // wildcats.localhost -> 'wildcats'
      subdomain = parts[0];
    } else {
      // localhost -> 'localhost'
      subdomain = parts[0];
    }
  } else {
    // Production: extract subdomain from hostname like 'wildcats.teamstride.net'
    const parts = hostnameWithoutPort.split('.');
    if (parts.length > 2) {
      subdomain = parts[0];
    }
  }

  console.log(`[Middleware] Processing request for hostname: ${hostname}, subdomain: ${subdomain}, path: ${url.pathname}`);

  // Check if token needs refresh for the current subdomain
  const needsRefresh = shouldRefreshTokenForSubdomain(subdomain);
  if (needsRefresh) {
    console.log(`[Middleware] Token needs refresh for subdomain: ${subdomain}`);
    // Note: Client-side token refresh will be handled by the frontend
    // We continue with the request and let the frontend handle the refresh
  }

  // Handle www subdomain - marketing site
  if (subdomain === 'www' || subdomain === '' || subdomain === 'localhost') {
    // Set context for marketing site
    const response = NextResponse.next();
    response.headers.set('x-context', 'www');
    response.headers.set('x-needs-token-refresh', needsRefresh.toString());
    console.log(`[Middleware] Setting www context for hostname: ${hostname}`);
    return response;
  }

  // Handle app subdomain - global admin
  if (subdomain === 'app') {
    // Set context for admin site
    const response = NextResponse.next();
    response.headers.set('x-context', 'app');
    response.headers.set('x-needs-token-refresh', needsRefresh.toString());
    console.log(`[Middleware] Setting app context for hostname: ${hostname}`);
    return response;
  }

  // Handle team subdomains
  if (subdomain && subdomain !== 'www' && subdomain !== 'app') {
    try {
      // Resolve team from subdomain
      const team = await resolveTeamBySubdomain(subdomain);
      
      if (!team) {
        // Team doesn't exist - rewrite to a redirect handler page
        console.log(`[Middleware] Team not found for subdomain: ${subdomain}`);
        // Use rewrite to avoid cross-subdomain redirect issues
        const redirectHandlerUrl = new URL('/redirect-to-team-not-found', request.url);
        console.log(`[Middleware] Rewriting to redirect handler: ${redirectHandlerUrl}`);
        return NextResponse.rewrite(redirectHandlerUrl);
      }

      console.log(`[Middleware] Team resolved: ${team.name} (ID: ${team.id})`);

      // Set team context headers
      const response = NextResponse.next();
      response.headers.set('x-context', 'team');
      response.headers.set('x-team-id', team.id);
      response.headers.set('x-team-subdomain', team.subdomain);
      response.headers.set('x-team-name', team.name);
      response.headers.set('x-team-primary-color', team.theme.primaryColor);
      response.headers.set('x-team-secondary-color', team.theme.secondaryColor);
      response.headers.set('x-team-logo-url', team.theme.logoUrl || '');
      response.headers.set('x-needs-token-refresh', needsRefresh.toString());
      
      return response;
      
    } catch (error) {
      console.error(`[Middleware] Error resolving team for subdomain ${subdomain}:`, error);
      // Use relative redirect that works for both dev and production
      const redirectUrl = hostname.includes('localhost') 
        ? `http://localhost:3000/error`
        : `https://www.teamstride.net/error`;
      return NextResponse.redirect(new URL(redirectUrl));
    }
  }

  // Default: let the request pass through
  return NextResponse.next();
}

export const config = {
  // Match all paths except static files and API routes
  matcher: [
    /*
     * Match all request paths except for the ones starting with:
     * - api (API routes)
     * - _next/static (static files)
     * - _next/image (image optimization files)
     * - favicon.ico (favicon file)
     * - robots.txt, sitemap.xml, etc.
     */
    '/((?!api|_next/static|_next/image|favicon.ico|robots.txt|sitemap.xml).*)',
  ],
}; 