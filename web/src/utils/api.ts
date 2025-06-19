import { TeamsApiParams, TeamsApiResponse, CreateTeamWithNewOwnerDto, CreateTeamWithExistingOwnerDto, GlobalAdminTeamDto, GlobalAdminUpdateTeamDto } from '@/types/team';
import { UsersApiParams, UsersApiResponse, GlobalAdminUserDto, UserStatus, GlobalAdminResetPasswordDto, PasswordResetResultDto } from '@/types/user';
import { 
  TeamRegistrationDto, 
  SubmitRegistrationDto, 
  UpdateRegistrationStatusDto, 
  TeamRegistrationWindowDto, 
  CreateRegistrationWindowDto, 
  UpdateRegistrationWindowDto,
  UserRegistrationDto
} from '@/types/registration';
import { 
  AthleteDto, 
  CreateAthleteDto, 
  UpdateAthleteDto, 
  AthleteApiParams, 
  PaginatedAthleteResponse, 
  AthleteRole, 
  UpdateAthleteProfileDto 
} from '@/types/athlete';

export interface DashboardStatsDto {
  activeTeamsCount: number;
  totalUsersCount: number;
  globalAdminsCount: number;
}

export interface PublicTeamCreationResultDto {
  teamId: string;
  teamName: string;
  teamSubdomain: string;
  redirectUrl: string;
}

class ApiError extends Error {
  status: number;
  
  constructor(message: string, status: number) {
    super(message);
    this.status = status;
    this.name = 'ApiError';
  }
}

const getAuthToken = (): string | null => {
  if (typeof window === 'undefined') return null;
  return localStorage.getItem('token');
};

const getCurrentSubdomain = (): string => {
  if (typeof window === 'undefined') return '';
  
  const hostname = window.location.hostname;
  
  if (hostname.includes('localhost')) {
    const parts = hostname.split('.');
    if (parts.length > 1) {
      return parts[0]; // wildcats.localhost -> 'wildcats'
    }
    return ''; // just 'localhost' -> no subdomain
  } else {
    const parts = hostname.split('.');
    if (parts.length > 2) {
      return parts[0]; // wildcats.teamstride.net -> 'wildcats'
    }
    return ''; // no subdomain
  }
};

const buildQueryString = (params: Record<string, unknown>): string => {
  const searchParams = new URLSearchParams();
  
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') {
      searchParams.append(key, value.toString());
    }
  });
  
  const queryString = searchParams.toString();
  return queryString ? `?${queryString}` : '';
};

const apiRequest = async <T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> => {
  const token = getAuthToken();
  const subdomain = getCurrentSubdomain();
  
  const defaultHeaders: HeadersInit = {
    'Content-Type': 'application/json',
  };
  
  if (token) {
    defaultHeaders.Authorization = `Bearer ${token}`;
  }
  
  // Add subdomain header for API team context resolution
  if (subdomain) {
    defaultHeaders['X-Subdomain'] = subdomain;
  }
  
  const config: RequestInit = {
    ...options,
    headers: {
      ...defaultHeaders,
      ...options.headers,
    },
  };
  
  const response = await fetch(endpoint, config);
  
  if (!response.ok) {
    let errorMessage = `Request failed with status ${response.status}`;
    
    try {
      const errorData = await response.json();
      console.error('API Error Response:', errorData);
      
      // Handle ASP.NET ModelState validation errors
      if (errorData.errors && typeof errorData.errors === 'object') {
        const validationErrors = Object.entries(errorData.errors)
          .map(([field, messages]) => `${field}: ${Array.isArray(messages) ? messages.join(', ') : messages}`)
          .join('; ');
        errorMessage = `Validation errors: ${validationErrors}`;
      } else {
        errorMessage = errorData.message || errorData.detail || errorData.title || errorMessage;
      }
    } catch {
      // If we can't parse error as JSON, use default message
    }
    
    throw new ApiError(errorMessage, response.status);
  }
  
  // Check if response has content to parse
  const contentType = response.headers.get('content-type');
  const contentLength = response.headers.get('content-length');
  
  // If response is 204 No Content or has no content-length/content-type indicating JSON
  if (response.status === 204 || contentLength === '0' || 
      (!contentType?.includes('application/json') && !contentType?.includes('text/json'))) {
    return undefined as T;
  }
  
  // Check if response body is empty
  const text = await response.text();
  if (!text.trim()) {
    return undefined as T;
  }
  
  try {
    return JSON.parse(text);
  } catch (error) {
    console.warn('Failed to parse response as JSON:', text, error);
    return undefined as T;
  }
};

