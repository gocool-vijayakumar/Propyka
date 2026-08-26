import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

// ─── Enums (mirror backend) ───────────────────────────────────────────────────

export type ListingType = 'Sale' | 'Rent';
export type PropertyKind = 'Apartment' | 'House' | 'Villa' | 'Plot' | 'Commercial' | 'Warehouse';
export type ListingStatus = 'Draft' | 'Published' | 'UnderOffer' | 'Sold' | 'Rented' | 'Archived';
export type RentPeriod = 'Monthly' | 'Yearly';
export type FurnishingLevel = 'Unfurnished' | 'SemiFurnished' | 'Furnished';
export type EnquiryStatus = 'New' | 'Responded' | 'Closed';
export type PropertySort = 'Newest' | 'PriceAsc' | 'PriceDesc';

// ─── Shared ───────────────────────────────────────────────────────────────────

export interface PagedResult<T> {
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
  items: T[];
}

// ─── Property responses ───────────────────────────────────────────────────────

export interface PropertySummary {
  id: string;
  slug: string;
  title: string;
  listingType: ListingType;
  kind: PropertyKind;
  status: ListingStatus;
  price: number;
  currency: string;
  rentPeriod: RentPeriod | null;
  bedrooms: number;
  bathrooms: number;
  areaSqFt: number;
  city: string;
  locality: string | null;
  primaryImageUrl: string | null;
  publishedAt: string | null;
}

export interface PropertyDetail {
  id: string;
  slug: string;
  title: string;
  description: string;
  listingType: ListingType;
  kind: PropertyKind;
  status: ListingStatus;
  price: number;
  currency: string;
  rentPeriod: RentPeriod | null;
  bedrooms: number;
  bathrooms: number;
  areaSqFt: number;
  plotAreaSqFt: number | null;
  yearBuilt: number | null;
  furnishing: FurnishingLevel | null;
  viewCount: number;
  address: PropertyAddress;
  images: PropertyImage[];
  amenities: Amenity[];
  owner: PropertyOwner;
  createdAt: string;
  publishedAt: string | null;
}

export interface PropertyAddress {
  line1: string;
  line2: string | null;
  locality: string | null;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  latitude: number | null;
  longitude: number | null;
}

export interface PropertyImage {
  id: string;
  storageKey: string;
  url: string;
  caption: string | null;
  sortOrder: number;
  isPrimary: boolean;
}

export interface PropertyOwner {
  id: string;
  fullName: string;
}

export interface Amenity {
  id: number;
  name: string;
  slug: string;
}

export interface CreatedProperty {
  id: string;
  slug: string;
}

// ─── Enquiry responses ────────────────────────────────────────────────────────

export interface Enquiry {
  id: string;
  propertyId: string;
  propertyTitle: string;
  propertySlug: string;
  name: string;
  email: string;
  phone: string | null;
  message: string;
  status: EnquiryStatus;
  createdAt: string;
}

// ─── Requests ─────────────────────────────────────────────────────────────────

export interface AddressRequest {
  line1: string;
  line2?: string;
  locality?: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  latitude?: number;
  longitude?: number;
}

export interface CreatePropertyRequest {
  title: string;
  description?: string;
  listingType: ListingType;
  kind: PropertyKind;
  price: number;
  rentPeriod?: RentPeriod;
  bedrooms: number;
  bathrooms: number;
  areaSqFt: number;
  plotAreaSqFt?: number;
  yearBuilt?: number;
  furnishing?: FurnishingLevel;
  address: AddressRequest;
  amenityIds?: number[];
}

export interface CreateEnquiryRequest {
  name: string;
  email: string;
  phone?: string;
  message: string;
}

export interface PropertySearchQuery {
  search?: string;
  city?: string;
  listingType?: ListingType;
  kind?: PropertyKind;
  minPrice?: number;
  maxPrice?: number;
  minBedrooms?: number;
  amenityIds?: number[];
  sort?: PropertySort;
  page?: number;
  pageSize?: number;
}

