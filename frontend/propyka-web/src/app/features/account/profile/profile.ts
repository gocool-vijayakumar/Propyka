import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Auth } from '../../../core/services/auth';
import { Toasts } from '../../../core/services/toast';
import { environment } from '../../../../environments/environment';
import { Topbar } from '../../../shared/topbar/topbar';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [FormsModule, RouterLink, Topbar],
  templateUrl: './profile.html',
  styleUrl: './profile.css'
})
export class Profile implements OnInit {

  readonly auth = inject(Auth);
  private router = inject(Router);
  private toasts = inject(Toasts);
  private http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  firstName = signal('');
  lastName = signal('');
  isSaving = signal(false);
  saveError = signal('');
  saveSuccess = signal(false);

  // Change password section
  currentPassword = signal('');
  newPassword = signal('');
  confirmPassword = signal('');
  isChangingPassword = signal(false);
  passwordError = signal('');

  // Delete account section
  deletePassword = signal('');
  isDeleting = signal(false);
  deleteError = signal('');
  showDeleteConfirm = signal(false);

  initial = () => {
    const p = this.auth.profile();
    return (p?.firstName || p?.email || 'P').charAt(0).toUpperCase();
  };

  ngOnInit(): void {
    const p = this.auth.profile();
    if (p) {
      this.firstName.set(p.firstName);
      this.lastName.set(p.lastName);
    } else {
      this.auth.loadProfile().subscribe({
        next: (p) => {
          this.firstName.set(p.firstName);
          this.lastName.set(p.lastName);
        }
      });
    }
  }

  saveProfile(): void {
    if (!this.firstName().trim() || !this.lastName().trim()) {
      this.saveError.set('First and last name are required.');
      return;
    }
    this.isSaving.set(true);
    this.saveError.set('');
    this.saveSuccess.set(false);

    this.http.put(`${this.apiUrl}/api/account/me`, {
      firstName: this.firstName().trim(),
      lastName: this.lastName().trim()
    }).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.saveSuccess.set(true);
        this.toasts.success('Your name has been updated.');
        this.auth.loadProfile().subscribe();
      },
      error: (err: HttpErrorResponse) => {
        this.isSaving.set(false);
        this.saveError.set(err.error?.detail ?? 'Could not update profile.');
      }
    });
  }

  changePassword(): void {
    this.passwordError.set('');

    if (!this.currentPassword() || !this.newPassword()) {
      this.passwordError.set('Enter your current password and a new one.');
      return;
    }
    if (this.newPassword() !== this.confirmPassword()) {
      this.passwordError.set('The new passwords do not match.');
      return;
    }

    this.isChangingPassword.set(true);

    this.auth.changePassword(this.currentPassword(), this.newPassword()).subscribe({
      next: () => {
        // Signing out here means every device re-authenticates with the new
        // password, and the form never keeps the old values on screen.
        this.toasts.success('Password updated. Please sign in with your new password.');
        this.auth.logout();
        this.router.navigate(['/login']);
      },
      error: (err: HttpErrorResponse) => {
        this.isChangingPassword.set(false);
        const errors = err.error?.errors as Record<string, string[]> | undefined;
        this.passwordError.set(
          errors ? Object.values(errors).flat().join(' ')
          : err.error?.detail ?? 'Could not change your password.'
        );
      }
    });
  }

  deleteAccount(): void {
    if (!this.deletePassword().trim()) {
      this.deleteError.set('Enter your current password to confirm deletion.');
      return;
    }
    this.isDeleting.set(true);
    this.deleteError.set('');

    this.auth.deleteOwnAccount(this.deletePassword()).subscribe({
      next: () => {
        this.auth.logout();
        this.router.navigate(['/login']);
      },
      error: (err: HttpErrorResponse) => {
        this.isDeleting.set(false);
        const errors = err.error?.errors as Record<string, string[]> | undefined;
        this.deleteError.set(
          errors ? Object.values(errors).flat().join(' ')
          : err.error?.detail ?? 'Could not delete account.'
        );
      }
    });
  }

  logout(): void { this.auth.logout(); this.router.navigate(['/login']); }
}
