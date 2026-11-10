import { Component, OnInit, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Auth } from '../../core/services/auth';
import { PlanInfo, Plans } from '../../core/services/plans';
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

  upgradeHref(): string {
    return `mailto:${this.upgradeEmail}?subject=${encodeURIComponent('Upgrade to Pro')}`;
  }
}
