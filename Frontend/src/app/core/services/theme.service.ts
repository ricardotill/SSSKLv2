import { Injectable, signal, effect, computed } from '@angular/core';

export type ThemeMode = 'auto' | 'light' | 'dark';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly THEME_KEY = 'sssklv2-theme-mode';
  
  // Initialize from localStorage or default to 'auto'
  mode = signal<ThemeMode>((localStorage.getItem(this.THEME_KEY) as ThemeMode) || 'auto');

  isDark = computed(() => {
    const currentMode = this.mode();
    if (currentMode === 'auto') {
      return window.matchMedia('(prefers-color-scheme: dark)').matches;
    }
    return currentMode === 'dark';
  });

  constructor() {
    // React to mode changes
    effect(() => {
      const currentMode = this.mode();
      localStorage.setItem(this.THEME_KEY, currentMode);
      this.applyTheme(currentMode);
    });

    // Listen for system theme changes if in 'auto' mode
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
      if (this.mode() === 'auto') {
        this.applyTheme('auto');
      }
    });
  }

  setMode(newMode: ThemeMode) {
    this.mode.set(newMode);
  }

  private applyTheme(mode: ThemeMode) {
    const html = document.documentElement;
    let isDark = false;

    if (mode === 'auto') {
      isDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    } else {
      isDark = mode === 'dark';
    }

    if (isDark) {
      html.classList.add('dark');
    } else {
      html.classList.remove('dark');
    }
  }

  // Initial call to ensure theme is applied on startup
  init() {
    this.applyTheme(this.mode());
  }
}
