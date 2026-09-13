export type UserRole = 'Admin' | 'Pharmacist' | 'Doctor';

export interface CreateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  confirmPassword: string;
  role: UserRole;
  licenseNumber?: string;
  specialization?: string;
  phoneNumber?: string;
}

export interface UserDto {
  id: string;
  email: string;
  fullName: string | null;
  isActive: boolean;
  roles: string[];
}
