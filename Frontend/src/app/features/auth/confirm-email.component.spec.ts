import { TestBed } from '@angular/core/testing';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import ConfirmEmailComponent from './confirm-email.component';

describe('ConfirmEmailComponent', () => {
  let component: ConfirmEmailComponent;
  let http: { get: ReturnType<typeof vi.fn> };

  function create(query: Record<string, string>) {
    TestBed.configureTestingModule({
      imports: [ConfirmEmailComponent],
      providers: [
        { provide: HttpClient, useValue: http },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap(query) } } },
      ],
    });
    TestBed.overrideComponent(ConfirmEmailComponent, { set: { template: '' } });
    const fixture = TestBed.createComponent(ConfirmEmailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  beforeEach(() => {
    http = { get: vi.fn(() => of('ok')) };
  });

  it('reports a link with missing data', () => {
    create({ userId: 'u1' });
    expect(http.get).not.toHaveBeenCalled();
    expect(component.isLoading()).toBe(false);
    expect(component.statusText()).toBe('Deze bevestigingslink mist gegevens.');
  });

  it('confirms the email address', () => {
    create({ userId: 'u1', code: 'abc' });
    expect(http.get).toHaveBeenCalledWith('/api/v1/identity/confirmEmail', {
      params: { userId: 'u1', code: 'abc' },
      responseType: 'text',
    });
    expect(component.statusText()).toBe('Je e-mailadres is bevestigd.');
    expect(component.successMessage()).toBeTruthy();
  });

  it('passes a changed email address along', () => {
    create({ userId: 'u1', code: 'abc', changedEmail: 'new@example.com' });
    expect(http.get.mock.calls[0][1].params).toEqual({ userId: 'u1', code: 'abc', changedEmail: 'new@example.com' });
  });

  it('reports an invalid or expired link', () => {
    http.get.mockReturnValue(throwError(() => new Error('x')));
    create({ userId: 'u1', code: 'abc' });
    expect(component.isLoading()).toBe(false);
    expect(component.errorMessage()).toContain('ongeldig of verlopen');
  });
});
