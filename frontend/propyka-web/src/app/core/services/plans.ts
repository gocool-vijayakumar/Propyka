import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

/** One plan from GET /api/plans. The server owns these numbers. */
export interface PlanInfo {
  readonly name: string;
  readonly tagline: string;
  readonly monthlyPriceInr: number;
  readonly adsEnabled: boolean;
  readonly maxActiveListings: number;
  readonly maxPhotosPerListing: number;
  readonly maxSavedListings: number;
  readonly features: readonly string[];
}

@Injectable({ providedIn: 'root' })
export class Plans {

  private http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  list(): Observable<PlanInfo[]> {
    return this.http.get<PlanInfo[]>(`${this.apiUrl}/api/plans`);
  }
}
