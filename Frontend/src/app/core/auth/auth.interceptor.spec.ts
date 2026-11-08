import { TestBed } from '@angular/core/testing';
import { HttpClient, HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { BehaviorSubject, Subject, throwError } from 'rxjs';
import { describe, it, expect, afterEach, vi } from 'vitest';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

describe('authInterceptor', () => {
  let httpTestingController: HttpTestingController;

  function setup(embedded: boolean) {
    const refresh$ = new Subject<{ accessToken: string; refreshToken: string }>();
    const authService = {
      isEmbedded: embedded,
      isRefreshing: false,
      refreshTokenSubject: new BehaviorSubject<string | null>(null),
      getAccessToken: vi.fn(() => 'old-token'),
      refreshTokens: vi.fn(() => refresh$.asObservable()),
      logout: vi.fn(),
    };
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authService },
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    httpTestingController = TestBed.inject(HttpTestingController);
    const http = TestBed.inject(HttpClient);
    const error = vi.fn();
    http.get('/api/v1/products').subscribe({ error });
    httpTestingController.expectOne('/api/v1/products').flush(null, { status: 401, statusText: 'Unauthorized' });
    return { authService, refresh$, error };
  }

  const httpError = (status: number) => new HttpErrorResponse({ status, url: '/api/v1/identity/refresh' });

  afterEach(() => httpTestingController.verify());

  it('retries the request with the renewed token', () => {
    const { authService, refresh$ } = setup(false);

    refresh$.next({ accessToken: 'new-token', refreshToken: 'r' });

    const retry = httpTestingController.expectOne('/api/v1/products');
    expect(retry.request.headers.get('Authorization')).toBe('Bearer new-token');
    retry.flush([]);
    expect(authService.logout).not.toHaveBeenCalled();
  });

  it('logs out when the refresh token is rejected', () => {
    const { authService, refresh$, error } = setup(true);

    refresh$.error(httpError(401));

    expect(authService.logout).toHaveBeenCalledTimes(1);
    expect(error).toHaveBeenCalled();
  });

  it('logs out on a 400 from refresh', () => {
    const { authService, refresh$ } = setup(true);

    refresh$.error(httpError(400));

    expect(authService.logout).toHaveBeenCalledTimes(1);
  });

  it('logs out when there is no refresh token', () => {
    const { authService } = setup(true);
    // The first request already started a refresh; simulate a missing token on a fresh refresh.
    authService.isRefreshing = false;
    authService.refreshTokens.mockReturnValueOnce(throwError(() => new Error('No refresh token available')));

    TestBed.inject(HttpClient).get('/api/v1/other').subscribe({ error: () => undefined });
    httpTestingController.expectOne('/api/v1/other').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(authService.logout).toHaveBeenCalledTimes(1);
  });

  it('does not sign out the mobile app when the refresh fails for a non-auth reason', () => {
    const { authService, refresh$, error } = setup(true);

    refresh$.error(httpError(0));

    expect(authService.logout).not.toHaveBeenCalled();
    expect(error).toHaveBeenCalled();
  });

  it('does not sign out the mobile app on a server error during refresh', () => {
    const { authService, refresh$ } = setup(true);

    refresh$.error(httpError(503));

    expect(authService.logout).not.toHaveBeenCalled();
  });

  it('still logs out in a normal browser when the refresh fails', () => {
    const { authService, refresh$ } = setup(false);

    refresh$.error(httpError(503));

    expect(authService.logout).toHaveBeenCalledTimes(1);
  });

  it('does not log out when the retried request fails', () => {
    const { authService, refresh$, error } = setup(true);

    refresh$.next({ accessToken: 'new-token', refreshToken: 'r' });
    httpTestingController.expectOne('/api/v1/products').flush(null, { status: 500, statusText: 'Server Error' });

    expect(authService.logout).not.toHaveBeenCalled();
    expect(error).toHaveBeenCalled();
  });
});
