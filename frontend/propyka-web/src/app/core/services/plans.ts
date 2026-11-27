import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

/** One plan from GET /api/plans. The server owns these numbers. */
export interface PlanInfo {
  readonly name: string;
  readonly tagline: string;
  readonly monthlyPriceInr: number;
  readonly yearlyPriceInr: number;
  /** Seasonal banner set by an admin, such as "Diwali offer". */
  readonly offerLabel: string | null;
  readonly adsEnabled: boolean;
  readonly maxActiveListings: number;
  readonly maxPhotosPerListing: number;
  readonly maxSavedListings: number;
  readonly features: readonly string[];
}

export type BillingPeriod = 'monthly' | 'yearly';

/**
 * Percentage saved by paying yearly instead of twelve monthly payments.
 * Zero when yearly is not cheaper, for example during a monthly-only offer.
 */
export function yearlySavingPercent(plan: Pick<PlanInfo, 'monthlyPriceInr' | 'yearlyPriceInr'>): number {
  const twelveMonths = plan.monthlyPriceInr * 12;

  if (twelveMonths <= 0 || plan.yearlyPriceInr >= twelveMonths) {
    return 0;
  }

  return Math.round((1 - plan.yearlyPriceInr / twelveMonths) * 100);
}

@Injectable({ providedIn: 'root' })
export class Plans {

  private http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  list(): Observable<PlanInfo[]> {
    return this.http.get<PlanInfo[]>(`${this.apiUrl}/api/plans`);
  }
}
