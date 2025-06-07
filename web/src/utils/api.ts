import { TeamsApiParams, TeamsApiResponse, CreateTeamWithNewOwnerDto, CreateTeamWithExistingOwnerDto, GlobalAdminTeamDto, GlobalAdminUpdateTeamDto } from '@/types/team';
import { UsersApiParams, UsersApiResponse, GlobalAdminUserDto, UserStatus } from '@/types/user';

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
  
  const defaultHeaders: HeadersInit = {
    'Content-Type': 'application/json',
  };
  
  if (token) {
    defaultHeaders.Authorization = `Bearer ${token}`;
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
      errorMessage = errorData.message || errorData.detail || errorMessage;
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

export const teamsApi = {
  getTeams: async (params: TeamsApiParams = {}): Promise<TeamsApiResponse> => {
    const queryString = buildQueryString(params as Record<string, unknown>);
    return apiRequest<TeamsApiResponse>(`/api/admin/teams${queryString}`);
  },
  
  createTeamWithNewOwner: async (dto: CreateTeamWithNewOwnerDto): Promise<GlobalAdminTeamDto> => {
    return apiRequest<GlobalAdminTeamDto>('/api/admin/teams/with-new-owner', {
      method: 'POST',
      body: JSON.stringify(dto),
    });
  },
  
  createTeamWithExistingOwner: async (dto: CreateTeamWithExistingOwnerDto): Promise<GlobalAdminTeamDto> => {
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

  checkSubdomainAvailability: async (subdomain: string, excludeTeamId?: string): Promise<boolean> => {
    const params: Record<string, string> = { subdomain };
    if (excludeTeamId) {
      params.excludeTeamId = excludeTeamId;
    }
    const queryString = buildQueryString(params);
    return apiRequest<boolean>(`/api/admin/teams/subdomain-availability${queryString}`);
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
};

export { ApiError }; 