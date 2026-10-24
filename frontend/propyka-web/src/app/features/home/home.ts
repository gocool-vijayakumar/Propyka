import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Auth } from '../../core/services/auth';
import { Topbar } from '../../shared/topbar/topbar';
import { AdSlot } from '../../shared/ad-slot/ad-slot';

@Component({
  selector: 'app-home',
  imports: [Topbar, RouterLink, AdSlot],
  templateUrl: './home.html',
  styleUrl: './home.css'
})
export class Home implements OnInit {

  readonly auth = inject(Auth);

  loadFailed = signal(false);

  firstName = computed(() => this.auth.profile()?.firstName ?? '');

  /** Time-of-day greeting, read once when the page opens. */
  greeting(): string {
    const hour = new Date().getHours();
    if (hour < 12) return 'Good morning';
    if (hour < 18) return 'Good afternoon';
    return 'Good evening';
  }

  ngOnInit(): void {
    if (this.auth.profile()) {
      return;
    }

    this.auth.loadProfile().subscribe({
      error: () => this.loadFailed.set(true)
    });
  }
}
