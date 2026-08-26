import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { Auth } from '../../core/services/auth';
import { ThemeToggle } from '../../shared/theme-toggle/theme-toggle';

@Component({
  selector: 'app-home',
  imports: [ThemeToggle, RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.css'
})
export class Home implements OnInit {

  readonly auth = inject(Auth);
  private router = inject(Router);

  loadFailed = signal(false);

  initial = computed(() => {
    const profile = this.auth.profile();
    return (profile?.firstName || profile?.email || 'P').charAt(0).toUpperCase();
  });

  firstName = computed(() => this.auth.profile()?.firstName ?? '');

  ngOnInit(): void {
    if (this.auth.profile()) {
      return;
    }

    this.auth.loadProfile().subscribe({
      error: () => this.loadFailed.set(true)
    });
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
