import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ProductDto, ProductCreateDto, ProductUpdateDto, PaginatedProducts } from '../models/product.model';

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/product';

  getProducts(skip: number = 0, take: number = 1000, all: boolean = false): Observable<PaginatedProducts | ProductDto[]> {
    return this.http.get<PaginatedProducts | ProductDto[]>(this.baseUrl, {
      params: { skip, take, all: all.toString() }
    });
  }

  getProduct(id: string): Observable<ProductDto> {
    return this.http.get<ProductDto>(`${this.baseUrl}/${id}`);
  }

  createProduct(dto: ProductCreateDto): Observable<ProductDto> {
    return this.http.post<ProductDto>(this.baseUrl, dto);
  }

  updateProduct(id: string, dto: ProductUpdateDto): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, dto);
  }

  deleteProduct(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
