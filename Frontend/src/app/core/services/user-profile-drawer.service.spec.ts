import { TestBed } from '@angular/core/testing';
import { UserProfileDrawerService } from './user-profile-drawer.service';

describe('UserProfileDrawerService', () => {
  let service: UserProfileDrawerService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(UserProfileDrawerService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should have drawerVisible as false by default', () => {
    expect(service.drawerVisible()).toBe(false);
  });

  it('should have selectedUserId as null by default', () => {
    expect(service.selectedUserId()).toBe(null);
  });

  it('should set selectedUserId and drawerVisible to true when open is called', () => {
    const testId = 'user-123';
    service.open(testId);
    expect(service.selectedUserId()).toBe(testId);
    expect(service.drawerVisible()).toBe(true);
  });

  it('should clear selectedUserId and set drawerVisible to false when close is called', () => {
    service.open('user-123');
    service.close();
    expect(service.drawerVisible()).toBe(false);
    expect(service.selectedUserId()).toBe(null);
  });
});
