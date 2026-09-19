import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { FormBuilder } from '@angular/forms';
import { MessageService, ConfirmationService } from 'primeng/api';
import UsersComponent from './users.component';
import { ApplicationUserService } from '../../users/services/application-user.service';
import { RoleService } from '../services/role.service';

describe('UsersComponent', () => {
  let userService: {
    getLatestRecalculateAllStatsStatus: ReturnType<typeof vi.fn>;
    getRecalculateAllStatsStatus: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    userService = {
      getLatestRecalculateAllStatsStatus: vi.fn(),
      getRecalculateAllStatsStatus: vi.fn()
    };

    TestBed.configureTestingModule({
      imports: [UsersComponent],
      providers: [
        FormBuilder,
        { provide: ApplicationUserService, useValue: userService },
        { provide: RoleService, useValue: { getAdminRoles: vi.fn(() => of([])) } },
        { provide: MessageService, useValue: { add: vi.fn() } },
        { provide: ConfirmationService, useValue: { confirm: vi.fn() } }
      ]
    });
    TestBed.overrideComponent(UsersComponent, { set: { template: '' } });
  });

  it('does not restore the latest recalculation job when it is already completed', () => {
    const fixture = TestBed.createComponent(UsersComponent);
    const component = fixture.componentInstance;
    userService.getLatestRecalculateAllStatsStatus.mockReturnValue(of({
      id: 'job-1',
      status: 'Completed',
      totalUsers: 10,
      processedUsers: 10,
      failedUsers: 0,
      startedAt: '2026-09-19T00:00:00Z',
      completedAt: '2026-09-19T00:01:00Z',
      startedByUserId: 'admin'
    }));

    (component as any).checkForRunningRecalculateAllJob();

    expect(component.recalculateAllJob()).toBeNull();
    expect(userService.getRecalculateAllStatsStatus).not.toHaveBeenCalled();
  });

  it('restores and polls the latest recalculation job when it is still running', () => {
    const fixture = TestBed.createComponent(UsersComponent);
    const component = fixture.componentInstance;
    userService.getLatestRecalculateAllStatsStatus.mockReturnValue(of({
      id: 'job-1',
      status: 'Running',
      totalUsers: 10,
      processedUsers: 2,
      failedUsers: 0,
      startedAt: '2026-09-19T00:00:00Z',
      startedByUserId: 'admin'
    }));
    userService.getRecalculateAllStatsStatus.mockReturnValue(of({
      id: 'job-1',
      status: 'Completed',
      totalUsers: 10,
      processedUsers: 10,
      failedUsers: 0,
      startedAt: '2026-09-19T00:00:00Z',
      completedAt: '2026-09-19T00:01:00Z',
      startedByUserId: 'admin'
    }));

    (component as any).checkForRunningRecalculateAllJob();

    expect(component.recalculateAllJob()?.status).toBe('Running');
    component.ngOnDestroy();
  });
});