export const publicTeamsApi = {
  checkSubdomainAvailability: async (subdomain: string, excludeTeamId?: string): Promise<boolean> => {
    const params: Record<string, string> = { subdomain };
    if (excludeTeamId) {
      params.excludeTeamId = excludeTeamId;
    }
    const queryString = buildQueryString(params);
    return apiRequest<boolean>(`/api/public/teams/subdomain-availability${queryString}`);
  },

  createTeamWithNewOwner: async (dto: CreateTeamWithNewOwnerDto): Promise<PublicTeamCreationResultDto> => {
    return apiRequest<PublicTeamCreationResultDto>('/api/public/teams/with-new-owner', {
      method: 'POST',
      body: JSON.stringify(dto),
    });
  },

  createTeamWithExistingOwner: async (dto: CreateTeamWithExistingOwnerDto): Promise<PublicTeamCreationResultDto> => {
    return apiRequest<PublicTeamCreationResultDto>('/api/public/teams/with-existing-owner', {
      method: 'POST',
      body: JSON.stringify(dto),
    });
  },
};

export const teamsApi = {
  getTeams: async (params: TeamsApiParams = {}): Promise<TeamsApiResponse> => {
    const queryString = buildQueryString(params as Record<string, unknown>);
    return apiRequest<TeamsApiResponse>(`/api/admin/teams${queryString}`);
  },
  
  getTeamById: async (teamId: string): Promise<GlobalAdminTeamDto> => {
    return apiRequest<GlobalAdminTeamDto>(`/api/admin/teams/${teamId}`);
  },

  createTeamWithNewOwnerAdmin: async (dto: CreateTeamWithNewOwnerDto): Promise<GlobalAdminTeamDto> => {
    return apiRequest<GlobalAdminTeamDto>('/api/admin/teams/with-new-owner', {
      method: 'POST',
      body: JSON.stringify(dto),
    });
  },
  
  createTeamWithExistingOwnerAdmin: async (dto: CreateTeamWithExistingOwnerDto): Promise<GlobalAdminTeamDto> => {
    return apiRequest<GlobalAdminTeamDto>('/api/admin/teams/with-existing-owner', {
      method: 'POST',
      body: JSON.stringify(dto),
    });
  },

  updateTeam: async (teamId: string, dto: GlobalAdminUpdateTeamDto): Promise<GlobalAdminTeamDto> => {
    return apiRequest<GlobalAdminTeamDto>(`/api/admin/teams/${teamId}`, {
      method: 'PUT',
      body: JSON.stringify(dto),
    });
  },

  deleteTeam: async (teamId: string): Promise<void> => {
    return apiRequest<void>(`/api/admin/teams/${teamId}`, {
      method: 'DELETE',
    });
  },

  purgeTeam: async (teamId: string): Promise<void> => {
    return apiRequest<void>(`/api/admin/teams/${teamId}/purge`, {
      method: 'DELETE',
    });
  },

  getGradeLevels: async (): Promise<Array<{value: number, name: string, displayName: string}>> => {
    return apiRequest<Array<{value: number, name: string, displayName: string}>>('/api/teams/gradelevels');
  },
};

interface ApplicationRole {
  id: string;
  name: string;
  description?: string;
  userCount: number;
}

interface CreateUserDto {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  applicationRoles: string[];
  requirePasswordChange: boolean;
}

interface UpdateUserDto {
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  status: UserStatus;
  isActive: boolean;
  emailConfirmed: boolean;
  phoneNumberConfirmed: boolean;
}

