export enum UserStatus {
  Active = 0,
  Inactive = 1,
  Suspended = 2
}

export interface UserTeamSummaryDto {
  userTeamId: string;
  teamId: string;
  teamName: string;
  teamSubdomain: string;
  role: string;
  memberType: string;
  isActive: boolean;
  joinedOn: string;
}

export interface GlobalAdminUserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  displayName: string;
  phoneNumber?: string;
  status: UserStatus;
  isActive: boolean;
  isDeleted: boolean;
  
  // Application role information
  applicationRoles: string[];
  isGlobalAdmin: boolean;
  
  // Account status information
  emailConfirmed: boolean;
  phoneNumberConfirmed: boolean;
  twoFactorEnabled: boolean;
  lockoutEnabled: boolean;
  lockoutEnd?: string;
  accessFailedCount: number;
  
  // Activity information
  lastLoginOn?: string;
  defaultTeamId?: string;
  defaultTeamName?: string;
  
  // Audit information
  createdOn: string;
  createdBy?: string;
  createdByName?: string;
  modifiedOn?: string;
  modifiedBy?: string;
  modifiedByName?: string;
  deletedOn?: string;
  deletedBy?: string;
  deletedByName?: string;
  
  // Team memberships summary
  teamCount: number;
  teamMemberships: UserTeamSummaryDto[];
}

export interface UsersApiParams {
  pageNumber?: number;
  pageSize?: number;
  searchQuery?: string;
  status?: UserStatus;
  isActive?: boolean;
  isDeleted?: boolean;
}

export interface UsersApiResponse {
  items: GlobalAdminUserDto[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface GlobalAdminResetPasswordDto {
  newPassword?: string;
  requirePasswordChange: boolean;
  sendPasswordByEmail: boolean;
  sendPasswordBySms: boolean;
}

export interface PasswordResetResultDto {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  temporaryPassword?: string;
  requirePasswordChange: boolean;
  passwordSentByEmail: boolean;
  passwordSentBySms: boolean;
  resetTimestamp: string;
} 