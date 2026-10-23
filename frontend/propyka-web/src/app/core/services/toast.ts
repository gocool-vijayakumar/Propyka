import { Injectable, signal } from '@angular/core';

export type ToastKind = 'success' | 'error' | 'info';

export interface Toast {
  readonly id: number;
  readonly kind: ToastKind;
  readonly message: string;
}

/**
 * Short, non-blocking feedback shown in the corner of the screen. Keeps at most
 * four messages on screen; each one clears itself after a few seconds.
 */
@Injectable({ providedIn: 'root' })
export class Toasts {

  private static readonly MAX_VISIBLE = 4;

  readonly items = signal<readonly Toast[]>([]);

  private nextId = 1;

  /** Shows a message and returns its id so the caller can dismiss it early. */
  show(message: string, kind: ToastKind = 'info', durationMs = 4500): number {
    const id = this.nextId++;

    this.items.update(list =>
      [...list, { id, kind, message }].slice(-Toasts.MAX_VISIBLE)
    );

    setTimeout(() => this.dismiss(id), durationMs);
    return id;
  }

  success(message: string): number {
    return this.show(message, 'success');
  }

  /** Errors stay a little longer so people have time to read them. */
  error(message: string): number {
    return this.show(message, 'error', 6500);
  }

  dismiss(id: number): void {
    this.items.update(list => list.filter(toast => toast.id !== id));
  }
}
