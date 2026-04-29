import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { AppVersionService } from '../../../core/services/app-version.service';
import { BrandingComponent } from './branding.component';

describe('BrandingComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BrandingComponent],
      providers: [
        { provide: AuthService, useValue: { isAuthenticated: signal(false) } },
        { provide: AppVersionService, useValue: { version: signal('3.8.3') } },
        { provide: Router, useValue: { navigate: () => Promise.resolve(true) } }
      ]
    }).compileComponents();
  });

  it('should render the version from the app version service', () => {
    const fixture = TestBed.createComponent(BrandingComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('v3.8.3');
  });

  it('should hide the version when showVersion is false', () => {
    const fixture = TestBed.createComponent(BrandingComponent);
    fixture.componentRef.setInput('showVersion', false);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('v3.8.3');
  });
});
