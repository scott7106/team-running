export enum AthleteRole {
  Athlete = 0,
  Captain = 1
}

export interface AthleteProfileDto {
  id: string;
  athleteId: string;
  preferredEvents?: string;
  personalBests?: string;
  goals?: string;
  trainingNotes?: string;
  medicalNotes?: string;
  dietaryRestrictions?: string;
  uniformSize?: string;
  warmupRoutine?: string;
}

export interface AthleteDto {
  id: string;
  userId?: string;
  firstName: string;
  lastName: string;
  email?: string;
  role: AthleteRole;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  dateOfBirth?: string;
  grade?: string;
  hasPhysicalOnFile: boolean;
  hasWaiverSigned: boolean;
  profile?: AthleteProfileDto;
}

export interface CreateAthleteDto {
  email?: string;
  firstName: string;
  lastName: string;
  role?: AthleteRole;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  dateOfBirth?: string;
  grade?: string;
  profile?: CreateAthleteProfileDto;
}

export interface UpdateAthleteDto {
  firstName?: string;
  lastName?: string;
  role?: AthleteRole;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  dateOfBirth?: string;
  grade?: string;
  hasPhysicalOnFile?: boolean;
  hasWaiverSigned?: boolean;
  profile?: UpdateAthleteProfileDto;
}

export interface CreateAthleteProfileDto {
  preferredEvents?: string;
  personalBests?: string;
  goals?: string;
  trainingNotes?: string;
  medicalNotes?: string;
  dietaryRestrictions?: string;
  uniformSize?: string;
  warmupRoutine?: string;
}

export interface UpdateAthleteProfileDto {
  preferredEvents?: string;
  personalBests?: string;
  goals?: string;
  trainingNotes?: string;
  medicalNotes?: string;
  dietaryRestrictions?: string;
  uniformSize?: string;
  warmupRoutine?: string;
}

export interface AthleteApiParams {
  pageNumber?: number;
  pageSize?: number;
  searchQuery?: string;
  role?: AthleteRole;
  hasPhysical?: boolean;
  hasWaiver?: boolean;
}

export interface PaginatedAthleteResponse {
  items: AthleteDto[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
} 