export interface TopUpDto {
  id: string;
  userName?: string;
  saldo: number;
  createdOn: string;
}

export interface PaginatedTopUps {
  items: TopUpDto[];
  totalCount: number;
}

export interface TopUpCreateDto {
  userName: string;
  saldo: number;
}
