import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Auth } from '../../../core/services/auth';
import { ThemeToggle } from '../../../shared/theme-toggle/theme-toggle';
import {
  ListingsService,
  PropertyDetail,
  PropertyImage
} from '../../../core/services/listings/listings.service';

@Component({
  selector: 'app-listing-images',
  standalone: true,
  imports: [RouterLink, ThemeToggle],
  templateUrl: './listing-images.html',
  styleUrl: './listing-images.css'
})
export class ListingImages implements OnInit {

  private listings = inject(ListingsService);
  readonly auth = inject(Auth);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  property = signal<PropertyDetail | null>(null);
  isLoading = signal(true);
  errorMessage = signal('');
  uploadError = signal('');
  isUploading = signal(false);
  deletingId = signal<string | null>(null);
  settingPrimaryId = signal<string | null>(null);

  initial = () => {
    const p = this.auth.profile();
    return (p?.firstName || p?.email || 'P').charAt(0).toUpperCase();
  };

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.loadProperty(id);
  }

  private loadProperty(id: string): void {
    this.isLoading.set(true);
    this.listings.getOwnedProperty(id).subscribe({
      next: (p) => { this.property.set(p); this.isLoading.set(false); },
      error: () => { this.errorMessage.set('Could not load this listing.'); this.isLoading.set(false); }
    });
  }

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = input.files;
    if (!files || files.length === 0) return;

    const p = this.property();
    if (!p) return;

    const file = files[0];
    const maxMb = 12;

    if (file.size > maxMb * 1024 * 1024) {
      this.uploadError.set(`Image must be under ${maxMb} MB.`);
      return;
    }

    if (!file.type.startsWith('image/')) {
      this.uploadError.set('Please choose an image file (JPG, PNG, WebP, etc).');
      return;
    }

    this.isUploading.set(true);
    this.uploadError.set('');

    this.listings.uploadImage(p.id, file).subscribe({
      next: () => { this.isUploading.set(false); this.loadProperty(p.id); },
      error: (err) => {
        this.isUploading.set(false);
        this.uploadError.set(err.error?.detail ?? 'Upload failed. Try again.');
      }
    });

    input.value = '';
  }

  setPrimary(image: PropertyImage): void {
    const p = this.property();
    if (!p || this.settingPrimaryId()) return;
    this.settingPrimaryId.set(image.id);
    this.listings.updateImage(p.id, image.id, image.caption, image.sortOrder, true).subscribe({
      next: () => { this.settingPrimaryId.set(null); this.loadProperty(p.id); },
      error: () => this.settingPrimaryId.set(null)
    });
  }

  deleteImage(imageId: string): void {
    const p = this.property();
    if (!p || this.deletingId()) return;
    this.deletingId.set(imageId);
    this.listings.deleteImage(p.id, imageId).subscribe({
      next: () => { this.deletingId.set(null); this.loadProperty(p.id); },
      error: () => this.deletingId.set(null)
    });
  }

  doneAndPublish(): void {
    const p = this.property();
    if (!p) return;
    if (p.status === 'Draft') {
      this.listings.publishProperty(p.id).subscribe({
        next: () => this.router.navigate(['/listings', p.slug]),
        error: (err) => {
          this.errorMessage.set(err.error?.detail ?? 'Could not publish. Add images first if needed.');
        }
      });
    } else {
      this.router.navigate(['/listings', p.slug]);
    }
  }

  logout(): void { this.auth.logout(); this.router.navigate(['/login']); }
}
