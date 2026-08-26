import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DecimalPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Auth } from '../../../core/services/auth';
import { ThemeToggle } from '../../../shared/theme-toggle/theme-toggle';
import {
  ListingsService,
  PropertyDetail,
  PropertyImage
} from '../../../core/services/listings/listings.service';

@Component({
  selector: 'app-listing-detail',
  standalone: true,
  imports: [FormsModule, RouterLink, DecimalPipe, DatePipe, ThemeToggle],
  templateUrl: './listing-detail.html',
  styleUrl: './listing-detail.css'
})
export class ListingDetail implements OnInit {

  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private listings = inject(ListingsService);
  readonly auth = inject(Auth);

  property = signal<PropertyDetail | null>(null);
  isLoading = signal(true);
  errorMessage = signal('');
  activeImageIndex = signal(0);
  isFavourited = signal(false);
  isFavLoading = signal(false);

  // Enquiry form
  showEnquiryForm = signal(false);
  enquiryName = signal('');
  enquiryEmail = signal('');
  enquiryPhone = signal('');
  enquiryMessage = signal('');
  enquiryLoading = signal(false);
  enquiryError = signal('');
  enquirySent = signal(false);

  // Owner actions (when viewing your own listing)
  actionLoading = signal(false);
  actionError = signal('');

  slug = '';

  isOwner = computed(() => {
    const p = this.property();
    const profile = this.auth.profile();
    return !!(p && profile && p.owner.id === profile.id);
  });

  images = computed(() => {
    const p = this.property();
    return p?.images ?? [];
  });

  activeImage = computed(() => {
    const imgs = this.images();
    return imgs[this.activeImageIndex()] ?? null;
  });

  initial = computed(() => {
    const p = this.auth.profile();
    return (p?.firstName || p?.email || 'P').charAt(0).toUpperCase();
  });

  ngOnInit(): void {
    this.slug = this.route.snapshot.paramMap.get('slug') ?? '';
    this.load();

    if (this.auth.profile() && !this.auth.profile()) {
      this.auth.loadProfile().subscribe();
    }
  }

  load(): void {
    this.isLoading.set(true);
    this.listings.getPropertyBySlug(this.slug).subscribe({
      next: (p) => {
        this.property.set(p);
        this.isLoading.set(false);
        if (this.auth.isAuthenticated()) {
          this.loadFavouriteStatus(p.id);
        }
        // Pre-fill enquiry name/email if logged in
        const profile = this.auth.profile();
        if (profile) {
          this.enquiryName.set(`${profile.firstName} ${profile.lastName}`.trim());
          this.enquiryEmail.set(profile.email);
        }
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.status === 404
          ? 'This listing no longer exists or has been taken down.'
          : 'Could not load this listing. Try again.');
      }
    });
  }

  private loadFavouriteStatus(propertyId: string): void {
    this.listings.getFavouriteIds().subscribe({
      next: (ids) => this.isFavourited.set(ids.includes(propertyId)),
      error: () => {}
    });
  }

  setActiveImage(index: number): void {
    this.activeImageIndex.set(index);
  }

  nextImage(): void {
    const count = this.images().length;
    if (count === 0) return;
    this.activeImageIndex.set((this.activeImageIndex() + 1) % count);
  }

  prevImage(): void {
    const count = this.images().length;
    if (count === 0) return;
    this.activeImageIndex.set((this.activeImageIndex() - 1 + count) % count);
  }

  toggleFavourite(): void {
    if (!this.auth.isAuthenticated()) {
      this.router.navigate(['/login']);
      return;
    }
    const p = this.property();
    if (!p || this.isFavLoading()) return;

    this.isFavLoading.set(true);
    const isFav = this.isFavourited();
    this.isFavourited.set(!isFav);

    const req = isFav
      ? this.listings.removeFavourite(p.id)
      : this.listings.addFavourite(p.id);

    req.subscribe({
      next: () => this.isFavLoading.set(false),
      error: () => { this.isFavourited.set(isFav); this.isFavLoading.set(false); }
    });
  }

  openEnquiry(): void {
    if (!this.auth.isAuthenticated()) {
      this.router.navigate(['/login']);
      return;
    }
    this.showEnquiryForm.set(true);
  }

  submitEnquiry(): void {
    if (!this.enquiryName().trim() || !this.enquiryEmail().trim() || !this.enquiryMessage().trim()) {
      this.enquiryError.set('Name, email and message are all required.');
      return;
    }
    const p = this.property();
    if (!p) return;

    this.enquiryLoading.set(true);
    this.enquiryError.set('');

    this.listings.createEnquiry(p.id, {
      name: this.enquiryName().trim(),
      email: this.enquiryEmail().trim(),
      phone: this.enquiryPhone().trim() || undefined,
      message: this.enquiryMessage().trim()
    }).subscribe({
      next: () => {
        this.enquiryLoading.set(false);
        this.enquirySent.set(true);
        this.showEnquiryForm.set(false);
      },
      error: () => {
        this.enquiryLoading.set(false);
        this.enquiryError.set('Could not send your enquiry. Please try again.');
      }
    });
  }

  // ── Owner actions ─────────────────────────────────────────────────────────
  publish(): void {
    const p = this.property();
    if (!p) return;
    this.actionLoading.set(true);
    this.actionError.set('');
    this.listings.publishProperty(p.id).subscribe({
      next: () => { this.actionLoading.set(false); this.load(); },
      error: (err: HttpErrorResponse) => {
        this.actionLoading.set(false);
        this.actionError.set(err.error?.detail ?? 'Could not publish.');
      }
    });
  }

  archive(): void {
    const p = this.property();
    if (!p) return;
    this.actionLoading.set(true);
    this.actionError.set('');
    this.listings.archiveProperty(p.id).subscribe({
      next: () => { this.actionLoading.set(false); this.load(); },
      error: () => { this.actionLoading.set(false); this.actionError.set('Could not archive.'); }
    });
  }

  formatPrice(): string {
    const p = this.property();
    if (!p) return '';
    const formatted = new Intl.NumberFormat('en-IN', {
      style: 'currency', currency: p.currency, maximumFractionDigits: 0
    }).format(p.price);
    return p.listingType === 'Rent' && p.rentPeriod
      ? `${formatted}/${p.rentPeriod === 'Monthly' ? 'month' : 'year'}`
      : formatted;
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
