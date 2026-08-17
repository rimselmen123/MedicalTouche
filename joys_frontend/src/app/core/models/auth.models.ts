export type UUID = string;
export type ISODateTimeString = string;

export const ROLES = { Admin: 'Admin', Client: 'Client' } as const;


export interface AuthUserDto {
  userId: string;
  email: string;
  fullName?: string;
  roles: string[];
}

export interface AuthResponseDto {
  token: string;
  expiresAt: ISODateTimeString;
  userId: string;
  email: string;
  fullName?: string;
  roles: string[];
}

export interface RegisterRequestDto {
  email: string;
  password: string;
  fullName?: string;
  phone?: string;
}

export interface LoginRequestDto {
    email: string;
    password: string;
}

