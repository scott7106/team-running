export enum AthleteRole {
  Athlete = 0,
  Captain = 1
}

export enum Gender {
  Female = 1,
  Male = 2,
  NS = 3
}

export enum GradeLevel {
  K = 0,
  First = 1,
  Second = 2,
  Third = 3,
  Fourth = 4,
  Fifth = 5,
  Sixth = 6,
  Seventh = 7,
  Eighth = 8,
  Ninth = 9,
  Tenth = 10,
  Eleventh = 11,
  Twelfth = 12,
  Other = 13,
  Redshirt = 20,
  Freshman = 21,
  Sophomore = 22,
  Junior = 23,
  Senior = 24
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
  gender: Gender;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  dateOfBirth: string;
  gradeLevel: GradeLevel;
  hasPhysicalOnFile: boolean;
  hasWaiverSigned: boolean;
  profile?: AthleteProfileDto;
}

export interface CreateAthleteDto {
  email?: string;
  firstName: string;
  lastName: string;
  role?: AthleteRole;
  gender: Gender;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  dateOfBirth: string;
  gradeLevel: GradeLevel;
  profile?: CreateAthleteProfileDto;
}

export interface UpdateAthleteDto {
  firstName?: string;
  lastName?: string;
  role?: AthleteRole;
  gender?: Gender;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  dateOfBirth?: string;
  gradeLevel?: GradeLevel;
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
  gender?: Gender;
  gradeLevel?: GradeLevel;
  hasPhysical?: boolean;
  hasWaiver?: boolean;
}

export interface PaginatedAthleteResponse {
  items: AthleteDto[];
  totalCount: number;
  pageNumber: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

// Helper functions for display names
export function getGenderDisplayName(gender: Gender): string {
  switch (gender) {
    case Gender.Female:
      return 'Female';
    case Gender.Male:
      return 'Male';
    case Gender.NS:
      return 'Not Specified';
    default:
      return 'Unknown';
  }
}

export function getGenderAbbreviation(gender: Gender): string {
  switch (gender) {
    case Gender.Female:
      return 'F';
    case Gender.Male:
      return 'M';
    case Gender.NS:
      return 'NS';
    default:
      return '?';
  }
}

export function getGradeLevelDisplayName(gradeLevel: GradeLevel): string {
  switch (gradeLevel) {
    case GradeLevel.K:
      return 'Kindergarten';
    case GradeLevel.First:
      return '1st Grade';
    case GradeLevel.Second:
      return '2nd Grade';
    case GradeLevel.Third:
      return '3rd Grade';
    case GradeLevel.Fourth:
      return '4th Grade';
    case GradeLevel.Fifth:
      return '5th Grade';
    case GradeLevel.Sixth:
      return '6th Grade';
    case GradeLevel.Seventh:
      return '7th Grade';
    case GradeLevel.Eighth:
      return '8th Grade';
    case GradeLevel.Ninth:
      return '9th Grade';
    case GradeLevel.Tenth:
      return '10th Grade';
    case GradeLevel.Eleventh:
      return '11th Grade';
    case GradeLevel.Twelfth:
      return '12th Grade';
    case GradeLevel.Other:
      return 'Other';
    case GradeLevel.Redshirt:
      return 'Redshirt';
    case GradeLevel.Freshman:
      return 'Freshman';
    case GradeLevel.Sophomore:
      return 'Sophomore';
    case GradeLevel.Junior:
      return 'Junior';
    case GradeLevel.Senior:
      return 'Senior';
    default:
      return 'Unknown';
  }
} 