// ─── Service ──────────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class ListingsService {

  private http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  // ── Amenities ────────────────────────────────────────────────────────────────
  getAmenities(): Observable<Amenity[]> {
    return this.http.get<Amenity[]>(`${this.base}/api/amenities`);
  }

  // ── Public property search ────────────────────────────────────────────────────
  searchProperties(query: PropertySearchQuery): Observable<PagedResult<PropertySummary>> {
    let params = new HttpParams();

    if (query.search)       params = params.set('search', query.search);
    if (query.city)         params = params.set('city', query.city);
    if (query.listingType)  params = params.set('listingType', query.listingType);
    if (query.kind)         params = params.set('kind', query.kind);
    if (query.minPrice != null) params = params.set('minPrice', query.minPrice);
    if (query.maxPrice != null) params = params.set('maxPrice', query.maxPrice);
    if (query.minBedrooms != null) params = params.set('minBedrooms', query.minBedrooms);
    if (query.sort)         params = params.set('sort', query.sort);
    if (query.page)         params = params.set('page', query.page);
    if (query.pageSize)     params = params.set('pageSize', query.pageSize);
    if (query.amenityIds?.length) {
      query.amenityIds.forEach(id => params = params.append('amenityIds', id));
    }

    return this.http.get<PagedResult<PropertySummary>>(`${this.base}/api/properties`, { params });
  }

  getPropertyBySlug(slug: string): Observable<PropertyDetail> {
    return this.http.get<PropertyDetail>(`${this.base}/api/properties/${slug}`);
  }

  // ── Owner endpoints ───────────────────────────────────────────────────────────
  getMyListings(page = 1, pageSize = 20): Observable<PagedResult<PropertySummary>> {
    return this.http.get<PagedResult<PropertySummary>>(
      `${this.base}/api/properties/mine`,
      { params: { page, pageSize } }
    );
  }

  getOwnedProperty(id: string): Observable<PropertyDetail> {
    return this.http.get<PropertyDetail>(`${this.base}/api/properties/mine/${id}`);
  }

  createProperty(request: CreatePropertyRequest): Observable<CreatedProperty> {
    return this.http.post<CreatedProperty>(`${this.base}/api/properties`, request);
  }

  updateProperty(id: string, request: CreatePropertyRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/api/properties/${id}`, request);
  }

  publishProperty(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/api/properties/${id}/publish`, {});
  }

  archiveProperty(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/api/properties/${id}/archive`, {});
  }

  // ── Images ───────────────────────────────────────────────────────────────────
  uploadImage(propertyId: string, file: File): Observable<PropertyImage> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<PropertyImage>(`${this.base}/api/properties/${propertyId}/images`, form);
  }

  updateImage(propertyId: string, imageId: string, caption: string | null, sortOrder: number, isPrimary: boolean): Observable<void> {
    return this.http.put<void>(
      `${this.base}/api/properties/${propertyId}/images/${imageId}`,
      { caption, sortOrder, isPrimary }
    );
  }

  deleteImage(propertyId: string, imageId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/properties/${propertyId}/images/${imageId}`);
  }

  // ── Enquiries ─────────────────────────────────────────────────────────────────
  createEnquiry(propertyId: string, request: CreateEnquiryRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(
      `${this.base}/api/properties/${propertyId}/enquiries`,
      request
    );
  }

  getReceivedEnquiries(status?: EnquiryStatus, page = 1, pageSize = 20): Observable<PagedResult<Enquiry>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (status) params = params.set('status', status);
    return this.http.get<PagedResult<Enquiry>>(`${this.base}/api/enquiries/received`, { params });
  }

  getSentEnquiries(page = 1, pageSize = 20): Observable<PagedResult<Enquiry>> {
    return this.http.get<PagedResult<Enquiry>>(
      `${this.base}/api/enquiries/sent`,
      { params: { page, pageSize } }
    );
  }

  setEnquiryStatus(id: string, status: EnquiryStatus): Observable<void> {
    return this.http.put<void>(`${this.base}/api/enquiries/${id}/status`, { status });
  }

  // ── Favourites ────────────────────────────────────────────────────────────────
  getFavourites(page = 1, pageSize = 20): Observable<PagedResult<PropertySummary>> {
    return this.http.get<PagedResult<PropertySummary>>(
      `${this.base}/api/favorites`,
      { params: { page, pageSize } }
    );
  }

  getFavouriteIds(): Observable<string[]> {
    return this.http.get<string[]>(`${this.base}/api/favorites/ids`);
  }

  addFavourite(propertyId: string): Observable<void> {
    return this.http.put<void>(`${this.base}/api/favorites/${propertyId}`, {});
  }

  removeFavourite(propertyId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/favorites/${propertyId}`);
  }
}