export const usersApi = {
  getUsers: async (params: UsersApiParams = {}): Promise<UsersApiResponse> => {
    const queryString = buildQueryString(params as Record<string, unknown>);
    return apiRequest<UsersApiResponse>(`/api/admin/users${queryString}`);
  },

  createUser: async (dto: CreateUserDto): Promise<GlobalAdminUserDto> => {
    return apiRequest<GlobalAdminUserDto>('/api/admin/users', {
      method: 'POST',
      body: JSON.stringify(dto),
    });
  },

  getApplicationRoles: async (): Promise<ApplicationRole[]> => {
    return apiRequest<ApplicationRole[]>('/api/admin/users/roles');
  },

  deleteUser: async (userId: string): Promise<void> => {
    return apiRequest<void>(`/api/admin/users/${userId}`, {
      method: 'DELETE',
    });
  },

  updateUser: async (userId: string, dto: UpdateUserDto): Promise<GlobalAdminUserDto> => {
    return apiRequest<GlobalAdminUserDto>(`/api/admin/users/${userId}`, {
      method: 'PUT',
      body: JSON.stringify(dto),
    });
  },

  checkEmailAvailability: async (email: string, excludeUserId?: string): Promise<boolean> => {
    const params: Record<string, string> = { email };
    if (excludeUserId) {
      params.excludeUserId = excludeUserId;
    }
    const queryString = buildQueryString(params);
    return apiRequest<boolean>(`/api/admin/users/email-availability${queryString}`);
  },

  purgeUser: async (userId: string): Promise<void> => {
    return apiRequest<void>(`/api/admin/users/${userId}/purge`, {
      method: 'DELETE',
    });
  },

  resetPassword: async (userId: string, dto: GlobalAdminResetPasswordDto): Promise<PasswordResetResultDto> => {
    return apiRequest<PasswordResetResultDto>(`/api/admin/users/${userId}/reset-password`, {
      method: 'POST',
      body: JSON.stringify(dto),
    });
  },

  resetLockout: async (userId: string): Promise<GlobalAdminUserDto> => {
    return apiRequest<GlobalAdminUserDto>(`/api/admin/users/${userId}/reset-lockout`, {
      method: 'POST',
    });
  },
};

export const dashboardApi = {
  getStats: async (): Promise<DashboardStatsDto> => {
    return apiRequest<DashboardStatsDto>('/api/admin/dashboard/stats');
  },
};

export const registrationApi = {
  // Team registration window management
  createRegistrationWindow: async (teamId: string, dto: CreateRegistrationWindowDto): Promise<TeamRegistrationWindowDto> => {
    return apiRequest<TeamRegistrationWindowDto>(`/api/teams/${teamId}/registration/windows`, {
      method: 'POST',
      body: JSON.stringify(dto),
    });
  },

  updateRegistrationWindow: async (teamId: string, windowId: string, dto: UpdateRegistrationWindowDto): Promise<TeamRegistrationWindowDto> => {
    return apiRequest<TeamRegistrationWindowDto>(`/api/teams/${teamId}/registration/windows/${windowId}`, {
      method: 'PUT',
      body: JSON.stringify(dto),
    });
  },

  getRegistrationWindows: async (teamId: string): Promise<TeamRegistrationWindowDto[]> => {
    return apiRequest<TeamRegistrationWindowDto[]>(`/api/teams/${teamId}/registration/windows`);
  },

  getActiveRegistrationWindow: async (teamId: string): Promise<TeamRegistrationWindowDto | null> => {
    try {
      return await apiRequest<TeamRegistrationWindowDto>(`/api/teams/${teamId}/registration/windows/active`);
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) {
        return null;
      }
      throw error;
    }
  },

  // Team registration submissions
  submitRegistration: async (teamId: string, dto: SubmitRegistrationDto): Promise<TeamRegistrationDto> => {
    return apiRequest<TeamRegistrationDto>(`/api/teams/${teamId}/registration`, {
      method: 'POST',
      body: JSON.stringify(dto),
    });
  },

  updateRegistrationStatus: async (teamId: string, registrationId: string, dto: UpdateRegistrationStatusDto): Promise<TeamRegistrationDto> => {
    return apiRequest<TeamRegistrationDto>(`/api/teams/${teamId}/registration/${registrationId}/status`, {
      method: 'PUT',
      body: JSON.stringify(dto),
    });
  },

  getRegistrations: async (teamId: string): Promise<TeamRegistrationDto[]> => {
    return apiRequest<TeamRegistrationDto[]>(`/api/teams/${teamId}/registration`);
  },

  getWaitlist: async (teamId: string): Promise<TeamRegistrationDto[]> => {
    return apiRequest<TeamRegistrationDto[]>(`/api/teams/${teamId}/registration/waitlist`);
  },

  // User account registration
  registerUser: async (dto: UserRegistrationDto): Promise<void> => {
    return apiRequest<void>('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify(dto),
    });
  },
};

