import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Admin, AdminPlanPrice } from '../../../core/services/admin/admin';
import { Auth } from '../../../core/services/auth';
import { Toasts } from '../../../core/services/toast';
import { yearlySavingPercent } from '../../../core/services/plans';

/** Editable copy of one plan's price, so typing does not change the saved values. */
interface PriceDraft {
  monthlyPriceInr: number;
  yearlyPriceInr: number;
  offerLabel: string;
}

@Component({
  selector: 'app-admin-pricing',
  imports: [FormsModule, DatePipe, DecimalPipe],
  templateUrl: './admin-pricing.html',
  styleUrl: './admin-pricing.css'
})
export class AdminPricing implements OnInit {

  private admin = inject(Admin);
  private toasts = inject(Toasts);
  readonly auth = inject(Auth);

  readonly offerMaxLength = 60;

  prices = signal<AdminPlanPrice[]>([]);
  drafts = signal<Record<string, PriceDraft>>({});
  isLoading = signal(true);
  loadFailed = signal(false);
  savingPlan = signal<string | null>(null);

  ngOnInit(): void {
    this.admin.getPlanPrices().subscribe({
      next: (prices) => {
        this.prices.set(prices);
        this.drafts.set(Object.fromEntries(prices.map(p => [p.planName, this.toDraft(p)])));
        this.isLoading.set(false);
      },
      error: () => {
        this.loadFailed.set(true);
        this.isLoading.set(false);
      }
    });
  }

  /** Only admins can change prices; moderators get a read-only view. */
  canEdit(price: AdminPlanPrice): boolean {
    return price.isEditable && this.auth.isAdmin();
  }

  draft(planName: string): PriceDraft {
    return this.drafts()[planName];
  }

  update(planName: string, field: keyof PriceDraft, value: string | number): void {
    this.drafts.update(all => ({ ...all, [planName]: { ...all[planName], [field]: value } }));
  }

  isDirty(price: AdminPlanPrice): boolean {
    const draft = this.draft(price.planName);

    return Number(draft.monthlyPriceInr) !== price.monthlyPriceInr
      || Number(draft.yearlyPriceInr) !== price.yearlyPriceInr
      || (draft.offerLabel.trim() || null) !== price.offerLabel;
  }

  /** Same rules the server enforces, checked first so the admin gets instant feedback. */
  validationError(planName: string): string | null {
    const draft = this.draft(planName);
    const monthly = Number(draft.monthlyPriceInr);
    const yearly = Number(draft.yearlyPriceInr);

    if (!Number.isInteger(monthly) || !Number.isInteger(yearly)) {
      return 'Use whole rupees.';
    }

    if (monthly <= 0 || yearly <= 0) {
      return 'Monthly and yearly prices must both be above zero.';
    }

    if (draft.offerLabel.length > this.offerMaxLength) {
      return `Keep the offer label to ${this.offerMaxLength} characters.`;
    }

    return null;
  }

  /** A note about yearly value, so a seasonal monthly discount does not make yearly look bad by accident. */
  yearlyHint(planName: string): string {
    const draft = this.draft(planName);
    const monthly = Number(draft.monthlyPriceInr);
    const yearly = Number(draft.yearlyPriceInr);
    const saving = yearlySavingPercent({ monthlyPriceInr: monthly, yearlyPriceInr: yearly });

    if (monthly <= 0 || yearly <= 0) {
      return '';
    }

    return saving > 0
      ? `Yearly saves ${saving}% compared with 12 monthly payments.`
      : 'Yearly costs the same as or more than 12 monthly payments. Members will pick monthly.';
  }

  reset(price: AdminPlanPrice): void {
    this.drafts.update(all => ({ ...all, [price.planName]: this.toDraft(price) }));
  }

  save(price: AdminPlanPrice): void {
    if (this.validationError(price.planName) || !this.isDirty(price)) {
      return;
    }

    const draft = this.draft(price.planName);
    this.savingPlan.set(price.planName);

    this.admin.updatePlanPrice(price.planName, {
      monthlyPriceInr: Number(draft.monthlyPriceInr),
      yearlyPriceInr: Number(draft.yearlyPriceInr),
      offerLabel: draft.offerLabel.trim() || null
    }).subscribe({
      next: (saved) => {
        this.prices.update(list => list.map(p => p.planName === saved.planName ? saved : p));
        this.reset(saved);
        this.savingPlan.set(null);
        this.toasts.success(`${saved.planName} prices updated. The pricing page shows them now.`);
      },
      error: (error: HttpErrorResponse) => {
        this.savingPlan.set(null);
        this.toasts.error(error.error?.detail ?? 'The prices could not be saved. Try again.');
      }
    });
  }

  private toDraft(price: AdminPlanPrice): PriceDraft {
    return {
      monthlyPriceInr: price.monthlyPriceInr,
      yearlyPriceInr: price.yearlyPriceInr,
      offerLabel: price.offerLabel ?? ''
    };
  }
}
