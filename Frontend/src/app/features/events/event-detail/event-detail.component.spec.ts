import { TestBed, ComponentFixture } from '@angular/core/testing';
import { signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { MessageService } from 'primeng/api';
import { vi } from 'vitest';
import EventDetailComponent from './event-detail.component';
import { EventService } from '../services/event.service';
import { AuthService } from '../../../core/auth/auth.service';
import { GoogleMapsService } from '../../../core/services/google-maps.service';
import { ThemeService } from '../../../core/services/theme.service';
import { UrlService } from '../../../core/services/url.service';
import { EventDto } from '../../../core/models/event.model';

describe('EventDetailComponent', () => {
  let fixture: ComponentFixture<EventDetailComponent>;
  let component: EventDetailComponent;
  let eventService: any;
  let messageService: { add: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };
  let currentUser: ReturnType<typeof signal<any>>;
  let mapsLoaded: boolean;

  const evt = {
    id: 'e1',
    title: 'Kamp',
    description: '<p>Leuk <b>kamp</b></p>',
    imageUrl: '/img/e1',
    startDateTime: '2026-07-01T10:00:00Z',
    endDateTime: '2026-07-02T10:00:00Z',
    creatorId: 'creator',
    creatorName: 'Creator',
    createdOn: '2026-01-01T00:00:00Z',
    acceptedUsers: [],
    declinedUsers: [],
    requiredRoles: ['Leiding'],
    latitude: 52.1,
    longitude: 5.1,
    locationName: 'Clubhuis',
  } as unknown as EventDto;

  function create(paramId: string | null = 'e1') {
    TestBed.configureTestingModule({
      imports: [EventDetailComponent],
      providers: [
        { provide: EventService, useValue: eventService },
        { provide: AuthService, useValue: { currentUser } },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => paramId } } } },
        { provide: GoogleMapsService, useValue: { load: () => of(mapsLoaded), getMapId: () => of(undefined) } },
        { provide: ThemeService, useValue: { isDark: () => true } },
        { provide: UrlService, useValue: { resolveApiUrl: (u: string | null | undefined) => (u ? `https://api${u}` : undefined) } },
      ],
    });
    TestBed.overrideComponent(EventDetailComponent, {
      set: {
        template: '<div #detailMapContainer></div>',
        providers: [{ provide: MessageService, useValue: messageService }],
      },
    });
    fixture = TestBed.createComponent(EventDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  beforeEach(() => {
    eventService = {
      getEvent: vi.fn(() => of(evt)),
      updateEventImage: vi.fn(() => of(void 0)),
      rsvp: vi.fn(() => of(void 0)),
      deleteEvent: vi.fn(() => of(void 0)),
    };
    messageService = { add: vi.fn() };
    router = { navigate: vi.fn() };
    currentUser = signal<any>({ id: 'u1', roles: ['User'] });
    mapsLoaded = false;
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.useRealTimers();
  });

  it('loads the event from the route and sets the meta tags', () => {
    create();

    expect(eventService.getEvent).toHaveBeenCalledWith('e1');
    expect(component.event()).toEqual(evt);
    expect(component.loading()).toBe(false);
    expect(document.title).toBe('Kamp - SSSKL');
    expect(document.querySelector('meta[property="og:description"]')?.getAttribute('content')).toBe('Leuk kamp');
    expect(document.querySelector('meta[property="og:image"]')?.getAttribute('content')).toBe(
      'https://api/img/e1/social-preview.jpg',
    );
  });

  it('does not load without a route id', () => {
    create(null);
    expect(eventService.getEvent).not.toHaveBeenCalled();
  });

  it('reports a failed load', () => {
    eventService.getEvent.mockReturnValue(throwError(() => new Error('x')));
    create();
    expect(component.loading()).toBe(false);
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
  });

  it('only lets admins and the creator edit', () => {
    create();
    expect(component.canEdit()).toBe(false);

    currentUser.set({ id: 'creator', roles: [] });
    expect(component.canEdit()).toBe(true);

    currentUser.set({ id: 'u2', roles: ['Admin'] });
    expect(component.canEdit()).toBe(true);

    currentUser.set(null);
    expect(component.canEdit()).toBe(false);
  });

  it('only lets users with a required role RSVP', () => {
    create();
    expect(component.canRsvp()).toBe(false);

    currentUser.set({ id: 'u1', roles: ['Leiding'] });
    expect(component.canRsvp()).toBe(true);

    currentUser.set({ id: 'u1', roles: ['Admin'] });
    expect(component.canRsvp()).toBe(true);

    currentUser.set({ id: 'u1', roles: [] });
    component.event.set({ ...evt, requiredRoles: [] });
    expect(component.canRsvp()).toBe(true);

    currentUser.set(null);
    expect(component.canRsvp()).toBe(false);
  });

  it('renders the map once the maps API is loaded', async () => {
    vi.useFakeTimers();
    const Map = vi.fn();
    const AdvancedMarkerElement = vi.fn();
    vi.stubGlobal('google', {
      maps: {
        Map,
        ColorScheme: { DARK: 'DARK', LIGHT: 'LIGHT' },
        importLibrary: vi.fn(async () => ({ AdvancedMarkerElement })),
      },
    });
    mapsLoaded = true;
    create();
    fixture.detectChanges();

    await vi.advanceTimersByTimeAsync(100);

    expect(Map).toHaveBeenCalledWith(
      expect.any(HTMLElement),
      expect.objectContaining({ center: { lat: 52.1, lng: 5.1 }, colorScheme: 'DARK', styles: expect.any(Array) }),
    );
    expect(AdvancedMarkerElement).toHaveBeenCalledWith(expect.objectContaining({ title: 'Clubhuis' }));
  });

  it('opens and closes the image dialog with the current image', () => {
    create();
    component.triggerImageUpload();
    expect(component.imageDialogVisible()).toBe(true);
    expect(component.imagePreview()).toBe('https://api/img/e1');

    component.closeImageModal();
    expect(component.imageDialogVisible()).toBe(false);
    expect(component.selectedImageFile()).toBeNull();
  });

  it('previews a selected image and resets when the selection is cleared', () => {
    create();
    vi.stubGlobal('URL', { ...URL, createObjectURL: vi.fn(() => 'blob:preview') });
    const file = new File(['x'], 'a.png', { type: 'image/png' });

    component.onImageFileSelected({ target: { files: [file] } } as any);
    expect(component.selectedImageFile()).toBe(file);
    expect(component.imagePreview()).toBe('blob:preview');

    component.onImageFileSelected({ target: { files: [] } } as any);
    expect(component.selectedImageFile()).toBeNull();
    expect(component.imagePreview()).toBe('https://api/img/e1');
  });

  it('uploads the selected image and reloads the event', () => {
    create();
    const file = new File(['x'], 'a.png', { type: 'image/png' });
    component.uploadSelectedImage();
    expect(eventService.updateEventImage).not.toHaveBeenCalled();

    component.selectedImageFile.set(file);
    component.uploadSelectedImage();

    expect(eventService.updateEventImage).toHaveBeenCalledWith('e1', file);
    expect(component.uploadingImage()).toBe(false);
    expect(eventService.getEvent).toHaveBeenCalledTimes(2);
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
  });

  it('reports a failed image upload', () => {
    create();
    eventService.updateEventImage.mockReturnValue(throwError(() => new Error('x')));
    component.selectedImageFile.set(new File(['x'], 'a.png'));
    component.uploadSelectedImage();
    expect(component.uploadingImage()).toBe(false);
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
  });

  it('shares through the Web Share API when available', () => {
    create();
    const share = vi.fn(() => Promise.resolve());
    vi.stubGlobal('navigator', { share });
    component.share();
    expect(share).toHaveBeenCalledWith(expect.objectContaining({ title: 'Kamp' }));
  });

  it('copies the link when sharing is not available', async () => {
    create();
    const writeText = vi.fn(() => Promise.resolve());
    vi.stubGlobal('navigator', { clipboard: { writeText } });
    component.share();
    await Promise.resolve();
    await Promise.resolve();
    expect(writeText).toHaveBeenCalled();
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ summary: 'Gekopieerd' }));
  });

  it('RSVPs and reloads the event', () => {
    create();
    component.rsvp('Accepted');
    expect(eventService.rsvp).toHaveBeenCalledWith('e1', 'Accepted');
    expect(component.rsvpLoading()).toBe(false);
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'Je bent aangemeld!' }));

    component.rsvp('Declined');
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'Je bent afgemeld.' }));
  });

  it('reports a failed RSVP', () => {
    create();
    eventService.rsvp.mockReturnValue(throwError(() => new Error('x')));
    component.rsvp('Accepted');
    expect(component.rsvpLoading()).toBe(false);
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'RSVP mislukt' }));
  });

  it('deletes the event after confirmation', () => {
    create();
    vi.stubGlobal('confirm', vi.fn(() => true));
    component.deleteEvent();
    expect(eventService.deleteEvent).toHaveBeenCalledWith('e1');
    expect(router.navigate).toHaveBeenCalledWith(['/events']);
  });

  it('keeps the event when deletion is cancelled or fails', () => {
    create();
    vi.stubGlobal('confirm', vi.fn(() => false));
    component.deleteEvent();
    expect(eventService.deleteEvent).not.toHaveBeenCalled();

    vi.stubGlobal('confirm', vi.fn(() => true));
    eventService.deleteEvent.mockReturnValue(throwError(() => new Error('x')));
    component.deleteEvent();
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'Verwijderen mislukt' }));
  });

  it('fixes coordinates mangled by locale formatting', () => {
    create();
    const sanitize = (component as any).sanitizeCoordinate.bind(component);
    expect(sanitize(undefined)).toBeUndefined();
    expect(sanitize(Number.NaN)).toBeUndefined();
    expect(sanitize(0)).toBe(0);
    expect(sanitize(52.1)).toBe(52.1);
    expect(sanitize(521234)).toBeCloseTo(52.1234);
  });

  it('ignores actions when no event is loaded', () => {
    eventService.getEvent.mockReturnValue(throwError(() => new Error('x')));
    create();
    component.share();
    component.rsvp('Accepted');
    component.deleteEvent();
    expect(eventService.rsvp).not.toHaveBeenCalled();
    expect(eventService.deleteEvent).not.toHaveBeenCalled();
  });
});
