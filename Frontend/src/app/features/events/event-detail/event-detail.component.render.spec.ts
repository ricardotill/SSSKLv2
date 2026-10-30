import { TestBed, ComponentFixture } from '@angular/core/testing';
import { signal } from '@angular/core';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { MessageService } from 'primeng/api';
import { vi } from 'vitest';
import EventDetailComponent from './event-detail.component';
import { EventService } from '../services/event.service';
import { AuthService } from '../../../core/auth/auth.service';
import { GoogleMapsService } from '../../../core/services/google-maps.service';
import { ThemeService } from '../../../core/services/theme.service';
import { UrlService } from '../../../core/services/url.service';
import { ReactionService } from '../../../core/services/reaction.service';
import { UserProfileDrawerService } from '../../../core/services/user-profile-drawer.service';
import { EventDto } from '../../../core/models/event.model';

describe('EventDetailComponent (rendered)', () => {
  let fixture: ComponentFixture<EventDetailComponent>;
  let component: EventDetailComponent;
  let currentUser: ReturnType<typeof signal<any>>;
  let drawer: { open: ReturnType<typeof vi.fn> };
  let eventService: any;

  const user = (id: string, pic: string | null = null) => ({ userId: id, userName: `user-${id}`, profilePictureUrl: pic });

  const evt = (overrides: Partial<EventDto> = {}) =>
    ({
      id: 'e1',
      title: 'Kamp',
      description: '<p>Leuk <b>kamp</b></p>',
      imageUrl: '/img/e1',
      startDateTime: '2026-07-01T10:00:00Z',
      endDateTime: '2026-07-02T10:00:00Z',
      creatorId: 'creator',
      creatorName: 'Creator',
      creatorProfilePictureUrl: null,
      createdOn: '2026-01-01T00:00:00Z',
      acceptedUsers: [user('a1', '/pic/a1'), user('a2'), user('a3'), user('a4'), user('a5'), user('a6')],
      declinedUsers: [user('d1')],
      requiredRoles: ['Leiding'],
      latitude: 52.1,
      longitude: 5.1,
      locationName: 'Clubhuis',
      userResponse: 'Accepted',
      ...overrides,
    }) as unknown as EventDto;

  function create(event: EventDto) {
    eventService = {
      getEvent: vi.fn(() => of(event)),
      updateEventImage: vi.fn(() => of(void 0)),
      rsvp: vi.fn(() => of(void 0)),
      deleteEvent: vi.fn(() => of(void 0)),
    };
    TestBed.configureTestingModule({
      imports: [EventDetailComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: EventService, useValue: eventService },
        { provide: AuthService, useValue: { currentUser, isAuthenticated: () => !!currentUser() } },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'e1' } } } },
        { provide: GoogleMapsService, useValue: { load: () => of(false), getMapId: () => of(undefined) } },
        { provide: ThemeService, useValue: { isDark: () => false } },
        { provide: UrlService, useValue: { resolveApiUrl: (u: string | null | undefined) => (u ? `https://api${u}` : undefined) } },
        { provide: ReactionService, useValue: { getReactions: vi.fn(() => of([])), toggleReaction: vi.fn(() => of(void 0)) } },
        { provide: UserProfileDrawerService, useValue: drawer },
        { provide: MessageService, useValue: { add: vi.fn() } },
      ],
    });
    fixture = TestBed.createComponent(EventDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  const text = () => (fixture.nativeElement as HTMLElement).textContent ?? '';

  beforeEach(() => {
    currentUser = signal<any>({ id: 'creator', roles: ['Leiding'] });
    drawer = { open: vi.fn() };
  });

  it('renders the full event for its creator', () => {
    create(evt());
    const el = fixture.nativeElement as HTMLElement;

    expect(el.querySelector('h1')?.textContent).toContain('Kamp');
    expect(text()).toContain('Clubhuis');
    expect(text()).toContain('Ga naar Google Maps');
    expect(text()).toContain('Toegestane Rollen');
    expect(text()).toContain('+1');
    expect(text()).toContain('user-d1');
    expect(text()).toContain('Afbeelding wijzigen');
    expect(el.querySelector('a[href*="google.com/maps"]')?.getAttribute('href')).toContain('52.1,5.1');
    expect(el.querySelector('.rich-text-content')?.innerHTML).toContain('kamp');
  });

  it('opens the profile drawer from attendee avatars and names', () => {
    create(evt());
    const el = fixture.nativeElement as HTMLElement;

    el.querySelectorAll<HTMLElement>('p-avatar').forEach((a) => a.click());
    el.querySelectorAll<HTMLElement>('span.cursor-pointer').forEach((s) => s.click());

    expect(drawer.open).toHaveBeenCalledWith('a1');
    expect(drawer.open).toHaveBeenCalledWith('d1');
    expect(drawer.open).toHaveBeenCalledWith('creator');
  });

  it('renders an empty event without location, roles or attendees', () => {
    create(
      evt({
        imageUrl: undefined,
        locationName: undefined,
        latitude: undefined,
        longitude: undefined,
        requiredRoles: [],
        acceptedUsers: [],
        declinedUsers: [],
        creatorProfilePictureUrl: '/pic/c',
        userResponse: 'Declined',
      } as Partial<EventDto>),
    );

    expect(text()).toContain('Nog geen aanmeldingen');
    expect(text()).toContain('Nog geen afmeldingen');
    expect(text()).not.toContain('Toegestane Rollen');
  });

  it('tells users without the right role that they cannot RSVP', () => {
    currentUser.set({ id: 'u2', roles: ['User'] });
    create(evt());

    expect(text()).toContain('Je hebt niet de juiste rol');
    expect(text()).not.toContain('Afbeelding wijzigen');
  });

  it('shows a login button to anonymous visitors', () => {
    currentUser.set(null);
    create(evt());

    expect(text()).not.toContain('Je hebt niet de juiste rol');
    expect((fixture.nativeElement as HTMLElement).querySelector('p-button[icon="pi pi-sign-in"]')).toBeTruthy();
  });

  it('renders the image dialog with a preview and the selected file', () => {
    create(evt());
    component.imageDialogVisible.set(true);
    component.imagePreview.set('data:image/png;base64,AAAA');
    component.selectedImageFile.set(new File(['x'], 'banner.png', { type: 'image/png' }));
    fixture.detectChanges();

    expect(document.body.textContent).toContain('banner.png');
    expect(document.querySelector('img[alt="Preview van eventafbeelding"]')).toBeTruthy();

    component.imagePreview.set(null);
    fixture.detectChanges();
    expect(document.querySelector('img[alt="Huidige eventafbeelding"]')).toBeTruthy();
  });

  it('renders the image dialog placeholder without any image', () => {
    create(evt({ imageUrl: undefined } as Partial<EventDto>));
    component.imageDialogVisible.set(true);
    fixture.detectChanges();

    expect(document.body.textContent).toContain('Geen afbeelding beschikbaar');
  });

  it('shows a spinner while loading', () => {
    create(evt());
    component.loading.set(true);
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('p-progress-spinner')).toBeTruthy();
  });
});
