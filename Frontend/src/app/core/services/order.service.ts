import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { OrderInitializeDto, OrderSubmitDto } from '../models/order.model';

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
}
