import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Auth } from '../../../core/services/auth';
import { ThemeToggle } from '../../../shared/theme-toggle/theme-toggle';

@Component({
  selector: 'app-admin-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ThemeToggle],
  templateUrl: './admin-shell.html',
  styleUrl: './admin-shell.css'
})
export class AdminShell {

  readonly auth = inject(Auth);
  private router = inject(Router);

  initial(): string {
    const profile = this.auth.profile();
    return profile?.firstName?.charAt(0).toUpperCase() ?? 'A';
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}