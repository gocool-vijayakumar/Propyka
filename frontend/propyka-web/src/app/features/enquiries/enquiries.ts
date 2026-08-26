import { Component, OnInit, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { Auth } from '../../core/services/auth';
import { ThemeToggle } from '../../shared/theme-toggle/theme-toggle';
import {
  ListingsService,
  Enquiry,
  EnquiryStatus
} from '../../core/services/listings/listings.service';

type Tab = 'received' | 'sent';

@Component({
  selector: 'app-enquiries',
  standalone: true,
  imports: [RouterLink, DatePipe, ThemeToggle],
  templateUrl: './enquiries.html',
  styleUrl: './enquiries.css'
})
export class Enquiries implements OnInit {

  private listings = inject(ListingsService);
  readonly auth = inject(Auth);
  private router = inject(Router);

  tab = signal<Tab>('received');
  enquiries = signal<Enquiry[]>([]);
  isLoading = signal(true);
  errorMessage = signal('');
  total = signal(0);
  page = signal(1);
  totalPages = signal(1);
  statusFilter = signal<EnquiryStatus | undefined>(undefined);
  actionLoadingId = signal<string | null>(null);

  initial = () => {
    const p = this.auth.profile();
    return (p?.firstName || p?.email || 'P').charAt(0).toUpperCase();
  };

  readonly statusOptions: { value: EnquiryStatus | undefined; label: string }[] = [
    { value: undefined, label: 'All' },
    { value: 'New', label: 'New' },
    { value: 'Responded', label: 'Responded' },
    { value: 'Closed', label: 'Closed' }
  ];

  ngOnInit(): void { this.load(); }

  setTab(t: Tab): void {
    if (this.tab() === t) return;
    this.tab.set(t);
    this.page.set(1);
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');

    const req = this.tab() === 'received'
      ? this.listings.getReceivedEnquiries(this.statusFilter(), this.page())
      : this.listings.getSentEnquiries(this.page());

    req.subscribe({
      next: (r) => {
        this.enquiries.set(r.items);
        this.total.set(r.total);
        this.totalPages.set(r.totalPages || 1);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load enquiries.');
        this.isLoading.set(false);
      }
    });
  }

  setStatusFilter(s: EnquiryStatus | undefined): void {
    this.statusFilter.set(s);
    this.page.set(1);
    this.load();
  }

  setStatus(enquiryId: string, status: EnquiryStatus): void {
    this.actionLoadingId.set(enquiryId);
    this.listings.setEnquiryStatus(enquiryId, status).subscribe({
      next: () => { this.actionLoadingId.set(null); this.load(); },
      error: () => this.actionLoadingId.set(null)
    });
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages()) return;
    this.page.set(p);
    this.load();
  }

  logout(): void { this.auth.logout(); this.router.navigate(['/login']); }
}
