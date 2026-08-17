import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  tokenType: string;
  accessToken: string;
  expiresIn: number;
  refreshToken: string;
}

export interface UserProfile {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  createdAt: string;
  roles: string[];
}

@Injectable({
  providedIn: 'root'
})
export class Auth {

  private http = inject(HttpClient);

  private readonly apiUrl = environment.apiUrl;

  isAuthenticated = signal(
    !!localStorage.getItem('accessToken')
  );

  /** Populated by loadProfile(); null until then. */
  profile = signal<UserProfile | null>(null);

  isAdmin = computed(() =>
    this.profile()?.roles.includes('Admin') ?? false
  );

  isModerator = computed(() =>
    this.profile()?.roles.includes('Moderator') ?? false
  );

  displayName = computed(() => {
    const profile = this.profile();
    return profile ? `${profile.firstName} ${profile.lastName}`.trim() : '';
  });

  /**
   * Uses the custom AccountController, not MapIdentityApi's /register —
   * the built-in endpoint only accepts email and password, so it cannot
   * capture FirstName / LastName on ApplicationUser.
   */
  register(request: RegisterRequest): Observable<void> {
    return this.http.post<void>(
      `${this.apiUrl}/api/account/register`,
      request
    );
  }

  /** MapIdentityApi's /login, which issues the tokens. */
  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${this.apiUrl}/login`, request)
      .pipe(
        tap((response) => {
          localStorage.setItem('accessToken', response.accessToken);
          localStorage.setItem('refreshToken', response.refreshToken);

          this.isAuthenticated.set(true);
        })
      );
  }

  loadProfile(): Observable<UserProfile> {
    return this.http
      .get<UserProfile>(`${this.apiUrl}/api/account/me`)
      .pipe(tap((profile) => this.profile.set(profile)));
  }

  deleteOwnAccount(password: string): Observable<void> {
    return this.http.request<void>(
      'delete',
      `${this.apiUrl}/api/account/me`,
      { body: { password } }
    );
  }

  logout(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');

    this.profile.set(null);
    this.isAuthenticated.set(false);
  }
}