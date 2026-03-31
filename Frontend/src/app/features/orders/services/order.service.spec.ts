import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { OrderService } from './order.service';
import { PaginatedOrders } from '../../../core/models/order.model';

describe('OrderService', () => {
  let service: OrderService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        OrderService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(OrderService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should submit order', () => {
    const orderData = { productId: '1', quantity: 1 };
    service.submit(orderData as any).subscribe();

    const req = httpMock.expectOne('/api/v1/Order');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(orderData);
    req.flush(null);
  });

  it('should fetch personal orders', () => {
    const mockResponse: PaginatedOrders = { items: [], totalCount: 0 };
    service.getPersonalOrders().subscribe();
    const req = httpMock.expectOne((req) => req.url.startsWith('/api/v1/Order/personal'));
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });
});
