import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DecimalPipe, TitleCasePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Auth } from '../../../core/services/auth';
import { ThemeToggle } from '../../../shared/theme-toggle/theme-toggle';
import {
  ListingsService,
  Amenity,
  ListingType,
  PropertyKind,
  RentPeriod,
  FurnishingLevel,
  CreatePropertyRequest
} from '../../../core/services/listings/listings.service';

type FormStep = 'basics' | 'details' | 'location' | 'review';

@Component({
  selector: 'app-listing-form',
  standalone: true,
  imports: [FormsModule, RouterLink, ThemeToggle, DecimalPipe, TitleCasePipe],
  templateUrl: './listing-form.html',
  styleUrl: './listing-form.css'
})
export class ListingForm implements OnInit {

  private listings = inject(ListingsService);
  readonly auth = inject(Auth);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  // Edit mode
  editId = signal<string | null>(null);
  isEdit = computed(() => !!this.editId());

  // Step
  step = signal<FormStep>('basics');
  readonly steps: FormStep[] = ['basics', 'details', 'location', 'review'];
  stepIndex = computed(() => this.steps.indexOf(this.step()));

  // Amenities
  allAmenities = signal<Amenity[]>([]);

  // Form fields — basics
  title = signal('');
  description = signal('');
  listingType = signal<ListingType>('Sale');
  kind = signal<PropertyKind>('Apartment');
  price = signal<number | null>(null);
  rentPeriod = signal<RentPeriod>('Monthly');

  // Form fields — details
  bedrooms = signal(0);
  bathrooms = signal(0);
  areaSqFt = signal<number | null>(null);
  plotAreaSqFt = signal<number | null>(null);
  yearBuilt = signal<number | null>(null);
  furnishing = signal<FurnishingLevel | ''>('');
  selectedAmenityIds = signal<Set<number>>(new Set());

  // Form fields — location
  addrLine1 = signal('');
  addrLine2 = signal('');
  addrLocality = signal('');
  addrCity = signal('');
  addrState = signal('');
  addrPostal = signal('');
  addrCountry = signal('India');
  addrLat = signal<number | null>(null);
  addrLng = signal<number | null>(null);

  // State
  isSubmitting = signal(false);
  errorMessage = signal('');
  stepError = signal('');

  // Options
  readonly listingTypes: ListingType[] = ['Sale', 'Rent'];
  readonly propertyKinds: PropertyKind[] = ['Apartment', 'House', 'Villa', 'Plot', 'Commercial', 'Warehouse'];
  readonly rentPeriods: RentPeriod[] = ['Monthly', 'Yearly'];
  readonly furnishingLevels: FurnishingLevel[] = ['Unfurnished', 'SemiFurnished', 'Furnished'];

  initial = computed(() => {
    const p = this.auth.profile();
    return (p?.firstName || p?.email || 'P').charAt(0).toUpperCase();
  });

