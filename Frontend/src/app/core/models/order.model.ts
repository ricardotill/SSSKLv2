import { ApplicationUserDto } from './application-user.model';
import { ProductDto } from './product.model';

export interface OrderSubmitDto {
  products: string[];
  users: string[];
  amount: number;
  split: boolean;
}

export interface OrderInitializeDto {
  users?: ApplicationUserDto[];
  products?: ProductDto[];
}

export interface OrderDto {
  id: string;
  createdOn: string;
  productId?: string;
  productName: string;
  userId?: string;
  userFullName: string;
  amount: number;
  paid: number;
  profilePictureUrl?: string;
}

export interface PaginatedOrders {
  items: OrderDto[];
  totalCount: number;
}
