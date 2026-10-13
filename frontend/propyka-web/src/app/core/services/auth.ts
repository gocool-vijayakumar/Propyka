import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, finalize, shareReplay, tap, throwError } from 'rxjs';
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

/** Same shape from /login and /refresh. */
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

  /** Set while a refresh is running, so parallel callers share one request. */
  private refreshInFlight: Observable<LoginResponse> | null = null;

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

  /** Pro members and admins: no advertising and the higher limits. */
  isPro = computed(() => {
    const roles = this.profile()?.roles ?? [];
    return roles.includes('Pro') || roles.includes('Admin');
  });

  /** Guests and free members see ads. Pro members do not. */
  adsEnabled = computed(() => !this.isPro());

  displayName = computed(() => {
    const profile = this.profile();
    return profile ? `${profile.firstName} ${profile.lastName}`.trim() : '';
  });

  /** The access token currently stored, or null when signed out. */
  get accessToken(): string | null {
    return localStorage.getItem('accessToken');
  }

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
      .pipe(tap((response) => this.storeTokens(response)));
  }

  /**
   * Exchanges the stored refresh token for a new access token. Errors when no
   * refresh token is stored or the server rejects it. Callers that arrive while
   * a refresh is running get that same request, so a burst of 401s causes one
   * refresh rather than one per request.
   */
  refreshSession(): Observable<LoginResponse> {
    if (this.refreshInFlight) {
      return this.refreshInFlight;
    }

    const refreshToken = localStorage.getItem('refreshToken');

    if (!refreshToken) {
      return throwError(() => new Error('No refresh token is stored.'));
    }

    this.refreshInFlight = this.http
      .post<LoginResponse>(`${this.apiUrl}/refresh`, { refreshToken })
      .pipe(
        tap((response) => this.storeTokens(response)),
        finalize(() => (this.refreshInFlight = null)),
        shareReplay({ bufferSize: 1, refCount: false })
      );

    return this.refreshInFlight;
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

  /** MapIdentityApi's /manage/info. The server checks the current password. */
  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/manage/info`, {
      oldPassword: currentPassword,
      newPassword
    });
  }

  logout(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');

    this.refreshInFlight = null;
    this.profile.set(null);
    this.isAuthenticated.set(false);
  }

  private storeTokens(response: LoginResponse): void {
    localStorage.setItem('accessToken', response.accessToken);
    localStorage.setItem('refreshToken', response.refreshToken);

    this.isAuthenticated.set(true);
  }
}
