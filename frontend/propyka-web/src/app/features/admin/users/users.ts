import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import {
  Admin,
  AdminUser,
  UserStatusFilter
} from '../../../core/services/admin/admin';
import { Auth } from '../../../core/services/auth';

type PendingAction = 'trash' | 'restore';

@Component({
  selector: 'app-admin-users',
  imports: [DatePipe],
  templateUrl: './users.html',
  styleUrl: './users.css'
})
export class AdminUsers implements OnInit {

  private admin = inject(Admin);
  private auth = inject(Auth);

  readonly allRoles = ['Admin', 'Moderator', 'Member'];

  users = signal<AdminUser[]>([]);
  isLoading = signal(true);
  errorMessage = signal('');

  search = signal('');
  status = signal<UserStatusFilter>('active');
  page = signal(1);
  totalPages = signal(1);
  total = signal(0);

  /** Confirmation dialog state. */
  pendingUser = signal<AdminUser | null>(null);
  pendingAction = signal<PendingAction>('trash');
  isWorking = signal(false);

  /** Role editor state. */
  rolesUser = signal<AdminUser | null>(null);
  draftRoles = signal<string[]>([]);

  currentUserId = computed(() => this.auth.profile()?.id ?? '');

  private searchInput$ = new Subject<string>();

  constructor() {
    this.searchInput$
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((value) => {
        this.search.set(value);
        this.page.set(1);
        this.load();
      });
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');

    this.admin.getUsers({
      search: this.search(),
      status: this.status(),
      page: this.page(),
      pageSize: 20
    }).subscribe({
      next: (result) => {
        this.users.set(result.items);
        this.totalPages.set(result.totalPages || 1);
        this.total.set(result.total);
        this.isLoading.set(false);
      },

      error: (error: HttpErrorResponse) => {
        this.isLoading.set(false);
        this.errorMessage.set(this.describe(error));
      }
    });
  }

  onSearchInput(value: string): void {
    this.searchInput$.next(value);
  }

  setStatus(status: UserStatusFilter): void {
    if (this.status() === status) {
      return;
    }

    this.status.set(status);
    this.page.set(1);
    this.load();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) {
      return;
    }

    this.page.set(page);
    this.load();
  }

  /** An admin cannot action their own account — the server refuses too. */
  canAction(user: AdminUser): boolean {
    return user.id !== this.currentUserId();
  }

  confirm(user: AdminUser, action: PendingAction): void {
    this.pendingUser.set(user);
    this.pendingAction.set(action);
    this.errorMessage.set('');
  }

  cancelConfirm(): void {
    this.pendingUser.set(null);
  }

  runPendingAction(): void {
    const user = this.pendingUser();

    if (!user) {
      return;
    }

    this.isWorking.set(true);

    const request = this.pendingAction() === 'trash'
      ? this.admin.trashUser(user.id)
      : this.admin.restoreUser(user.id);

    request.subscribe({
      next: () => {
        this.isWorking.set(false);
        this.pendingUser.set(null);
        this.load();
      },

      error: (error: HttpErrorResponse) => {
        this.isWorking.set(false);
        this.pendingUser.set(null);
        this.errorMessage.set(this.describe(error));
      }
    });
  }

  editRoles(user: AdminUser): void {
    this.rolesUser.set(user);
    this.draftRoles.set([...user.roles]);
    this.errorMessage.set('');
  }

  cancelRoles(): void {
    this.rolesUser.set(null);
  }

  toggleRole(role: string): void {
    const roles = this.draftRoles();

    this.draftRoles.set(
      roles.includes(role)
        ? roles.filter((r) => r !== role)
        : [...roles, role]
    );
  }

  saveRoles(): void {
    const user = this.rolesUser();

    if (!user) {
      return;
    }

    this.isWorking.set(true);

    this.admin.setRoles(user.id, this.draftRoles()).subscribe({
      next: () => {
        this.isWorking.set(false);
        this.rolesUser.set(null);
        this.load();
      },

      error: (error: HttpErrorResponse) => {
        this.isWorking.set(false);
        this.rolesUser.set(null);
        this.errorMessage.set(this.describe(error));
      }
    });
  }

  initial(user: AdminUser): string {
    return (user.firstName || user.email).charAt(0).toUpperCase();
  }

  private describe(error: HttpErrorResponse): string {
    if (error.status === 0) {
      return 'Cannot reach the Propyka server. Check that the API is running.';
    }

    if (error.status === 403) {
      return 'Your account does not have permission for that.';
    }

    // ProblemDetails puts the reason in `detail`.
    return error.error?.detail
      ?? 'Something went wrong. Try again.';
  }
}