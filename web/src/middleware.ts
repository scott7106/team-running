import { NextRequest, NextResponse } from 'next/server';
import { shouldRefreshTokenForSubdomain } from './utils/auth';

/**
 * Extracts subdomain from hostname
 */
function extractSubdomain(hostname: string): string {
  // Remove port number from hostname for proper subdomain extraction
  const hostnameWithoutPort = hostname.split(':')[0];
  
  if (hostnameWithoutPort.includes('localhost')) {
    // Local development: extract subdomain from hostname like 'wildcats.localhost' or 'localhost'
    const parts = hostnameWithoutPort.split('.');
    if (parts.length > 1) {
      // wildcats.localhost -> 'wildcats'
      return parts[0];
    } else {
      // localhost -> 'localhost'
      return parts[0];
    }
  } else {
    // Production: extract subdomain from hostname like 'wildcats.teamstride.net'
    const parts = hostnameWithoutPort.split('.');
    if (parts.length > 2) {
      return parts[0];
    }
  }
  
  return '';
}

export async function middleware(request: NextRequest) {
  const hostname = request.headers.get('host') || '';
  const url = request.nextUrl;
  
  // Extract subdomain from hostname
  const subdomain = extractSubdomain(hostname);

  console.log(`[Middleware] Processing request for hostname: ${hostname}, subdomain: ${subdomain}, path: ${url.pathname}`);

  // Check if token needs refresh for the current subdomain
  const needsRefresh = shouldRefreshTokenForSubdomain(subdomain);
  if (needsRefresh) {
    console.log(`[Middleware] Token needs refresh for subdomain: ${subdomain}`);
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
    // For team subdomains, we'll let the client-side handle theme data fetching
    // Middleware only sets the context and subdomain for routing purposes
    console.log(`[Middleware] Team subdomain detected: ${subdomain}`);

    // Set team context headers (client-side will fetch theme data)
    const response = NextResponse.next();
    response.headers.set('x-context', 'team');
    response.headers.set('x-team-subdomain', subdomain);
    response.headers.set('x-needs-token-refresh', needsRefresh.toString());
    
    console.log(`[Middleware] Set team context for subdomain: ${subdomain}`);
    return response;
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