import { decodeJwt } from 'jose';

export interface JwtClaims {
  sub: string; // user id
  email: string;
  first_name?: string;
  last_name?: string;
  is_global_admin?: string;
  team_id?: string;
  team_role?: string;
  member_type?: string;
  exp: number;
  iat: number;
  jti: string;
}

export function decodeToken(token: string): JwtClaims | null {
  try {
    return decodeJwt(token) as JwtClaims;
  } catch (error) {
    console.error('Failed to decode JWT token:', error);
    return null;
  }
}

export function getUserFromToken(): { firstName: string; lastName: string; email: string } | null {
  const token = localStorage.getItem('token');
  if (!token) return null;
  
  const claims = decodeToken(token);
  if (!claims) return null;
  
  return {
    firstName: claims.first_name || '',
    lastName: claims.last_name || '',
    email: claims.email || ''
  };
}

export function isTokenExpired(token: string): boolean {
  const claims = decodeToken(token);
  if (!claims) return true;
  
  const now = Math.floor(Date.now() / 1000);
  return claims.exp < now;
}

export function logout(): void {
  localStorage.removeItem('token');
  localStorage.removeItem('refreshToken');
  
  // Force a hard reload to ensure all state is cleared
  window.location.replace('/');
} 