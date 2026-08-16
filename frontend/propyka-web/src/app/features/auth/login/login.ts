import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Auth } from '../../../core/services/auth';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login {
  private auth = inject(Auth);

  email = '';
  password = '';

  isLoading = false;
  errorMessage = '';

  login(): void {
    this.errorMessage = '';
    this.isLoading = true;

    this.auth.login({
      email: this.email,
      password: this.password
    }).subscribe({
      next: (response) => {
  console.log('Login successful:', response);

  localStorage.setItem(
    'accessToken',
    response.accessToken
  );

  localStorage.setItem(
    'refreshToken',
    response.refreshToken
  );

  this.isLoading = false;
},


      error: (error) => {
        console.error('Login failed:', error);
        this.errorMessage = 'Invalid email or password.';
        this.isLoading = false;
      }
    });
  }
}
