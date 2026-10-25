import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { MessageService } from 'primeng/api';
import { vi } from 'vitest';
import AdminNotificationsComponent from './admin-notifications.component';
import { ApplicationUserService } from '../../users/services/application-user.service';
import { NotificationService } from '../../../core/services/notification.service';

describe('AdminNotificationsComponent', () => {
  let component: AdminNotificationsComponent;
  let userService: { getAdminUsers: ReturnType<typeof vi.fn> };
  let notificationService: { sendCustomNotification: ReturnType<typeof vi.fn> };
  let messageService: { add: ReturnType<typeof vi.fn> };

  function create() {
    TestBed.configureTestingModule({
      imports: [AdminNotificationsComponent],
      providers: [
        { provide: ApplicationUserService, useValue: userService },
        { provide: NotificationService, useValue: notificationService },
        { provide: MessageService, useValue: messageService },
      ],
    });
    TestBed.overrideComponent(AdminNotificationsComponent, { set: { template: '' } });
    const fixture = TestBed.createComponent(AdminNotificationsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  beforeEach(() => {
    userService = {
      getAdminUsers: vi.fn(() =>
        of({ items: [{ id: 'u1', fullName: 'Jan Jansen', userName: 'jan' }, { id: 'u2', fullName: '', userName: 'piet' }] }),
      ),
    };
    notificationService = { sendCustomNotification: vi.fn(() => of(void 0)) };
    messageService = { add: vi.fn() };
  });

  it('loads the users to pick from', () => {
    create();
    expect(component.userOptions()).toEqual([
      { label: 'Jan Jansen (jan)', value: 'u1' },
      { label: 'piet (piet)', value: 'u2' },
    ]);
  });

  it('reports a failed user load', () => {
    userService.getAdminUsers.mockReturnValue(throwError(() => new Error('x')));
    create();
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
  });

  it('requires recipients unless the notification goes to everyone', () => {
    create();
    component.notificationForm.patchValue({ title: 'Hoi', message: 'Bericht', fanOut: false });
    expect(component.notificationForm.valid).toBe(false);

    component.notificationForm.patchValue({ fanOut: true });
    expect(component.notificationForm.valid).toBe(true);
  });

  it('sends a notification to everyone and resets the form', () => {
    create();
    component.sendNotification();
    expect(notificationService.sendCustomNotification).not.toHaveBeenCalled();

    component.notificationForm.patchValue({ title: 'Hoi', message: 'Bericht', userIds: ['u1'] });
    component.sendNotification();

    expect(notificationService.sendCustomNotification).toHaveBeenCalledWith({
      title: 'Hoi',
      message: 'Bericht',
      linkUri: undefined,
      fanOut: true,
      userIds: undefined,
    });
    expect(component.sending()).toBe(false);
    expect(component.notificationForm.getRawValue().title).toBe('');
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
  });

  it('sends to selected users and reports failures', () => {
    notificationService.sendCustomNotification.mockReturnValue(throwError(() => new Error('x')));
    create();
    component.notificationForm.patchValue({
      title: 'Hoi',
      message: 'Bericht',
      linkUri: '/events',
      fanOut: false,
      userIds: ['u1'],
    });
    component.sendNotification();

    expect(notificationService.sendCustomNotification).toHaveBeenCalledWith(
      expect.objectContaining({ linkUri: '/events', fanOut: false, userIds: ['u1'] }),
    );
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
  });
});
