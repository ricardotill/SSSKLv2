import { TestBed } from '@angular/core/testing';
import { Type } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { TURNSTILE_HEADER, TurnstileService } from '../../core/services/turnstile.service';
import ForgotPasswordComponent from './forgot-password.component';
import ResendConfirmationEmailComponent from './resend-confirmation-email.component';
import RegisterComponent from './register.component';
import LoginComponent from './login.component';
import { WebAuthnService } from './services/webauthn.service';
import { of, throwError } from 'rxjs';

const turnstileRejection = { status: 403, statusText: 'Forbidden' };
const turnstileBody = { code: 'turnstile_failed' };

function setup<T>(component: Type<T>) {
  TestBed.configureTestingModule({
    imports: [component],
    providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
  });
  const turnstile = TestBed.inject(TurnstileService);
  (turnstile as any).configRequested = true;
  turnstile.config.set({ enabled: true, siteKey: 'site-key' });
  // The widget is rendered for real but never gets a script; tests set the token directly
  vi.spyOn(turnstile, 'load').mockReturnValue(new Promise(() => {}));
  const fixture = TestBed.createComponent(component);
  fixture.detectChanges();
  const http = TestBed.inject(HttpTestingController);
  // The branding header asks for the app version
  http.match('/api/v1/public/version').forEach(r => r.flush({ version: '1.0.0' }));
  return { component: fixture.componentInstance as any, http, turnstile };
}

describe('Turnstile on auth forms', () => {
  let http: HttpTestingController;

  afterEach(() => http.verify());

  describe.each([
    { name: 'forgot password', type: ForgotPasswordComponent, form: 'forgotPasswordForm', url: '/api/v1/identity/forgotPassword', values: { email: 'a@b.nl' } },
    { name: 'resend confirmation', type: ResendConfirmationEmailComponent, form: 'resendForm', url: '/api/v1/identity/resendConfirmationEmail', values: { email: 'a@b.nl' } }
  ])('$name', ({ type, form, url, values }) => {
    let component: any;
    let turnstile: TurnstileService;

    beforeEach(() => {
      ({ component, http, turnstile } = setup(type as Type<unknown>));
      component[form].setValue(values);
    });

    it('blocks submit until a token is present and then sends it', () => {
      expect(turnstile.blocksSubmit(component.turnstileToken())).toBe(true);
      component.turnstileToken.set('tok');

      component.onSubmit();
      const req = http.expectOne(url);
      expect(req.request.headers.get(TURNSTILE_HEADER)).toBe('tok');
      req.flush({});

      expect(component.successMessage()).not.toBe('');
      expect(component.isLoading()).toBe(false);
    });

    it('shows the Turnstile message when the API rejects the check', () => {
      component.turnstileToken.set('tok');
      component.onSubmit();
      http.expectOne(url).flush(turnstileBody, turnstileRejection);

      expect(component.errorMessage()).toBe(component.lang.t().turnstile_failed);
    });

    it('shows the generic message on other errors', () => {
      component.turnstileToken.set('tok');
      component.onSubmit();
      http.expectOne(url).flush({}, { status: 500, statusText: 'Error' });

      expect(component.errorMessage()).not.toBe(component.lang.t().turnstile_failed);
      expect(component.errorMessage()).not.toBe('');
    });
  });

  describe('register', () => {
    let component: any;

    beforeEach(() => {
      ({ component, http } = setup(RegisterComponent));
      component.registerForm.patchValue({ userName: 'kim', email: 'kim@b.nl', name: 'Kim', surname: 'B', password: 'secret1' });
      component.turnstileToken.set('tok');
    });

    it('sends the token with the registration', () => {
      component.onSubmit();
      const req = http.expectOne('/api/v1/identity/register');
      expect(req.request.headers.get(TURNSTILE_HEADER)).toBe('tok');
      req.flush({});

      expect(component.successMessage()).not.toBe('');
    });

    it('shows validation errors from the API', () => {
      component.onSubmit();
      http.expectOne('/api/v1/identity/register').flush({ errors: { Email: ['E-mail is al in gebruik'] } }, { status: 400, statusText: 'Bad Request' });

      expect(component.errorMessage()).toBe('E-mail is al in gebruik');
    });

    it('shows the Turnstile message when the API rejects the check', () => {
      component.onSubmit();
      http.expectOne('/api/v1/identity/register').flush(turnstileBody, turnstileRejection);

      expect(component.errorMessage()).toBe(component.lang.t().turnstile_failed);
    });
  });

  describe('login', () => {
    let component: any;

    beforeEach(() => {
      ({ component, http } = setup(LoginComponent));
      component.loginForm.patchValue({ userName: 'kim', password: 'secret1' });
      component.turnstileToken.set('tok');
    });

    it('sends the token and treats a Turnstile 403 as a failed check, not a 2FA prompt', () => {
      component.onSubmit();
      const req = http.expectOne('/api/v1/identity/login');
      expect(req.request.headers.get(TURNSTILE_HEADER)).toBe('tok');
      req.flush(turnstileBody, turnstileRejection);

      expect(component.requires2FA()).toBe(false);
      expect(component.errorMessage()).toBe(component.lang.t().turnstile_failed);
    });

    it('navigates after a successful login and resets the widget token', () => {
      const router = TestBed.inject(Router);
      const navigate = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);

      component.onSubmit();
      http.expectOne('/api/v1/identity/login').flush({ accessToken: 'a', refreshToken: 'r', expiresIn: 3600, tokenType: 'Bearer' });
      http.match(() => true).forEach(r => r.flush({}));

      expect(navigate).toHaveBeenCalledWith('/pos');
    });

    it('asks for a user name before a passkey login', () => {
      component.loginForm.patchValue({ userName: '' });
      component.onPasskeyLogin();
      expect(component.errorMessage()).toBe(component.lang.t().enter_username_first);
    });

    it('navigates after a passkey login', () => {
      const router = TestBed.inject(Router);
      const navigate = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
      vi.spyOn(TestBed.inject(WebAuthnService), 'login').mockReturnValue(of({}));

      component.onPasskeyLogin();

      expect(component.isLoading()).toBe(false);
      expect(navigate).toHaveBeenCalledWith('/pos');
    });

    it('shows passkey login errors', () => {
      vi.spyOn(console, 'error').mockImplementation(() => undefined);
      vi.spyOn(TestBed.inject(WebAuthnService), 'login').mockReturnValue(throwError(() => new Error('Geannuleerd')));

      component.onPasskeyLogin();

      expect(component.isLoading()).toBe(false);
      expect(component.errorMessage()).toContain('Geannuleerd');
    });

    it('still asks for a 2FA code on the two-factor 403', () => {
      component.onSubmit();
      http.expectOne('/api/v1/identity/login').flush({ detail: 'RequiresTwoFactor' }, turnstileRejection);

      expect(component.requires2FA()).toBe(true);
    });
  });
});
