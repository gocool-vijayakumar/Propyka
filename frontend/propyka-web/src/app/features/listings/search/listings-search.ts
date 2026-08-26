import {
  Component, OnInit, inject, signal, computed
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { Auth } from '../../../core/services/auth';
import { ThemeToggle } from '../../../shared/theme-toggle/theme-toggle';
import {
  ListingsService,
  PropertySummary,
  PropertySearchQuery,
  ListingType,
  PropertyKind,
  PropertySort
} from '../../../core/services/listings/listings.service';

@Component({
  selector: 'app-listings-search',
  standalone: true,
  imports: [FormsModule, RouterLink, DecimalPipe, ThemeToggle],
  templateUrl: './listings-search.html',
  styleUrl: './listings-search.css'
})
export class ListingsSearch implements OnInit {

  private listings = inject(ListingsService);
  readonly auth = inject(Auth);
  private router = inject(Router);

  // ── Data ──────────────────────────────────────────────────────────────────
  properties = signal<PropertySummary[]>([]);
  isLoading = signal(true);
  errorMessage = signal('');
  total = signal(0);
  totalPages = signal(1);
  favouriteIds = signal<Set<string>>(new Set());

  // ── Filters ───────────────────────────────────────────────────────────────
  searchText = signal('');
  city = signal('');
  listingType = signal<ListingType | ''>('');
  kind = signal<PropertyKind | ''>('');
  minBedrooms = signal<number | null>(null);
  sort = signal<PropertySort>('Newest');
  page = signal(1);

  readonly pageSize = 12;

  readonly listingTypes: ListingType[] = ['Sale', 'Rent'];
  readonly propertyKinds: PropertyKind[] = ['Apartment', 'House', 'Villa', 'Plot', 'Commercial', 'Warehouse'];
  readonly sortOptions: { value: PropertySort; label: string }[] = [
    { value: 'Newest', label: 'Newest first' },
    { value: 'PriceAsc', label: 'Price: low to high' },
    { value: 'PriceDesc', label: 'Price: high to low' }
  ];

  displayName = computed(() => {
    const p = this.auth.profile();
    return p ? `${p.firstName} ${p.lastName}`.trim() : '';
  });

  initial = computed(() => {
    const p = this.auth.profile();
    return (p?.firstName || p?.email || 'P').charAt(0).toUpperCase();
  });

  private searchInput$ = new Subject<string>();
  private cityInput$ = new Subject<string>();

  constructor() {
    this.searchInput$.pipe(
      debounceTime(400), distinctUntilChanged(), takeUntilDestroyed()
    ).subscribe(v => { this.searchText.set(v); this.page.set(1); this.load(); });

    this.cityInput$.pipe(
      debounceTime(400), distinctUntilChanged(), takeUntilDestroyed()
    ).subscribe(v => { this.city.set(v); this.page.set(1); this.load(); });
  }

  ngOnInit(): void {
    this.load();
    if (this.auth.isAuthenticated()) {
      this.loadFavouriteIds();
    }
  }

  load(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');

    const query: PropertySearchQuery = {
      search: this.searchText() || undefined,
      city: this.city() || undefined,
      listingType: this.listingType() || undefined,
      kind: this.kind() || undefined,
      minBedrooms: this.minBedrooms() ?? undefined,
      sort: this.sort(),
      page: this.page(),
      pageSize: this.pageSize
    };

    this.listings.searchProperties(query).subscribe({
      next: (result) => {
        this.properties.set(result.items);
        this.total.set(result.total);
        this.totalPages.set(result.totalPages || 1);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load listings. Check your connection.');
        this.isLoading.set(false);
      }
    });
  }

  private loadFavouriteIds(): void {
    this.listings.getFavouriteIds().subscribe({
      next: (ids) => this.favouriteIds.set(new Set(ids)),
      error: () => { /* non-critical */ }
    });
  }

  // ── Actions ───────────────────────────────────────────────────────────────
  onSearchInput(value: string): void { this.searchInput$.next(value); }
  onCityInput(value: string): void   { this.cityInput$.next(value); }

  setListingType(value: ListingType | ''): void {
    this.listingType.set(value); this.page.set(1); this.load();
  }

  setKind(value: PropertyKind | ''): void {
    this.kind.set(value); this.page.set(1); this.load();
  }

  setSort(value: PropertySort): void {
    this.sort.set(value); this.page.set(1); this.load();
  }

  setBedrooms(value: number | null): void {
    this.minBedrooms.set(value); this.page.set(1); this.load();
  }

  clearFilters(): void {
    this.searchText.set('');
    this.city.set('');
    this.listingType.set('');
    this.kind.set('');
    this.minBedrooms.set(null);
    this.sort.set('Newest');
    this.page.set(1);
    this.load();
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages()) return;
    this.page.set(p);
    this.load();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  isFavourited(id: string): boolean {
    return this.favouriteIds().has(id);
  }

  toggleFavourite(event: Event, property: PropertySummary): void {
    event.preventDefault();
    event.stopPropagation();

    if (!this.auth.isAuthenticated()) {
      this.router.navigate(['/login']);
      return;
    }

    const id = property.id;
    const isFav = this.isFavourited(id);

    // Optimistic update
    const next = new Set(this.favouriteIds());
    if (isFav) next.delete(id); else next.add(id);
    this.favouriteIds.set(next);

    const req = isFav
      ? this.listings.removeFavourite(id)
      : this.listings.addFavourite(id);

    req.subscribe({
      error: () => {
        // Revert on error
        const reverted = new Set(this.favouriteIds());
        if (isFav) reverted.add(id); else reverted.delete(id);
        this.favouriteIds.set(reverted);
      }
    });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────
  formatPrice(p: PropertySummary): string {
    const formatted = new Intl.NumberFormat('en-IN', {
      style: 'currency', currency: p.currency, maximumFractionDigits: 0
    }).format(p.price);
    return p.listingType === 'Rent' && p.rentPeriod
      ? `${formatted}/${p.rentPeriod === 'Monthly' ? 'mo' : 'yr'}`
      : formatted;
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }

  pagesArray(): number[] {
    const total = this.totalPages();
    const current = this.page();
    const pages: number[] = [];
    const range = 2;

    if (total <= 7) {
      for (let i = 1; i <= total; i++) pages.push(i);
      return pages;
    }

    pages.push(1);
    if (current - range > 2) pages.push(-1); // ellipsis
    for (let i = Math.max(2, current - range); i <= Math.min(total - 1, current + range); i++) {
      pages.push(i);
    }
    if (current + range < total - 1) pages.push(-1);
    pages.push(total);
    return pages;
  }
}
