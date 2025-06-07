import { TeamsApiParams, TeamsApiResponse, CreateTeamWithNewOwnerDto, CreateTeamWithExistingOwnerDto, GlobalAdminTeamDto } from '@/types/team';
import { UsersApiParams, UsersApiResponse } from '@/types/user';

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
  
  return response.json();
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

  checkSubdomainAvailability: async (subdomain: string, excludeTeamId?: string): Promise<boolean> => {
    const params: Record<string, string> = { subdomain };
    if (excludeTeamId) {
      params.excludeTeamId = excludeTeamId;
    }
    const queryString = buildQueryString(params);
    return apiRequest<boolean>(`/api/admin/teams/subdomain-availability${queryString}`);
  },
};

export const usersApi = {
  getUsers: async (params: UsersApiParams = {}): Promise<UsersApiResponse> => {
    const queryString = buildQueryString(params as Record<string, unknown>);
    return apiRequest<UsersApiResponse>(`/api/admin/users${queryString}`);
  },
};

export { ApiError }; 