export enum UserRole {
  User = 0,
  Admin = 1,
}

export interface AuthenticatedUser {
  id: string;
  fullName: string;
  email: string;
  role: number;
}

export interface AuthResponse {
  accessToken: string;
  expiresAt: string;
  user: AuthenticatedUser;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest extends LoginRequest {
  fullName: string;
}
