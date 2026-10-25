import { TestBed } from '@angular/core/testing';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import ResetPasswordComponent from './reset-password.component';

describe('ResetPasswordComponent', () => {
  let component: ResetPasswordComponent;
  let http: { post: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };

  function create(query: Record<string, string>) {
    TestBed.configureTestingModule({
      imports: [ResetPasswordComponent],
      providers: [
        { provide: HttpClient, useValue: http },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap(query) } } },
      ],
    });
    TestBed.overrideComponent(ResetPasswordComponent, { set: { template: '' } });
    component = TestBed.createComponent(ResetPasswordComponent).componentInstance;
  }

  function fill(newPassword = 'Geheim123!', confirmPassword = newPassword) {
    component.resetPasswordForm.patchValue({ newPassword, confirmPassword });
  }

  beforeEach(() => {
    http = { post: vi.fn(() => of({})) };
    router = { navigate: vi.fn() };
  });

  afterEach(() => vi.useRealTimers());

  it('reports a link without a reset code', () => {
    create({ email: 'jan@example.com' });
    fill();
    component.onSubmit();
    expect(component.errorMessage()).toContain('resetcode');
    expect(http.post).not.toHaveBeenCalled();
  });

  it('requires matching passwords', () => {
    create({ email: 'jan@example.com', code: 'abc' });
    fill('Geheim123!', 'Anders123!');
    expect(component.resetPasswordForm.errors).toEqual({ passwordMismatch: true });
    component.onSubmit();
    expect(http.post).not.toHaveBeenCalled();
  });

  it('resets the password and redirects to the login', () => {
    vi.useFakeTimers();
    create({ email: 'jan@example.com', resetCode: 'abc' });
    fill();
    component.onSubmit();

    expect(http.post).toHaveBeenCalledWith('/api/v1/identity/resetPassword', {
      email: 'jan@example.com',
      resetCode: 'abc',
      newPassword: 'Geheim123!',
    });
    expect(component.isLoading()).toBe(false);
    expect(component.successMessage()).toBeTruthy();

    vi.advanceTimersByTime(1800);
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('shows the first validation error from the server', () => {
    http.post.mockReturnValue(throwError(() => ({ error: { errors: { PasswordTooShort: ['Te kort'] } } })));
    create({ email: 'jan@example.com', code: 'abc' });
    fill();
    component.onSubmit();
    expect(component.errorMessage()).toBe('Te kort');
  });

  it('shows a generic error otherwise', () => {
    http.post.mockReturnValue(throwError(() => ({ error: null })));
    create({ email: 'jan@example.com', code: 'abc' });
    fill();
    component.onSubmit();
    expect(component.errorMessage()).toContain('kon niet worden bijgewerkt');
  });
});
