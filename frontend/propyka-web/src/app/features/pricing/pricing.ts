import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Auth } from '../../core/services/auth';
import { BillingPeriod, PlanInfo, Plans, yearlySavingPercent } from '../../core/services/plans';
import { Topbar } from '../../shared/topbar/topbar';

@Component({
  selector: 'app-pricing',
  imports: [RouterLink, DecimalPipe, Topbar],
  templateUrl: './pricing.html',
  styleUrl: './pricing.css'
})
export class Pricing implements OnInit {

  readonly auth = inject(Auth);
  private plans = inject(Plans);

  /** Upgrades are handled by hand until a payment provider is connected. */
  readonly upgradeEmail = 'billing@propyka.example';

  catalogue = signal<PlanInfo[]>([]);
  isLoading = signal(true);
  loadFailed = signal(false);
  billing = signal<BillingPeriod>('monthly');

  /** Best yearly saving across paid plans, for the "Save x%" hint on the switch. */
  bestYearlySaving = computed(() =>
    Math.max(0, ...this.catalogue().map(plan => yearlySavingPercent(plan)))
  );

  /** The first seasonal offer an admin has set, shown as a banner. */
  offer = computed(() => this.catalogue().find(plan => plan.offerLabel)?.offerLabel ?? null);

  ngOnInit(): void {
    this.plans.list().subscribe({
      next: (plans) => {
        this.catalogue.set(plans);
        this.isLoading.set(false);
      },
      error: () => {
        this.loadFailed.set(true);
        this.isLoading.set(false);
      }
    });
  }

  isProPlan(plan: PlanInfo): boolean {
    return plan.name === 'Pro';
  }

  /** True when this card describes the plan the signed-in user is already on. */
  isCurrent(plan: PlanInfo): boolean {
    return this.auth.isAuthenticated() && (this.isProPlan(plan) === this.auth.isPro());
  }

  isFree(plan: PlanInfo): boolean {
    return plan.monthlyPriceInr === 0 && plan.yearlyPriceInr === 0;
  }

  /** Price for the selected billing period. */
  price(plan: PlanInfo): number {
    return this.billing() === 'yearly' ? plan.yearlyPriceInr : plan.monthlyPriceInr;
  }

  /** Yearly price spread over twelve months, for comparison. */
  perMonthOnYearly(plan: PlanInfo): number {
    return Math.round(plan.yearlyPriceInr / 12);
  }

  saving(plan: PlanInfo): number {
    return yearlySavingPercent(plan);
  }

  upgradeHref(plan: PlanInfo): string {
    const period = this.billing() === 'yearly' ? 'yearly' : 'monthly';
    const subject = `Upgrade to ${plan.name} (${period}, ₹${this.price(plan)})`;

    return `mailto:${this.upgradeEmail}?subject=${encodeURIComponent(subject)}`;
  }
}
