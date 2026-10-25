import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { MessageService } from 'primeng/api';
import { vi } from 'vitest';
import EventEditComponent from './event-edit.component';
import { EventService } from '../services/event.service';
import { AuthService } from '../../../core/auth/auth.service';
import { RoleService } from '../../admin/services/role.service';

describe('EventEditComponent', () => {
  let component: EventEditComponent;
  let eventService: any;
  let roleService: { getAllRoles: ReturnType<typeof vi.fn> };
  let messageService: { add: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };
  let currentUser: ReturnType<typeof signal<any>>;

  const evt = {
    id: 'e1',
    title: 'Kamp',
    description: 'Leuk',
    imageUrl: '/img/e1',
    startDateTime: '2026-07-01T10:00:00Z',
    endDateTime: '2026-07-01T12:00:00Z',
    creatorName: 'jan',
    locationName: 'Clubhuis',
    latitude: 52.1,
    longitude: 5.1,
    requiredRoles: ['Leiding'],
  };

  function create(id: string | null = null) {
    TestBed.configureTestingModule({
      imports: [EventEditComponent],
      providers: [
        { provide: EventService, useValue: eventService },
        { provide: RoleService, useValue: roleService },
        { provide: AuthService, useValue: { currentUser } },
        { provide: Router, useValue: router },
        { provide: MessageService, useValue: messageService },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => id } } } },
      ],
    });
    TestBed.overrideComponent(EventEditComponent, { set: { template: '' } });
    const fixture = TestBed.createComponent(EventEditComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  beforeEach(() => {
    eventService = {
      getEvent: vi.fn(() => of(evt)),
      createEvent: vi.fn(() => of({})),
      updateEvent: vi.fn(() => of({})),
    };
    roleService = { getAllRoles: vi.fn(() => of([{ name: 'Leiding' }])) };
    messageService = { add: vi.fn() };
    router = { navigate: vi.fn() };
    currentUser = signal<any>({ userName: 'jan', roles: [] });
  });

  afterEach(() => vi.useRealTimers());

  it('starts empty for a new event and loads the roles', () => {
    create();
    expect(component.isEdit).toBe(false);
    expect(eventService.getEvent).not.toHaveBeenCalled();
    expect(component.availableRoles()).toEqual([{ name: 'Leiding' }]);
  });

  it('logs when the roles cannot be loaded', () => {
    const error = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    roleService.getAllRoles.mockReturnValue(throwError(() => new Error('x')));
    create();
    expect(error).toHaveBeenCalledWith('Failed to load roles');
  });

  it('loads an event the creator may edit', () => {
    create('e1');
    expect(component.isEdit).toBe(true);
    expect(component.eventId).toBe('e1');
    expect(component.currentImageUri()).toBe('/img/e1');
    expect(component.eventForm.value.title).toBe('Kamp');
    expect(component.eventForm.value.requiredRoles).toEqual(['Leiding']);
  });

  it('sends other users back to the events list', () => {
    vi.useFakeTimers();
    currentUser.set({ userName: 'piet', roles: [] });
    create('e1');
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ summary: 'Toegang geweigerd' }));
    vi.advanceTimersByTime(1500);
    expect(router.navigate).toHaveBeenCalledWith(['/events']);
  });

  it('lets admins edit any event and reports load failures', () => {
    currentUser.set({ userName: 'admin', roles: ['Admin'] });
    create('e1');
    expect(component.eventForm.value.title).toBe('Kamp');

    eventService.getEvent.mockReturnValue(throwError(() => new Error('x')));
    component.loadEvent('e1');
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'Kan evenement niet laden' }));
  });

  it('defaults the end time to one hour after the start', () => {
    create();
    const start = new Date('2026-07-01T10:00:00Z');
    component.eventForm.patchValue({ startDateTime: start });
    expect(component.eventForm.value.endDateTime).toEqual(new Date('2026-07-01T11:00:00Z'));

    component.eventForm.patchValue({ endDateTime: '2026-07-01T09:00:00Z' });
    component.eventForm.patchValue({ startDateTime: '2026-07-01T10:00:00Z' });
    expect(component.eventForm.value.endDateTime).toEqual(new Date('2026-07-01T11:00:00Z'));

    component.eventForm.patchValue({ endDateTime: new Date('2026-07-01T15:00:00Z') });
    component.eventForm.patchValue({ startDateTime: start });
    expect(component.eventForm.value.endDateTime).toEqual(new Date('2026-07-01T15:00:00Z'));

    component.eventForm.patchValue({ startDateTime: 'not a date' });
    component.eventForm.patchValue({ startDateTime: null });
    expect(component.eventForm.value.endDateTime).toEqual(new Date('2026-07-01T15:00:00Z'));
  });

  it('sets and clears the location', () => {
    create();
    component.onLocationChanged({ name: 'Bos', lat: 51, lng: 4 });
    expect(component.eventForm.value).toEqual(expect.objectContaining({ locationName: 'Bos', latitude: 51, longitude: 4 }));

    component.onLocationChanged(null);
    expect(component.eventForm.value).toEqual(expect.objectContaining({ locationName: null, latitude: null }));
  });

  it('creates a new event as form data', () => {
    vi.useFakeTimers();
    create();
    component.onSubmit();
    expect(eventService.createEvent).not.toHaveBeenCalled();

    component.eventForm.patchValue({
      title: 'Kamp',
      description: 'Leuk',
      startDateTime: new Date('2026-07-01T10:00:00Z'),
      endDateTime: '2026-07-01T12:00:00Z',
      locationName: 'Bos',
      latitude: 51,
      longitude: 4,
      requiredRoles: ['Leiding', 'Bestuur'],
    });
    component.onSubmit();

    const formData: FormData = eventService.createEvent.mock.calls[0][0];
    expect(formData.get('Title')).toBe('Kamp');
    expect(formData.get('StartDateTime')).toBe('2026-07-01T10:00:00.000Z');
    expect(formData.get('EndDateTime')).toBe('2026-07-01T12:00:00.000Z');
    expect(formData.get('Latitude')).toBe('51');
    expect(formData.get('RequiredRoles[1]')).toBe('Bestuur');
    expect(component.submitting()).toBe(false);

    vi.advanceTimersByTime(1000);
    expect(router.navigate).toHaveBeenCalledWith(['/events']);
  });

  it('updates an existing event and reports failures', () => {
    create('e1');
    eventService.updateEvent.mockReturnValue(throwError(() => new Error('x')));
    component.onSubmit();

    expect(eventService.updateEvent).toHaveBeenCalledWith('e1', expect.any(FormData));
    expect(component.submitting()).toBe(false);
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'Opslaan mislukt' }));
  });
});
