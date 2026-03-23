export interface ApplicationUserDto {
  id: string;
  userName: string;
  fullName: string;
  saldo: number;
  lastOrdered?: string;
  roles: string[];
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
  profilePictureBase64?: string;
  roles: string[];
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
}

export interface PaginatedUsers {
  items: ApplicationUserDto[];
  totalCount: number;
}
