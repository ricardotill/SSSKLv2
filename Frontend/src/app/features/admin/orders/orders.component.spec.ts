import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { MessageService, ConfirmationService } from 'primeng/api';
import OrdersComponent from './orders.component';
import { OrderService } from '../../orders/services/order.service';
import { AuthService } from '../../../core/auth/auth.service';

describe('OrdersComponent', () => {
  let httpMock: HttpTestingController;
  let messageService: { add: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    vi.useFakeTimers();
    messageService = { add: vi.fn() };

    TestBed.configureTestingModule({
      imports: [OrdersComponent],
      providers: [
        OrderService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: MessageService, useValue: messageService },
        { provide: ConfirmationService, useValue: { confirm: vi.fn() } },
        { provide: AuthService, useValue: { refreshCurrentUser: vi.fn() } }
      ]
    });
    TestBed.overrideComponent(OrdersComponent, { set: { template: '' } });
    httpMock = TestBed.inject(HttpTestingController);

    Object.defineProperty(window.URL, 'createObjectURL', {
      configurable: true,
      value: vi.fn(() => 'blob:orders-csv')
    });
    Object.defineProperty(window.URL, 'revokeObjectURL', {
      configurable: true,
      value: vi.fn()
    });
  });

  afterEach(() => {
    vi.useRealTimers();
    httpMock.verify();
  });

  it('starts CSV export, polls until completed, and downloads the generated file', () => {
    const fixture = TestBed.createComponent(OrdersComponent);
    const component = fixture.componentInstance;

    (component as any).executeExport();

    const startReq = httpMock.expectOne('/api/v1/Order/export/csv');
    expect(startReq.request.method).toBe('POST');
    startReq.flush({
      id: 'job-1',
      status: 'Pending',
      startedAt: '2026-09-19T00:00:00Z',
      startedByUserId: 'admin',
      fileName: 'Orders_Export_2026-09-19.csv'
    });

    vi.advanceTimersByTime(3000);

    const statusReq = httpMock.expectOne('/api/v1/Order/export/csv/job-1');
    expect(statusReq.request.method).toBe('GET');
    statusReq.flush({
      id: 'job-1',
      status: 'Completed',
      startedAt: '2026-09-19T00:00:00Z',
      completedAt: '2026-09-19T00:00:01Z',
      startedByUserId: 'admin',
      fileName: 'Orders_Export_2026-09-19.csv'
    });

    const downloadReq = httpMock.expectOne('/api/v1/Order/export/csv/job-1/download');
    expect(downloadReq.request.method).toBe('GET');
    expect(downloadReq.request.responseType).toBe('blob');
    downloadReq.flush(new Blob(['OrderId']));

    expect(component.exporting()).toBe(false);
    expect(window.URL.createObjectURL).toHaveBeenCalled();
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
  });
});
