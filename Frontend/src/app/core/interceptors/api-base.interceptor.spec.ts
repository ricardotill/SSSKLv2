import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { apiBaseInterceptor } from './api-base.interceptor';
import { environment } from '../../../environments/environment';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

describe('apiBaseInterceptor', () => {
  let httpClient: HttpClient;
  let httpTestingController: HttpTestingController;
  let originalApiUrl: string;

  beforeEach(() => {
    // Save original value
    originalApiUrl = environment.apiUrl;

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([apiBaseInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    httpClient = TestBed.inject(HttpClient);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
    // Restore original value
    (environment as any).apiUrl = originalApiUrl;
  });

  it('should prefix requests starting with /api/ if apiUrl is provided', () => {
    (environment as any).apiUrl = 'https://api.test.com';
    
    httpClient.get('/api/users').subscribe();

    const req = httpTestingController.expectOne('https://api.test.com/api/users');
    expect(req.request.url).toBe('https://api.test.com/api/users');
  });

  it('should prefix requests starting with /hubs/ if apiUrl is provided', () => {
    (environment as any).apiUrl = 'https://api.test.com';
    
    httpClient.get('/hubs/notifications').subscribe();

    const req = httpTestingController.expectOne('https://api.test.com/hubs/notifications');
    expect(req.request.url).toBe('https://api.test.com/hubs/notifications');
  });

  it('should handle apiUrl with a trailing slash correctly and prevent double slashes', () => {
    (environment as any).apiUrl = 'https://api.test.com/';
    
    httpClient.get('/api/users').subscribe();

    const req = httpTestingController.expectOne('https://api.test.com/api/users');
    expect(req.request.url).toBe('https://api.test.com/api/users');
  });

  it('should handle apiUrl WITHOUT a trailing slash correctly', () => {
    (environment as any).apiUrl = 'https://api.test.com';
    
    httpClient.get('/api/users').subscribe();

    const req = httpTestingController.expectOne('https://api.test.com/api/users');
    expect(req.request.url).toBe('https://api.test.com/api/users');
  });

  it('should not modify requests that do not start with /api/ or /hubs/', () => {
    (environment as any).apiUrl = 'https://api.test.com';
    
    httpClient.get('/assets/logo.png').subscribe();

    const req = httpTestingController.expectOne('/assets/logo.png');
    expect(req.request.url).toBe('/assets/logo.png');
  });

  it('should not modify external requests', () => {
    (environment as any).apiUrl = 'https://api.test.com';
    
    httpClient.get('https://google.com').subscribe();

    const req = httpTestingController.expectOne('https://google.com');
    expect(req.request.url).toBe('https://google.com');
  });

  it('should not modify requests if apiUrl is not set', () => {
    (environment as any).apiUrl = '';
    
    httpClient.get('/api/users').subscribe();

    const req = httpTestingController.expectOne('/api/users');
    expect(req.request.url).toBe('/api/users');
  });
});