  ngOnInit(): void {
    this.listings.getAmenities().subscribe({
      next: (a) => this.allAmenities.set(a),
      error: () => {}
    });

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.editId.set(id);
      this.loadForEdit(id);
    }
  }

  private loadForEdit(id: string): void {
    this.listings.getOwnedProperty(id).subscribe({
      next: (p) => {
        this.title.set(p.title);
        this.description.set(p.description);
        this.listingType.set(p.listingType);
        this.kind.set(p.kind);
        this.price.set(p.price);
        if (p.rentPeriod) this.rentPeriod.set(p.rentPeriod);
        this.bedrooms.set(p.bedrooms);
        this.bathrooms.set(p.bathrooms);
        this.areaSqFt.set(p.areaSqFt);
        this.plotAreaSqFt.set(p.plotAreaSqFt);
        this.yearBuilt.set(p.yearBuilt);
        if (p.furnishing) this.furnishing.set(p.furnishing);
        this.selectedAmenityIds.set(new Set(p.amenities.map(a => a.id)));
        this.addrLine1.set(p.address.line1);
        this.addrLine2.set(p.address.line2 ?? '');
        this.addrLocality.set(p.address.locality ?? '');
        this.addrCity.set(p.address.city);
        this.addrState.set(p.address.state);
        this.addrPostal.set(p.address.postalCode);
        this.addrCountry.set(p.address.country);
        this.addrLat.set(p.address.latitude);
        this.addrLng.set(p.address.longitude);
      },
      error: () => this.errorMessage.set('Could not load this listing for editing.')
    });
  }

  // ── Navigation ────────────────────────────────────────────────────────────
  goNext(): void {
    this.stepError.set('');
    if (!this.validateCurrentStep()) return;
    const idx = this.stepIndex();
    if (idx < this.steps.length - 1) {
      this.step.set(this.steps[idx + 1]);
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  goPrev(): void {
    this.stepError.set('');
    const idx = this.stepIndex();
    if (idx > 0) this.step.set(this.steps[idx - 1]);
  }

  goToStep(s: FormStep): void {
    const targetIdx = this.steps.indexOf(s);
    if (targetIdx <= this.stepIndex()) this.step.set(s);
  }

  private validateCurrentStep(): boolean {
    switch (this.step()) {
      case 'basics':
        if (!this.title().trim()) { this.stepError.set('Title is required.'); return false; }
        if (this.title().trim().length > 160) { this.stepError.set('Title must be 160 characters or fewer.'); return false; }
        if ((this.price() ?? 0) <= 0) { this.stepError.set('Enter a price greater than zero.'); return false; }
        break;
      case 'details':
        if ((this.areaSqFt() ?? 0) <= 0) { this.stepError.set('Enter the property area.'); return false; }
        break;
      case 'location':
        if (!this.addrLine1().trim()) { this.stepError.set('Address line 1 is required.'); return false; }
        if (!this.addrCity().trim()) { this.stepError.set('City is required.'); return false; }
        if (!this.addrState().trim()) { this.stepError.set('State is required.'); return false; }
        if (!this.addrPostal().trim()) { this.stepError.set('Postal code is required.'); return false; }
        break;
    }
    return true;
  }

  // ── Amenities ─────────────────────────────────────────────────────────────
  toggleAmenity(id: number): void {
    const set = new Set(this.selectedAmenityIds());
    if (set.has(id)) set.delete(id); else set.add(id);
    this.selectedAmenityIds.set(set);
  }

  isAmenitySelected(id: number): boolean {
    return this.selectedAmenityIds().has(id);
  }

  // ── Submit ────────────────────────────────────────────────────────────────
  submit(): void {
    this.isSubmitting.set(true);
    this.errorMessage.set('');

    const request: CreatePropertyRequest = {
      title: this.title().trim(),
      description: this.description().trim() || undefined,
      listingType: this.listingType(),
      kind: this.kind(),
      price: this.price()!,
      rentPeriod: this.listingType() === 'Rent' ? this.rentPeriod() : undefined,
      bedrooms: this.bedrooms(),
      bathrooms: this.bathrooms(),
      areaSqFt: this.areaSqFt()!,
      plotAreaSqFt: this.plotAreaSqFt() ?? undefined,
      yearBuilt: this.yearBuilt() ?? undefined,
      furnishing: this.furnishing() || undefined,
      address: {
        line1: this.addrLine1().trim(),
        line2: this.addrLine2().trim() || undefined,
        locality: this.addrLocality().trim() || undefined,
        city: this.addrCity().trim(),
        state: this.addrState().trim(),
        postalCode: this.addrPostal().trim(),
        country: this.addrCountry().trim(),
        latitude: this.addrLat() ?? undefined,
        longitude: this.addrLng() ?? undefined
      },
      amenityIds: this.selectedAmenityIds().size > 0
        ? Array.from(this.selectedAmenityIds())
        : undefined
    };

    const id = this.editId();

    const onError = (err: HttpErrorResponse) => {
      this.isSubmitting.set(false);
      const errors = err.error?.errors as Record<string, string[]> | undefined;
      this.errorMessage.set(
        errors
          ? Object.values(errors).flat().join(' ')
          : (err.error?.detail ?? 'Could not save listing. Check your details and try again.')
      );
    };

    if (id) {
      // Edit: returns Observable<void>
      this.listings.updateProperty(id, request).subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.router.navigate(['/listings', 'mine']);
        },
        error: onError
      });
    } else {
      // Create: returns Observable<CreatedProperty>
      this.listings.createProperty(request).subscribe({
        next: (created) => {
          this.isSubmitting.set(false);
          this.router.navigate(['/listings', created.id, 'images']);
        },
        error: onError
      });
    }
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
