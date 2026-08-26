import { Routes } from '@angular/router';
import { Home } from './features/home/home';
import { authGuard } from './core/guards/auth-guard';
import { guestGuard } from './core/guards/guest-guard';
import { adminGuard } from './core/guards/admin-guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'home',
    pathMatch: 'full'
  },

  // ── Auth ────────────────────────────────────────────────────────────────────
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/login/login').then(m => m.Login)
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/register/register').then(m => m.Register)
  },

  // ── Home ────────────────────────────────────────────────────────────────────
  {
    path: 'home',
    component: Home,
    canActivate: [authGuard]
  },

  // ── Listings (public browse + authenticated management) ─────────────────────
  {
    path: 'listings',
    loadComponent: () =>
      import('./features/listings/search/listings-search').then(m => m.ListingsSearch)
  },
  {
    // Must come before :slug to avoid matching "new"
    path: 'listings/new',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/listings/create/listing-form').then(m => m.ListingForm)
  },
  {
    path: 'listings/mine',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/listings/my-listings/my-listings').then(m => m.MyListings)
  },
  {
    // Edit an existing listing by its DB id
    path: 'listings/:id/edit',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/listings/create/listing-form').then(m => m.ListingForm)
  },
  {
    // Image management for a listing
    path: 'listings/:id/images',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/listings/images/listing-images').then(m => m.ListingImages)
  },
  {
    // Public detail page — slug-based, must come AFTER literal segments above
    path: 'listings/:slug',
    loadComponent: () =>
      import('./features/listings/detail/listing-detail').then(m => m.ListingDetail)
  },

  // ── Enquiries ───────────────────────────────────────────────────────────────
  {
    path: 'enquiries',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/enquiries/enquiries').then(m => m.Enquiries)
  },

  // ── Favourites ──────────────────────────────────────────────────────────────
  {
    path: 'favourites',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/favourites/favourites').then(m => m.Favourites)
  },

  // ── Account ─────────────────────────────────────────────────────────────────
  {
    path: 'account',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/account/profile/profile').then(m => m.Profile)
  },

  // ── Admin ───────────────────────────────────────────────────────────────────
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./features/admin/shell/admin-shell').then(m => m.AdminShell),
    children: [
      {
        path: '',
        redirectTo: 'users',
        pathMatch: 'full'
      },
      {
        path: 'users',
        loadComponent: () =>
          import('./features/admin/users/users').then(m => m.AdminUsers)
      }
    ]
  },

  {
    path: '**',
    redirectTo: 'listings'
  }
];
