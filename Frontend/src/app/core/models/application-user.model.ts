export interface ApplicationUserDto {
  id: string;
  userName: string;
  fullName: string;
  saldo: number;
  lastOrdered?: string;
  roles: string[];
}
