import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Auth } from '../../../core/services/auth';
import { ThemeToggle } from '../../../shared/theme-toggle/theme-toggle';

interface PasswordRule {
  label: string;
  met: boolean;
}

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink, ThemeToggle],
  templateUrl: './register.html',
  styleUrl: './register.css'
})
export class Register {

  private auth = inject(Auth);
  private router = inject(Router);

  firstName = '';
  lastName = '';
  email = '';
  password = '';
  confirmPassword = '';

  showPassword = false;
  isLoading = false;
  errorMessage = '';

  /** Mirrors options.Password in Program.cs. Keep the two in sync. */
  ruleList(): PasswordRule[] {
    const value = this.password;

    return [
      { label: 'At least 8 characters', met: value.length >= 8 },
      { label: 'One uppercase letter', met: /[A-Z]/.test(value) },
      { label: 'One lowercase letter', met: /[a-z]/.test(value) },
      { label: 'One number', met: /\d/.test(value) },
      { label: 'One symbol', met: /[^A-Za-z0-9]/.test(value) }
    ];
  }

  passwordMeetsPolicy(): boolean {
    return this.ruleList().every((rule) => rule.met);
  }

  passwordsMatch(): boolean {
    return this.password === this.confirmPassword;
  }

  register(): void {
    this.errorMessage = '';

    if (!this.firstName.trim() || !this.lastName.trim()) {
      this.errorMessage = 'Enter your first and last name.';
      return;
    }

    if (!this.email.trim()) {
      this.errorMessage = 'Enter the email address for your account.';
      return;
    }

    if (!this.passwordMeetsPolicy()) {
      this.errorMessage = 'Your password does not meet all the requirements listed above.';
      return;
    }

    if (!this.passwordsMatch()) {
      this.errorMessage = 'Both passwords must be the same.';
      return;
    }

    this.isLoading = true;

    this.auth.register({
      firstName: this.firstName.trim(),
      lastName: this.lastName.trim(),
      email: this.email.trim(),
      password: this.password
    }).subscribe({
      next: () => this.signInAfterRegister(),

      error: (error: HttpErrorResponse) => {
        this.isLoading = false;
        this.errorMessage = this.describe(error);
      }
    });
  }

  /** Registration returns no tokens, so sign the new user straight in. */
  private signInAfterRegister(): void {
    this.auth.login({
      email: this.email.trim(),
      password: this.password
    }).subscribe({
      next: () => {
        this.isLoading = false;
        this.router.navigate(['/home']);
      },

      error: () => {
        this.isLoading = false;
        this.router.navigate(['/login']);
      }
    });
  }

  /**
   * ASP.NET Identity returns a ValidationProblemDetails whose `errors`
   * dictionary is keyed by IdentityError.Code.
   */
  private describe(error: HttpErrorResponse): string {
    if (error.status === 0) {
      return 'Cannot reach the Propyka server. Check that the API is running.';
    }

    const errors = error.error?.errors as Record<string, string[]> | undefined;

    if (errors) {
      const codes = Object.keys(errors);

      if (codes.some((code) => code.startsWith('Duplicate'))) {
        return 'An account already exists for that email. Sign in instead.';
      }

      const messages = Object.values(errors).flat();

      if (messages.length) {
        return messages.join(' ');
      }
    }

    return 'We could not create your account. Check your details and try again.';
  }
}