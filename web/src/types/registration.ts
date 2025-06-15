export enum RegistrationStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2,
  Waitlisted = 3
}

export interface AthleteRegistrationDto {
  id?: string;
  firstName: string;
  lastName: string;
  birthdate: string; // ISO date string
  gradeLevel: string;
  createdOn?: string;
}

export interface TeamRegistrationDto {
  id: string;
  teamId: string;
  teamName: string;
  email: string;
  firstName: string;
  lastName: string;
  emergencyContactName: string;
  emergencyContactPhone: string;
  codeOfConductAccepted: boolean;
  codeOfConductAcceptedOn: string;
  status: RegistrationStatus;
  createdOn: string;
  modifiedOn?: string;
  athletes: AthleteRegistrationDto[];
}

export interface SubmitRegistrationDto {
  email: string;
  firstName: string;
  lastName: string;
  emergencyContactName: string;
  emergencyContactPhone: string;
  codeOfConductAccepted: boolean;
  registrationPasscode: string;
  athletes: AthleteRegistrationDto[];
}

export interface UpdateRegistrationStatusDto {
  status: RegistrationStatus;
}

export interface TeamRegistrationWindowDto {
  id: string;
  teamId: string;
  teamName: string;
  startDate: string;
  endDate: string;
  maxRegistrations: number;
  registrationPasscode: string;
  isActive: boolean;
  createdOn: string;
  modifiedOn?: string;
}

export interface CreateRegistrationWindowDto {
  startDate: string;
  endDate: string;
  maxRegistrations: number;
  registrationPasscode: string;
}

export interface UpdateRegistrationWindowDto {
  startDate: string;
  endDate: string;
  maxRegistrations: number;
  registrationPasscode: string;
}

// User account registration (different from team membership registration)
export interface UserRegistrationDto {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

// Team creation registration
export interface TeamCreationDto {
  teamName: string;
  subdomain: string;
  tier: number; // TeamTier enum
  primaryColor?: string;
  secondaryColor?: string;
  ownerEmail: string;
  ownerFirstName: string;
  ownerLastName: string;
  ownerPassword: string;
} 