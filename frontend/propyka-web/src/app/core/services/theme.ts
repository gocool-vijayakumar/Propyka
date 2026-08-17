import { Injectable, computed, effect, signal } from '@angular/core';

export type ThemePreference = 'light' | 'dark' | 'system';
export type ResolvedTheme = 'light' | 'dark';

const STORAGE_KEY = 'propyka-theme';

@Injectable({
  providedIn: 'root'
})
export class Theme {

  private readonly media = window.matchMedia('(prefers-color-scheme: dark)');

  /** Tracks the OS setting so 'system' stays live. */
  private readonly systemPrefersDark = signal(this.media.matches);

  /** What the user chose: an explicit theme, or 'system' to follow the OS. */
  readonly preference = signal<ThemePreference>(this.readStoredPreference());

  /** The theme actually applied to the document. */
  readonly resolved = computed<ResolvedTheme>(() => {
    const preference = this.preference();

    if (preference === 'system') {
      return this.systemPrefersDark() ? 'dark' : 'light';
    }

    return preference;
  });

  constructor() {
    this.media.addEventListener('change', (event) => {
      this.systemPrefersDark.set(event.matches);
    });

    effect(() => {
      const theme = this.resolved();
      const preference = this.preference();

      document.documentElement.setAttribute('data-theme', theme);
      document.documentElement.style.colorScheme = theme;

      if (preference === 'system') {
        localStorage.removeItem(STORAGE_KEY);
      } else {
        localStorage.setItem(STORAGE_KEY, preference);
      }
    });
  }

  set(preference: ThemePreference): void {
    this.preference.set(preference);
  }

  /** Flips to the opposite of what is currently on screen. */
  toggle(): void {
    this.preference.set(this.resolved() === 'dark' ? 'light' : 'dark');
  }

  private readStoredPreference(): ThemePreference {
    const stored = localStorage.getItem(STORAGE_KEY);

    return stored === 'light' || stored === 'dark' ? stored : 'system';
  }
}