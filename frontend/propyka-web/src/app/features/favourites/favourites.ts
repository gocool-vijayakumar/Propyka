import { Component, OnInit, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { Auth } from '../../core/services/auth';
import { ThemeToggle } from '../../shared/theme-toggle/theme-toggle';
import {
  ListingsService,
  PropertySummary
} from '../../core/services/listings/listings.service';

@Component({
  selector: 'app-favourites',
  standalone: true,
  imports: [RouterLink, DecimalPipe, ThemeToggle],
  templateUrl: './favourites.html',
  styleUrl: './favourites.css'
})
export class Favourites implements OnInit {

  private listings = inject(ListingsService);
  readonly auth = inject(Auth);
  private router = inject(Router);

  properties = signal<PropertySummary[]>([]);
  isLoading = signal(true);
  errorMessage = signal('');
  total = signal(0);
  page = signal(1);
  totalPages = signal(1);

  initial = () => {
    const p = this.auth.profile();
    return (p?.firstName || p?.email || 'P').charAt(0).toUpperCase();
  };

  ngOnInit(): void { this.load(); }

  load(): void {
    this.isLoading.set(true);
    this.listings.getFavourites(this.page()).subscribe({
      next: (r) => {
        this.properties.set(r.items);
        this.total.set(r.total);
        this.totalPages.set(r.totalPages || 1);
        this.isLoading.set(false);
      },
      error: () => { this.errorMessage.set('Could not load saved properties.'); this.isLoading.set(false); }
    });
  }

  removeFavourite(id: string): void {
    this.listings.removeFavourite(id).subscribe({
      next: () => this.load(),
      error: () => {}
    });
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
