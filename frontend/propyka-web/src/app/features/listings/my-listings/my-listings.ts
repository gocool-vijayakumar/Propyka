import { Component, OnInit, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { DatePipe, DecimalPipe } from '@angular/common';
import { Auth } from '../../../core/services/auth';
import { ThemeToggle } from '../../../shared/theme-toggle/theme-toggle';
import {
  ListingsService,
  PropertySummary,
  ListingStatus
} from '../../../core/services/listings/listings.service';

@Component({
  selector: 'app-my-listings',
  standalone: true,
  imports: [RouterLink, DatePipe, DecimalPipe, ThemeToggle],
  templateUrl: './my-listings.html',
  styleUrl: './my-listings.css'
})
export class MyListings implements OnInit {

  private listings = inject(ListingsService);
  readonly auth = inject(Auth);
  private router = inject(Router);

  properties = signal<PropertySummary[]>([]);
  isLoading = signal(true);
  errorMessage = signal('');
  page = signal(1);
  totalPages = signal(1);
  total = signal(0);

  actionLoadingId = signal<string | null>(null);
  actionError = signal('');

  initial = () => {
    const p = this.auth.profile();
    return (p?.firstName || p?.email || 'P').charAt(0).toUpperCase();
  };

  ngOnInit(): void { this.load(); }

  load(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');
    this.listings.getMyListings(this.page(), 20).subscribe({
      next: (r) => {
        this.properties.set(r.items);
        this.totalPages.set(r.totalPages || 1);
        this.total.set(r.total);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load your listings.');
        this.isLoading.set(false);
      }
    });
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages()) return;
    this.page.set(p);
    this.load();
  }

  publish(id: string): void {
    this.actionLoadingId.set(id);
    this.actionError.set('');
    this.listings.publishProperty(id).subscribe({
      next: () => { this.actionLoadingId.set(null); this.load(); },
      error: (err) => {
        this.actionLoadingId.set(null);
        this.actionError.set(err.error?.detail ?? 'Could not publish this listing.');
      }
    });
  }

  archive(id: string): void {
    this.actionLoadingId.set(id);
    this.listings.archiveProperty(id).subscribe({
      next: () => { this.actionLoadingId.set(null); this.load(); },
      error: () => { this.actionLoadingId.set(null); }
    });
  }

  statusLabel(status: ListingStatus): string {
    const labels: Record<ListingStatus, string> = {
      Draft: 'Draft', Published: 'Live', UnderOffer: 'Under offer',
      Sold: 'Sold', Rented: 'Rented', Archived: 'Archived'
    };
    return labels[status];
  }

  formatPrice(p: PropertySummary): string {
    const formatted = new Intl.NumberFormat('en-IN', {
      style: 'currency', currency: p.currency, maximumFractionDigits: 0
    }).format(p.price);
    return p.listingType === 'Rent' && p.rentPeriod
      ? `${formatted}/${p.rentPeriod === 'Monthly' ? 'mo' : 'yr'}`
      : formatted;
  }

  logout(): void { this.auth.logout(); this.router.navigate(['/login']); }
}
