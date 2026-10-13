import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

import { authInterceptor } from './auth-interceptor';
import { environment } from '../../../environments/environment';

describe('authInterceptor', () => {
  const api = environment.apiUrl;
  const unauthorized = { status: 401, statusText: 'Unauthorized' };

  let http: HttpClient;
  let mock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([{ path: 'login', children: [] }])
      ]
    });

    http = TestBed.inject(HttpClient);
    mock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    mock.verify();
    localStorage.clear();
  });

  it('attaches the stored access token as a bearer header', () => {
    localStorage.setItem('accessToken', 'access-1');
    localStorage.setItem('refreshToken', 'refresh-1');

    http.get(`${api}/api/account/me`).subscribe();

    const req = mock.expectOne(`${api}/api/account/me`);
    expect(req.request.headers.get('Authorization')).toBe('Bearer access-1');
    req.flush({});
  });

  it('refreshes once on an expired token, then replays the request', () => {
    localStorage.setItem('accessToken', 'access-old');
    localStorage.setItem('refreshToken', 'refresh-1');

    let result: unknown;
    http.get(`${api}/api/account/me`).subscribe((value) => (result = value));

    mock.expectOne(`${api}/api/account/me`).flush(null, unauthorized);

    const refresh = mock.expectOne(`${api}/refresh`);
    expect(refresh.request.body).toEqual({ refreshToken: 'refresh-1' });
    refresh.flush({
      tokenType: 'Bearer',
      accessToken: 'access-new',
      expiresIn: 3600,
      refreshToken: 'refresh-2'
    });

    const replay = mock.expectOne(`${api}/api/account/me`);
    expect(replay.request.headers.get('Authorization')).toBe('Bearer access-new');
    replay.flush({ firstName: 'Asha' });

    expect(result).toEqual({ firstName: 'Asha' });
    expect(localStorage.getItem('accessToken')).toBe('access-new');
    expect(localStorage.getItem('refreshToken')).toBe('refresh-2');
  });

  it('shares a single refresh between parallel 401s', () => {
    localStorage.setItem('accessToken', 'access-old');
    localStorage.setItem('refreshToken', 'refresh-1');

    http.get(`${api}/api/a`).subscribe();
    http.get(`${api}/api/b`).subscribe();

    mock.expectOne(`${api}/api/a`).flush(null, unauthorized);
    mock.expectOne(`${api}/api/b`).flush(null, unauthorized);

    const refreshes = mock.match(`${api}/refresh`);
    expect(refreshes.length).toBe(1);

    refreshes[0].flush({
      tokenType: 'Bearer',
      accessToken: 'access-new',
      expiresIn: 3600,
      refreshToken: 'refresh-2'
    });

    expect(mock.expectOne(`${api}/api/a`).request.headers.get('Authorization'))
      .toBe('Bearer access-new');
    expect(mock.expectOne(`${api}/api/b`).request.headers.get('Authorization'))
      .toBe('Bearer access-new');
  });

  it('does not attempt a refresh when /login rejects the credentials', () => {
    let failed = false;

    http.post(`${api}/login`, { email: 'a@b.c', password: 'wrong' })
      .subscribe({ error: () => (failed = true) });

    mock.expectOne(`${api}/login`).flush(null, unauthorized);

    expect(failed).toBe(true);
    mock.expectNone(`${api}/refresh`);
  });

  it('signs the user out when the refresh itself is rejected', () => {
    localStorage.setItem('accessToken', 'access-old');
    localStorage.setItem('refreshToken', 'refresh-expired');

    let failed = false;
    http.get(`${api}/api/x`).subscribe({ error: () => (failed = true) });

    mock.expectOne(`${api}/api/x`).flush(null, unauthorized);
    mock.expectOne(`${api}/refresh`).flush(null, unauthorized);

    expect(failed).toBe(true);
    expect(localStorage.getItem('accessToken')).toBeNull();
    expect(localStorage.getItem('refreshToken')).toBeNull();
  });

  it('passes non-401 errors straight through without refreshing', () => {
    localStorage.setItem('accessToken', 'access-1');
    localStorage.setItem('refreshToken', 'refresh-1');

    let status: number | undefined;
    http.get(`${api}/api/x`).subscribe({
      error: (err: { status: number }) => (status = err.status)
    });

    mock.expectOne(`${api}/api/x`).flush(null, { status: 500, statusText: 'Server Error' });

    expect(status).toBe(500);
    mock.expectNone(`${api}/refresh`);
    expect(localStorage.getItem('accessToken')).toBe('access-1');
  });
});
