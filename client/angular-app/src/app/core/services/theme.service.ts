import { Injectable, inject, signal } from '@angular/core';

const THEME_KEY = 'theme';

/**
 * Light/dark theme toggle. The value is applied by toggling the `dark` class
 * on <html>, which switches the Material theme variables (see styles.scss).
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly darkSignal = signal(this.loadPreference());

  readonly dark = this.darkSignal.asReadonly();

  constructor() {
    this.apply();
  }

  toggle(): void {
    this.darkSignal.update((value) => !value);
    localStorage.setItem(THEME_KEY, this.darkSignal() ? 'dark' : 'light');
    this.apply();
  }

  private apply(): void {
    const dark = this.darkSignal();
    document.documentElement.classList.toggle('dark', dark);
    document.documentElement.style.colorScheme = dark ? 'dark' : 'light';
  }

  private loadPreference(): boolean {
    const stored = localStorage.getItem(THEME_KEY);
    if (stored) {
      return stored === 'dark';
    }
    return window.matchMedia('(prefers-color-scheme: dark)').matches;
  }
}