export const athletesApi = {
  getAthletes: async (params: AthleteApiParams = {}): Promise<PaginatedAthleteResponse> => {
    const queryString = buildQueryString(params as Record<string, unknown>);
    return apiRequest<PaginatedAthleteResponse>(`/api/teams/athletes${queryString}`);
  },

  getAthlete: async (athleteId: string): Promise<AthleteDto> => {
    return apiRequest<AthleteDto>(`/api/teams/athletes/${athleteId}`);
  },

  getAthleteByUserId: async (userId: string): Promise<AthleteDto | null> => {
    try {
      return await apiRequest<AthleteDto>(`/api/teams/athletes/by-user/${userId}`);
    } catch (error) {
      if (error instanceof ApiError && error.status === 204) {
        return null;
      }
      throw error;
    }
  },

  getCaptains: async (): Promise<AthleteDto[]> => {
    return apiRequest<AthleteDto[]>('/api/teams/athletes/captains');
  },

  createAthlete: async (dto: CreateAthleteDto): Promise<AthleteDto> => {
    return apiRequest<AthleteDto>('/api/teams/athletes', {
      method: 'POST',
      body: JSON.stringify(dto),
    });
  },

  updateAthlete: async (athleteId: string, dto: UpdateAthleteDto): Promise<AthleteDto> => {
    return apiRequest<AthleteDto>(`/api/teams/athletes/${athleteId}`, {
      method: 'PUT',
      body: JSON.stringify(dto),
    });
  },

  updateAthleteRole: async (athleteId: string, role: AthleteRole): Promise<AthleteDto> => {
    return apiRequest<AthleteDto>(`/api/teams/athletes/${athleteId}/role`, {
      method: 'PATCH',
      body: JSON.stringify(role),
    });
  },

  updatePhysicalStatus: async (athleteId: string, hasPhysical: boolean): Promise<AthleteDto> => {
    return apiRequest<AthleteDto>(`/api/teams/athletes/${athleteId}/physical`, {
      method: 'PATCH',
      body: JSON.stringify(hasPhysical),
    });
  },

  updateWaiverStatus: async (athleteId: string, hasSigned: boolean): Promise<AthleteDto> => {
    return apiRequest<AthleteDto>(`/api/teams/athletes/${athleteId}/waiver`, {
      method: 'PATCH',
      body: JSON.stringify(hasSigned),
    });
  },

  updateAthleteProfile: async (athleteId: string, profileDto: UpdateAthleteProfileDto): Promise<AthleteDto> => {
    return apiRequest<AthleteDto>(`/api/teams/athletes/${athleteId}/profile`, {
      method: 'PUT',
      body: JSON.stringify(profileDto),
    });
  },

  deleteAthlete: async (athleteId: string): Promise<void> => {
    return apiRequest<void>(`/api/teams/athletes/${athleteId}`, {
      method: 'DELETE',
    });
  },

  isAthleteInTeam: async (athleteId: string): Promise<boolean> => {
    return apiRequest<boolean>(`/api/teams/athletes/${athleteId}/is-in-team`);
  },
};

export { ApiError }; 