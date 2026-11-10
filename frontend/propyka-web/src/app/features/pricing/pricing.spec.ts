import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { Auth, UserProfile } from '../../core/services/auth';
import { PlanInfo, Plans } from '../../core/services/plans';
import { Pricing } from './pricing';

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

const catalogue: PlanInfo[] = [
  {
    name: 'Free',
    tagline: 'Start out',
    monthlyPriceInr: 0,
    adsEnabled: true,
    maxActiveListings: 3,
    maxPhotosPerListing: 6,
    maxSavedListings: 20,
    features: ['3 active listings']
  },
  {
    name: 'Pro',
    tagline: 'Go ad-free',
    monthlyPriceInr: 499,
    adsEnabled: false,
    maxActiveListings: 50,
    maxPhotosPerListing: 20,
    maxSavedListings: 500,
    features: ['50 active listings']
  }
];

const member: UserProfile = {
  id: 'u-1',
  email: 'asha@example.com',
  firstName: 'Asha',
  lastName: 'Rao',
  createdAt: '2026-01-01T00:00:00Z',
  roles: ['Member']
};

describe('Pricing', () => {
  let fixture: ComponentFixture<Pricing>;
  let auth: Auth;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Pricing],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: Plans, useValue: { list: () => of(catalogue) } }
      ]
    }).compileComponents();

    auth = TestBed.inject(Auth);
    fixture = TestBed.createComponent(Pricing);
    fixture.detectChanges();
  });

  it('shows both plans with sign-up calls to action for guests', () => {
    const element: HTMLElement = fixture.nativeElement;

    expect(element.querySelectorAll('.plan').length).toBe(2);
    expect(element.textContent).toContain('Create account to upgrade');
    expect(element.textContent).toContain('Create free account');
  });

  it('marks the Free plan as current for a free member and offers a Pro request', () => {
    auth.isAuthenticated.set(true);
    auth.profile.set(member);
    fixture.detectChanges();

    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('.plan__current')?.textContent).toContain('Your current plan');
    expect(element.textContent).toContain('Request Pro');
  });

  it('marks Pro as the current plan for a Pro member', () => {
    auth.isAuthenticated.set(true);
    auth.profile.set({ ...member, roles: ['Member', 'Pro'] });
    fixture.detectChanges();

    const element: HTMLElement = fixture.nativeElement;
    expect(element.textContent).toContain('Your current plan');
    expect(element.textContent).not.toContain('Request Pro');
  });
});
