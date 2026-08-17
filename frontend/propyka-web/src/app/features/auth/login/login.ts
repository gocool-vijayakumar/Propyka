import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Auth } from '../../../core/services/auth';
import { ThemeToggle } from '../../../shared/theme-toggle/theme-toggle';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink, ThemeToggle],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login {

  private auth = inject(Auth);
  private router = inject(Router);

  email = '';
  password = '';

  isLoading = false;
  errorMessage = '';
  showPassword = false;

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }

  login(): void {
    if (!this.email || !this.password) {
      this.errorMessage = 'Enter your email and password to sign in.';
      return;
    }

    this.errorMessage = '';
    this.isLoading = true;

    this.auth.login({
      email: this.email,
      password: this.password
    }).subscribe({
      next: () => {
        // Auth.login already stores the tokens.
        this.isLoading = false;
        this.router.navigate(['/home']);
      },

      error: (error) => {
        this.isLoading = false;

        this.errorMessage = error.status === 0
          ? 'Cannot reach the Propyka server. Check that the API is running.'
          : 'That email and password do not match. Check both and try again.';
      }
    });
  }
}