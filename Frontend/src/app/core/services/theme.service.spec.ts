import { TestBed } from '@angular/core/testing';
import { ThemeService, ThemeMode } from './theme.service';
import { vi } from 'vitest';

describe('ThemeService', () => {
  let service: ThemeService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [ThemeService]
    });
    service = TestBed.inject(ThemeService);
    // Effects in services might need a dummy component to run in tests.
    // However, we can also test the initial state and manual calls to init().
  });

  afterEach(() => {
    localStorage.clear();
    document.documentElement.classList.remove('dark');
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should initialize with default mode (auto)', () => {
    expect(service.mode()).toBe('auto');
  });

  it('should set mode and persist it (manual check)', () => {
    service.setMode('dark');
    expect(service.mode()).toBe('dark');
    // If effect doesn't run in service test without fixture, we test the signal and manual init
    service.init();
    expect(document.documentElement.classList.contains('dark')).toBe(true);
  });

  it('should apply dark class to html when mode is dark', () => {
    service.setMode('dark');
    service.init();
    expect(document.documentElement.classList.contains('dark')).toBe(true);
    
    service.setMode('light');
    service.init();
    expect(document.documentElement.classList.contains('dark')).toBe(false);
  });

  it('should handle auto mode based on system preference', () => {
    // Mock system preference
    const matchMediaSpy = vi.spyOn(window, 'matchMedia').mockImplementation((query) => ({
      matches: query.includes('dark'),
      media: query,
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn(),
    } as any));

    service.setMode('auto');
    service.init();
    expect(document.documentElement.classList.contains('dark')).toBe(true);

    matchMediaSpy.mockRestore();
  });
});
