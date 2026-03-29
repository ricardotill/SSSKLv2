import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { OrderDto, OrderInitializeDto, OrderSubmitDto, PaginatedOrders } from '../models/order.model';

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  private http = inject(HttpClient);
  private readonly API_URL = '/api/v1/Order';

  initialize(): Observable<OrderInitializeDto> {
    return this.http.get<OrderInitializeDto>(`${this.API_URL}/initialize`);
  }

  submit(order: OrderSubmitDto): Observable<void> {
    return this.http.post<void>(this.API_URL, order);
  }

  getPersonalOrders(skip: number = 0, take: number = 15): Observable<PaginatedOrders> {
    return this.http.get<PaginatedOrders>(`${this.API_URL}/personal`, {
      params: { skip, take }
    });
  }

  getLatestOrders(): Observable<OrderDto[]> {
    return this.http.get<OrderDto[]>(`${this.API_URL}/latest`);
  }

  getOrders(skip: number = 0, take: number = 15): Observable<PaginatedOrders> {
    return this.http.get<PaginatedOrders>(this.API_URL, {
      params: { skip, take }
    });
  }

  deleteOrder(id: string): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/${id}`);
  }

  exportCsv(): Observable<Blob> {
    return this.http.get(`${this.API_URL}/export/csv`, { responseType: 'blob' });
  }
}
