import {
  Component, ElementRef, HostListener, computed, inject, signal
} from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { Auth } from '../../core/services/auth';
import { ThemeToggle } from '../theme-toggle/theme-toggle';

/**
 * Shared header: brand, primary navigation, theme switch and the account menu.
 * On phones the navigation moves into a bottom tab bar.
 */
@Component({
  selector: 'app-topbar',
  imports: [RouterLink, RouterLinkActive, ThemeToggle],
  templateUrl: './topbar.html',
  styleUrl: './topbar.css'
})
export class Topbar {

  readonly auth = inject(Auth);
  private router = inject(Router);
  private host = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly menuOpen = signal(false);

  /** Exact matching, so "Browse" does not light up on "My listings". */
  readonly exactMatch = { exact: true };

  readonly homeLink = computed(() => (this.auth.isAuthenticated() ? '/home' : '/listings'));

  readonly initial = computed(() => {
    const profile = this.auth.profile();
    return (profile?.firstName || profile?.email || 'P').charAt(0).toUpperCase();
  });

  toggleMenu(): void {
    this.menuOpen.update(open => !open);
  }

  closeMenu(): void {
    this.menuOpen.set(false);
  }

  @HostListener('document:click', ['$event.target'])
  onDocumentClick(target: EventTarget | null): void {
    if (this.menuOpen() && target instanceof Node && !this.host.nativeElement.contains(target)) {
      this.closeMenu();
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closeMenu();
  }

  logout(): void {
    this.closeMenu();
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
