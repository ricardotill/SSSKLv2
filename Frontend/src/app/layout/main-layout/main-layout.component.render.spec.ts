import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ConfirmationService, MessageService } from 'primeng/api';
import { MainLayoutComponent } from './main-layout.component';
import { EmbeddedService } from '../../core/services/embedded.service';

describe('MainLayoutComponent (render, mobile app)', () => {
  it('leaves the header and menu to the app', () => {
    TestBed.configureTestingModule({
      imports: [MainLayoutComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        MessageService,
        ConfirmationService, { provide: EmbeddedService, useValue: { isEmbedded: true } }],
    });
    const fixture = TestBed.createComponent(MainLayoutComponent);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;

    expect(root.querySelector('app-header')).toBeNull();
    expect(root.querySelector('app-sidebar')).toBeNull();
    expect(root.querySelector('.layout-container')?.classList).toContain('embedded');
    expect(root.querySelector('main.content-area')).not.toBeNull();
  });
});
