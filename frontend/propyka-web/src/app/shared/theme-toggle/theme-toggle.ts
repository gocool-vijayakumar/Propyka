import { Component, inject } from '@angular/core';
import { Theme } from '../../core/services/theme';

@Component({
  selector: 'app-theme-toggle',
  imports: [],
  template: `
    <button
      type="button"
      class="pk-icon-btn"
      (click)="theme.toggle()"
      [attr.aria-label]="isDark() ? 'Switch to light theme' : 'Switch to dark theme'"
      [attr.title]="isDark() ? 'Switch to light theme' : 'Switch to dark theme'"
    >
      @if (isDark()) {
        <!-- Sun: click to go light -->
        <svg viewBox="0 0 24 24" width="17" height="17" fill="none"
             stroke="currentColor" stroke-width="1.6" stroke-linecap="round" aria-hidden="true">
          <circle cx="12" cy="12" r="4"></circle>
          <path d="M12 2.5v2M12 19.5v2M2.5 12h2M19.5 12h2M5.2 5.2l1.4 1.4M17.4 17.4l1.4 1.4M18.8 5.2l-1.4 1.4M6.6 17.4l-1.4 1.4"></path>
        </svg>
      } @else {
        <!-- Moon: click to go dark -->
        <svg viewBox="0 0 24 24" width="17" height="17" fill="none"
             stroke="currentColor" stroke-width="1.6" stroke-linejoin="round" aria-hidden="true">
          <path d="M20.5 14.6A8.6 8.6 0 0 1 9.4 3.5a8.7 8.7 0 1 0 11.1 11.1z"></path>
        </svg>
      }
    </button>
  `,
  styles: `
    :host {
      display: inline-flex;
    }
  `
})
export class ThemeToggle {

  readonly theme = inject(Theme);

  isDark = () => this.theme.resolved() === 'dark';
}