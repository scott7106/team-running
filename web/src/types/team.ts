export enum TeamStatus {
  Active = 0,
  Suspended = 1,
  Expired = 2,
  PendingSetup = 3
}

export enum TeamTier {
  Free = 0,
  Standard = 1,
  Premium = 2
}

export interface GlobalAdminTeamDto {
  id: string;
  name: string;
  subdomain: string;
  logoUrl?: string;
  primaryColor: string;
  secondaryColor: string;
  status: TeamStatus;
  tier: TeamTier;
  expiresOn?: string;
  createdOn: string;
  modifiedOn?: string;
  ownerId: string;
  ownerEmail: string;
  ownerFirstName: string;
  ownerLastName: string;
  ownerDisplayName: string;
  memberCount: number;
  athleteCount: number;
  adminCount: number;
  hasPendingOwnershipTransfer: boolean;
}

export interface CreateTeamWithNewOwnerDto {
  name: string;
  subdomain: string;
  ownerEmail: string;
  ownerFirstName: string;
  ownerLastName: string;
  ownerPassword: string;
  tier?: TeamTier;
  primaryColor?: string;
  secondaryColor?: string;
  expiresOn?: string;
}

export interface CreateTeamWithExistingOwnerDto {
  name: string;
  subdomain: string;
  ownerId: string;
  tier?: TeamTier;
  primaryColor?: string;
  secondaryColor?: string;
  expiresOn?: string;
}

export interface GlobalAdminUpdateTeamDto {
  name?: string;
  subdomain?: string;
  status?: TeamStatus;
  tier?: TeamTier;
  expiresOn?: string;
  primaryColor?: string;
  secondaryColor?: string;
  logoUrl?: string;
}

export interface PaginatedList<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export type TeamsApiResponse = PaginatedList<GlobalAdminTeamDto>;

export interface TeamsApiParams {
  pageNumber?: number;
  pageSize?: number;
  searchQuery?: string;
  status?: TeamStatus;
  tier?: TeamTier;
} 