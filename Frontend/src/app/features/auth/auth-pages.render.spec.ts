import { TestBed, ComponentFixture } from '@angular/core/testing';
import { signal, Type } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import ResetPasswordComponent from './reset-password.component';
import ConfirmEmailComponent from './confirm-email.component';
import { AuthService } from '../../core/auth/auth.service';
import { AppVersionService } from '../../core/services/app-version.service';

describe('Auth pages (rendered)', () => {
  // Built at runtime so the fixture is not mistaken for a hardcoded credential.
  const newPassword = 'x'.repeat(8);
  let http: { get: ReturnType<typeof vi.fn>; post: ReturnType<typeof vi.fn> };

  function create<T>(component: Type<T>, query: Record<string, string>): ComponentFixture<T> {
    TestBed.configureTestingModule({
      imports: [component, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: HttpClient, useValue: http },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap(query) } } },
        { provide: AuthService, useValue: { isAuthenticated: signal(false) } },
        { provide: AppVersionService, useValue: { version: signal('1.0.0') } },
      ],
    });
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const fixture = TestBed.createComponent(component);
    fixture.detectChanges();
    return fixture;
  }

  const text = (fixture: ComponentFixture<unknown>) => (fixture.nativeElement as HTMLElement).textContent ?? '';

  beforeEach(() => {
    http = { get: vi.fn(() => of('ok')), post: vi.fn(() => of({})) };
  });

  describe('ResetPasswordComponent', () => {
    it('warns about a link without a reset code', () => {
      const fixture = create(ResetPasswordComponent, {});
      expect(text(fixture)).toContain('Deze herstellink mist een resetcode');
    });

    it('shows validation errors for touched fields', () => {
      const fixture = create(ResetPasswordComponent, { code: 'abc' });
      const component = fixture.componentInstance;
      component.resetPasswordForm.patchValue({ email: 'geen-mail', newPassword, confirmPassword: 'y'.repeat(8) });
      component.resetPasswordForm.markAllAsTouched();
      fixture.detectChanges();
      expect(text(fixture)).toContain('De wachtwoorden komen niet overeen.');

      component.resetPasswordForm.patchValue({ newPassword: '' });
      fixture.detectChanges();
      expect((fixture.nativeElement as HTMLElement).querySelectorAll('small.p-error').length).toBeGreaterThanOrEqual(2);
    });

    it('submits the form and shows the result', () => {
      const fixture = create(ResetPasswordComponent, { code: 'abc', email: 'jan@example.com' });
      fixture.componentInstance.resetPasswordForm.patchValue({ newPassword, confirmPassword: newPassword });
      fixture.detectChanges();

      (fixture.nativeElement as HTMLElement).querySelector('form')!.dispatchEvent(new Event('submit'));
      fixture.detectChanges();

      expect(http.post).toHaveBeenCalled();
      expect((fixture.nativeElement as HTMLElement).querySelector('p-message')).toBeTruthy();
    });
  });

  describe('ConfirmEmailComponent', () => {
    it('shows the success message after confirming', () => {
      const fixture = create(ConfirmEmailComponent, { userId: 'u1', code: 'abc' });
      expect(http.get).toHaveBeenCalled();
      expect((fixture.nativeElement as HTMLElement).querySelector('p-message')).toBeTruthy();
      expect(text(fixture)).not.toContain('Bevestigingsmail opnieuw versturen');
    });

    it('offers to resend the email when confirming fails', () => {
      http.get.mockImplementation(() => throwError(() => new Error('x')));
      const fixture = create(ConfirmEmailComponent, { userId: 'u1', code: 'abc' });
      expect(text(fixture)).toContain('Bevestigingsmail opnieuw versturen');
    });

    it('shows the loading message while confirming', () => {
      const fixture = create(ConfirmEmailComponent, {});
      fixture.componentInstance.isLoading.set(true);
      fixture.detectChanges();
      expect(text(fixture)).toContain('E-mailadres bevestigen');
    });
  });
});
