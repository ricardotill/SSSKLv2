import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router, UrlTree } from '@angular/router';
import { firstValueFrom, Observable, of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { quotesGuard } from './quotes.guard';
import { AuthService } from './auth.service';

describe('quotesGuard', () => {
  let http: { get: ReturnType<typeof vi.fn> };
  const homeTree = {} as UrlTree;

  function run(): Promise<boolean | UrlTree> {
    TestBed.configureTestingModule({
      providers: [
        { provide: HttpClient, useValue: http },
        { provide: Router, useValue: { createUrlTree: vi.fn(() => homeTree) } },
        { provide: AuthService, useValue: { isInitialized: signal(true) } },
      ],
    });
    const result = TestBed.runInInjectionContext(() => quotesGuard({} as any, {} as any)) as Observable<boolean | UrlTree>;
    return firstValueFrom(result);
  }

  beforeEach(() => {
    http = { get: vi.fn(() => of({})) };
  });

  it('allows access when the quotes API responds', async () => {
    await expect(run()).resolves.toBe(true);
    expect(http.get).toHaveBeenCalledWith('/api/v1/quote', { params: { skip: '0', take: '1' } });
  });

  it('redirects home when access is forbidden', async () => {
    http.get.mockReturnValue(throwError(() => ({ status: 403 })));
    await expect(run()).resolves.toBe(homeTree);
  });

  it('allows access on other errors', async () => {
    http.get.mockReturnValue(throwError(() => ({ status: 500 })));
    await expect(run()).resolves.toBe(true);
  });
});
