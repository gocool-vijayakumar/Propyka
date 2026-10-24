import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { Auth, UserProfile } from '../../core/services/auth';
import { Topbar } from './topbar';

// jsdom does not implement matchMedia, which the theme service reads.
beforeAll(() => {
  window.matchMedia ??= (query: string) =>
    ({
      matches: false,
      media: query,
      onchange: null,
      addListener: () => {},
      removeListener: () => {},
      addEventListener: () => {},
      removeEventListener: () => {},
      dispatchEvent: () => false
    }) as unknown as MediaQueryList;
});

const member: UserProfile = {
  id: 'u-1',
  email: 'asha@example.com',
  firstName: 'Asha',
  lastName: 'Rao',
  createdAt: '2026-01-01T00:00:00Z',
  roles: ['Member']
};

describe('Topbar', () => {
  let fixture: ComponentFixture<Topbar>;
  let auth: Auth;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Topbar],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    }).compileComponents();

    auth = TestBed.inject(Auth);
    fixture = TestBed.createComponent(Topbar);
    fixture.detectChanges();
  });

  it('offers sign in and no account menu to guests', () => {
    const element: HTMLElement = fixture.nativeElement;

    expect(element.textContent).toContain('Sign in');
    expect(element.querySelector('.avatar-btn')).toBeNull();
    expect(element.querySelector('.tabbar')).toBeNull();
  });

  it('opens the account menu for a signed-in member', () => {
    auth.isAuthenticated.set(true);
    auth.profile.set(member);
    fixture.detectChanges();

    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('.menu__panel')).toBeNull();

    (element.querySelector('.avatar-btn') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(element.querySelector('.menu__panel')).not.toBeNull();
    expect(element.querySelector('.menu__email')?.textContent).toContain('asha@example.com');
    expect(element.querySelector('.tabbar')).not.toBeNull();
  });

  it('hides the admin link from members', () => {
    auth.isAuthenticated.set(true);
    auth.profile.set(member);
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.avatar-btn') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('Admin console');
  });

  it('signs out and returns to the login page', () => {
    auth.isAuthenticated.set(true);
    auth.profile.set(member);
    fixture.detectChanges();

    const logout = vi.spyOn(auth, 'logout');
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    (fixture.nativeElement.querySelector('.avatar-btn') as HTMLButtonElement).click();
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('.menu__item--danger') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(logout).toHaveBeenCalled();
    expect(navigate).toHaveBeenCalledWith(['/login']);
    expect(fixture.nativeElement.querySelector('.menu__panel')).toBeNull();
  });
});
