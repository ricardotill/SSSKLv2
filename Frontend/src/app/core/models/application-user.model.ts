export interface ApplicationUserDto {
  id: string;
  userName: string;
  fullName: string;
  saldo: number;
  lastOrdered?: string;
  roles: string[];
  profilePictureUrl?: string;
  description?: string;
}

export interface ApplicationUserDetailedDto {
  id: string;
  userName: string;
  email?: string;
  emailConfirmed: boolean;
  phoneNumber?: string;
  phoneNumberConfirmed: boolean;
  name?: string;
  surname?: string;
  fullName: string;
  saldo: number;
  lastOrdered?: string;
  profilePictureUrl?: string;
  roles: string[];
  description?: string;
}

export interface ApplicationUserUpdateDto {
  id?: string;
  userName?: string;
  email?: string;
  emailConfirmed: boolean;
  phoneNumber?: string;
  phoneNumberConfirmed: boolean;
  name?: string;
  surname?: string;
  password?: string;
  roles?: string[];
  description?: string;
}

export interface PaginatedUsers {
  items: ApplicationUserDto[];
  totalCount: number;
}

export interface ApplicationUserSelfUpdateDto {
  phoneNumber?: string;
  name?: string;
  surname?: string;
  description?: string;
}
