import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { Auth, UserProfile } from '../../core/services/auth';
import { AdSlot } from './ad-slot';

const baseProfile: Omit<UserProfile, 'roles'> = {
  id: 'u-1',
  email: 'asha@example.com',
  firstName: 'Asha',
  lastName: 'Rao',
  createdAt: '2026-01-01T00:00:00Z'
};

describe('AdSlot', () => {
  let fixture: ComponentFixture<AdSlot>;
  let auth: Auth;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdSlot],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    }).compileComponents();

    auth = TestBed.inject(Auth);
    fixture = TestBed.createComponent(AdSlot);
  });

  it('shows the placeholder ad to guests', () => {
    fixture.detectChanges();

    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('.ad')).not.toBeNull();
    expect(element.textContent).toContain('Advertisement');
  });

  it('shows the placeholder ad to free members', () => {
    auth.isAuthenticated.set(true);
    auth.profile.set({ ...baseProfile, roles: ['Member'] });
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('.ad')).not.toBeNull();
  });

  it('renders nothing for Pro members', () => {
    auth.isAuthenticated.set(true);
    auth.profile.set({ ...baseProfile, roles: ['Member', 'Pro'] });
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('.ad')).toBeNull();
  });

  it('renders nothing for admins', () => {
    auth.isAuthenticated.set(true);
    auth.profile.set({ ...baseProfile, roles: ['Admin'] });
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('.ad')).toBeNull();
  });
});
