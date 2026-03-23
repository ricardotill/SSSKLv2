export interface ProductDto {
  id: string;
  name: string;
  description?: string | null;
  price: number;
  stock: number;
}

export interface ProductCreateDto {
  name: string;
  description?: string | null;
  price: number;
  stock: number;
}

export interface ProductUpdateDto {
  id: string;
  name: string;
  description?: string | null;
  price: number;
  stock: number;
}

export interface PaginatedProducts {
  items: ProductDto[];
  totalCount: number;
}
