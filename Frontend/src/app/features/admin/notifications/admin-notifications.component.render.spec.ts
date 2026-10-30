import { TestBed, ComponentFixture } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { MessageService } from 'primeng/api';
import { vi } from 'vitest';
import AdminNotificationsComponent from './admin-notifications.component';
import { ApplicationUserService } from '../../users/services/application-user.service';
import { NotificationService } from '../../../core/services/notification.service';

describe('AdminNotificationsComponent (rendered)', () => {
  let fixture: ComponentFixture<AdminNotificationsComponent>;
  let component: AdminNotificationsComponent;
  let notificationService: { sendCustomNotification: ReturnType<typeof vi.fn> };

  const el = () => fixture.nativeElement as HTMLElement;

  beforeEach(() => {
    notificationService = { sendCustomNotification: vi.fn(() => of(void 0)) };
    TestBed.configureTestingModule({
      imports: [AdminNotificationsComponent, NoopAnimationsModule],
      providers: [
        {
          provide: ApplicationUserService,
          useValue: { getAdminUsers: vi.fn(() => of({ items: [{ id: 'u1', fullName: 'Jan Jansen', userName: 'jan' }] })) },
        },
        { provide: NotificationService, useValue: notificationService },
        { provide: MessageService, useValue: { add: vi.fn() } },
      ],
    });
    fixture = TestBed.createComponent(AdminNotificationsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('shows validation messages for touched invalid fields', () => {
    component.notificationForm.patchValue({ linkUri: 'events' });
    component.notificationForm.markAllAsTouched();
    fixture.detectChanges();

    expect(el().querySelectorAll('small.text-red-500')).toHaveLength(3);
    expect(el().textContent).toContain("Moet beginnen met '/'");
  });

  it('shows the user picker when not sending to everyone', () => {
    expect(el().querySelector('p-multiselect')).toBeNull();

    component.notificationForm.patchValue({ fanOut: false });
    component.notificationForm.get('userIds')!.markAsTouched();
    fixture.detectChanges();

    expect(el().querySelector('p-multiselect')).toBeTruthy();
    expect(el().querySelectorAll('small.text-red-500')).toHaveLength(1);
  });

  it('sends the notification when the form is submitted', () => {
    component.notificationForm.patchValue({ title: 'Hoi', message: 'Bericht', linkUri: '/events' });
    fixture.detectChanges();

    el().querySelector('form')!.dispatchEvent(new Event('submit'));

    expect(notificationService.sendCustomNotification).toHaveBeenCalled();
  });
});
