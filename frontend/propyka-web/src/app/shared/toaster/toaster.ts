import { Component, inject } from '@angular/core';
import { ToastKind, Toasts } from '../../core/services/toast';

/** Renders the queue from the Toasts service. Mount once, in the app shell. */
@Component({
  selector: 'app-toaster',
  template: `
    <div class="toaster" role="status" aria-live="polite">
      @for (toast of toasts.items(); track toast.id) {
        <div class="toast toast--{{ toast.kind }}">
          <span class="toast__icon" aria-hidden="true">{{ icon(toast.kind) }}</span>
          <p class="toast__message">{{ toast.message }}</p>
          <button
            type="button"
            class="toast__close"
            aria-label="Dismiss notification"
            (click)="toasts.dismiss(toast.id)"
          >×</button>
        </div>
      }
    </div>
  `,
  styleUrl: './toaster.css'
})
export class Toaster {

  readonly toasts = inject(Toasts);

  icon(kind: ToastKind): string {
    switch (kind) {
      case 'success': return '✓';
      case 'error': return '!';
      default: return 'i';
    }
  }
}
