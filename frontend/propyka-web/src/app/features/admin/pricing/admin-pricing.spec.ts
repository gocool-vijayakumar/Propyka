import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { Admin, AdminPlanPrice } from '../../../core/services/admin/admin';
import { Auth, UserProfile } from '../../../core/services/auth';
import { AdminPricing } from './admin-pricing';

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

const prices: AdminPlanPrice[] = [
  { planName: 'Free', monthlyPriceInr: 0, yearlyPriceInr: 0, offerLabel: null, isEditable: false,
    updatedAt: '2026-11-13T00:00:00Z', updatedByName: null },
  { planName: 'Pro', monthlyPriceInr: 49, yearlyPriceInr: 499, offerLabel: null, isEditable: true,
    updatedAt: '2026-11-13T00:00:00Z', updatedByName: null }
];

const adminUser: UserProfile = {
  id: 'a-1',
  email: 'admin@example.com',
  firstName: 'Ada',
  lastName: 'Min',
  createdAt: '2026-01-01T00:00:00Z',
  roles: ['Admin']
};

describe('AdminPricing', () => {
  let fixture: ComponentFixture<AdminPricing>;
  let component: AdminPricing;
  let updatePlanPrice: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    updatePlanPrice = vi.fn((planName: string, body: Omit<AdminPlanPrice, 'planName'>) =>
      of({ ...prices[1], ...body, planName, updatedByName: 'Ada Min' }));

    await TestBed.configureTestingModule({
      imports: [AdminPricing],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: Admin, useValue: { getPlanPrices: () => of(prices), updatePlanPrice } }
      ]
    }).compileComponents();

    const auth = TestBed.inject(Auth);
    auth.isAuthenticated.set(true);
    auth.profile.set(adminUser);

    fixture = TestBed.createComponent(AdminPricing);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('locks the Free plan and lets an admin edit Pro', () => {
    const element: HTMLElement = fixture.nativeElement;

    expect(element.textContent).toContain('Freemium');
    expect(element.querySelectorAll('form').length).toBe(1);
  });

  it('refuses a zero price before calling the server', () => {
    component.update('Pro', 'monthlyPriceInr', 0);

    expect(component.validationError('Pro')).toContain('above zero');
    component.save(prices[1]);
    expect(updatePlanPrice).not.toHaveBeenCalled();
  });

  it('warns when yearly is no cheaper than twelve monthly payments', () => {
    component.update('Pro', 'monthlyPriceInr', 9);

    expect(component.yearlyHint('Pro')).toContain('Members will pick monthly');
  });

  it('saves a seasonal price and offer label', () => {
    component.update('Pro', 'monthlyPriceInr', 29);
    component.update('Pro', 'offerLabel', '  Diwali offer  ');
    component.save(component.prices()[1]);

    expect(updatePlanPrice).toHaveBeenCalledWith('Pro', {
      monthlyPriceInr: 29,
      yearlyPriceInr: 499,
      offerLabel: 'Diwali offer'
    });
    expect(component.prices()[1].monthlyPriceInr).toBe(29);
    expect(component.isDirty(component.prices()[1])).toBe(false);
  });
});
