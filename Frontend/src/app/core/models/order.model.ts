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
