# Propyka — Project State

**Updated:** 8 October 2026
**Repo:** github.com/gocool-vijayakumar/Propyka (public)
**Local root:** `D:\Projects\Dev\Propyka`

This file is the single source of truth for where the project stands. Keep it in
the repo root next to `README.md` and update it at the end of each session.
`README.md` is how to *run* and *deploy* the project; this file is what is *built*
and what is *next*.

---

## 1. Current milestone

- **Milestone 1: Foundation and authentication.** Complete.
- **Milestone 2: Admin and account management.** Complete.
- **Milestone 3: Listings.** Built end to end: API, photos, favourites, enquiries,
  search and the owner workspace. Not yet hardened, and not yet deployed.
- **Milestone 4: MVP deployment.** Code is ready (see section 3, "Before real users").
  Waiting on a hosting decision, a verified backend build and a clean-up of the test account.

---

## 2. What is DONE

### Infrastructure

- .NET 8 Web API, PostgreSQL, EF Core 8, migrations applied on startup
- Angular 22 front end: standalone components, signals, lazy-loaded routes
- Swagger in Development, with bearer auth
- CORS origins read from configuration (localhost by default)
- Health check at `/health`
- Connection string and admin email kept in user secrets
- Forwarded-header support for platform proxies (`Proxy:TrustForwardedHeaders`)
- Migrations run on startup (`Database:ApplyMigrationsOnStartup`)
- Production build uses `environment.production.ts` (placeholder API URL, set before deploy)
- GitHub Actions workflow: API build, web production build, web unit tests
- `build.cmd` / `build.ps1` at the repo root: builds the API into `dist\api` and the web app into `dist\web`, with optional test and clean steps

### Authentication

- ASP.NET Identity with `IdentityDbContext<ApplicationUser>`; tables renamed
  (`Users`, `Roles`, `UserRoles`, and so on)
- `ApplicationUser` has `FirstName`, `LastName`, `CreatedAt`, `IsDeleted`,
  `DeletedAt`, `DeletedByUserId`
- `MapIdentityApi<ApplicationUser>()` provides `/login`, `/refresh`, `/manage/*`
- Password policy: 8+ characters, upper, lower, digit and symbol. Lockout after
  5 failed attempts for 15 minutes.
- **Silent session refresh (new):** the interceptor attaches the access token. On a
  401 for an expired token it refreshes once, shares that refresh across parallel
  requests, and replays the original request. Only a failed refresh signs the user
  out. `/login` and `/refresh` never trigger a refresh.
- Angular: `Auth` service, `authInterceptor`, `authGuard`, `guestGuard`, `adminGuard`
- Tokens in `localStorage` as `accessToken` / `refreshToken`

### Roles

- Three roles: `Admin`, `Moderator`, `Member`
- `IdentitySeeder` creates roles on startup and promotes the email in
  `Propyka:AdminEmail` to Admin. Idempotent.
- New registrations get `Member`

### Account

- `PUT /api/account/me` updates names. Profile page in the app.
- **Change password (new):** `POST /manage/info` on the profile page. On success the
  user is signed out and asked to sign in with the new password.
- `DELETE /api/account/me` soft-deletes after a password check. Profile page has a
  confirmation step.
- Soft delete is blocked in two places: `PropykaSignInManager.CanSignInAsync` and the
  `ActiveUser` default authorization policy, so tokens issued before deletion stop
  working immediately.
- No EF global query filter on users, deliberately (see Decisions).

### Plans and ads (new)

- Two plans, defined once in `Common/Plans/PlanCatalog.cs` and served by `GET /api/plans`.
  - **Free:** 3 active listings, 6 photos per listing, 20 saved, ads on, free.
  - **Pro:** 50 active listings, 20 photos per listing, 500 saved, ads off, INR 499 a month.
- The plan comes from roles (`Pro` or `Admin`), so there is no schema change. `PlanService` reads roles from the database, not the token, so a change applies at once for limit checks.
- Limits enforced: listing creation (`PropertyService`), photo uploads (`PropertyImageService`), favourites (`FavoriteService`). Over-limit requests return a readable 400.
- Profile returns `Plan` and `AdsEnabled`. The web `Auth` service exposes `isPro` and `adsEnabled`.
- Ads: an `<app-ad-slot>` placeholder. Shown after every sixth listing in search, in the detail sidebar and as a banner on home. Hidden for Pro and Admin. It is a house ad, not a network tag.
- Public browsing: search and listing detail are open to guests. Saving, enquiring and managing listings still require sign-in.
- Pricing page at `/pricing`. The upgrade button opens an email. Pro is granted by an admin with the roles endpoint.
- Topbar: Pricing link, a plan chip in the account menu, and a bottom tab bar for guests on phones.

### Admin console

- Separate shell at `/admin`. Users table with debounced search, Active / Deleted / All
  filter, server-side paging, role editor, delete and restore with confirmation.
- Server guards: cannot delete your own admin account, cannot remove your own admin
  role, cannot delete the last active admin.

### Listings

- Entities in `Modules/Listings/Domain`: `Property`, `PropertyAddress`,
  `PropertyImage`, `Amenity`, `PropertyAmenity`, `Favorite`, `Enquiry`, plus enums.
- Public search: published listings only. Filters: text, city, sale or rent, kind,
  price range, minimum bedrooms, amenities (all must match), sort.
- Public detail by slug, with view count incremented atomically (`ExecuteUpdate`).
- Owner workspace: `GET /api/properties/mine`, `GET /mine/{id}` (drafts included),
  create, update, publish (needs an image, an address and a price), archive.
- Photos: up to 20 per listing, 10 MB each, jpg / jpeg / png / webp. The first photo
  becomes the cover. Cover, sort order and captions can be changed. Deleting the last
  photo of a published listing is refused.
- Favourites: idempotent `PUT` and `DELETE`, `GET /ids` for heart icons.
- Enquiries: anonymous allowed, rate limited by IP. Owners see them under
  `/api/enquiries/received` and move them through New, Responded and Closed.

### Front end: design system

- `src/styles.css` holds ALL tokens: colour, type scale, spacing, radii, motion
- Full light and dark theming. Every colour is declared under `:root, [data-theme='light']`
  and `[data-theme='dark']`.
- `Theme` service: `light | dark | system`, saved to `localStorage` key `propyka-theme`
- Anti-flash script in `index.html` applies the theme before first paint
- Fonts: Bricolage Grotesque (display), Instrument Sans (UI), IBM Plex Mono
- Shared primitives: `.pk-brand` `.pk-mark` `.pk-label` `.pk-card` `.pk-ticks`
  `.pk-field` `.pk-input` `.pk-btn` `.pk-btn--primary` `.pk-btn--ghost`
  `.pk-icon-btn` `.pk-alert` `.pk-spinner` `.pk-grid-bg`
- Build new screens from these primitives, not new component CSS

### Front end: pages

- **Login, Register:** validation, Identity errors mapped to readable sentences
- **Home:** greeting, pillar tiles (marketplace and contractor tiles say "coming soon")
- **Listings search:** filters, sort, favourite hearts, pagination
- **Listing detail:** gallery, amenities, enquiry form
- **Listing form:** four steps (basics, details, location, review), create and edit
- **My listings, listing images, enquiries inbox, favourites**
- **Account:** profile, plan summary, change password, delete account
- **Pricing:** plan comparison, guest and member call-to-action
- **Listings search and detail:** public, with house ads for non-Pro visitors
- **Admin console:** section 2 above

### Tests

- Auth interceptor: 6 behaviour tests (token attached, single refresh, parallel 401s
  share one refresh, no refresh on `/login`, sign-out when refresh fails, other errors
  pass through)
- 13 spec files, 24 tests, all passing: `npx ng test --watch=false`. The generated
  stubs were failing before this session (missing HTTP and router providers, a wrong
  import path in `admin.spec.ts`, a stale title assertion in `app.spec.ts`, and no
  `matchMedia` in jsdom). They are fixed, not removed.
- Coverage is thin outside the auth interceptor. Most specs only check that a
  component or service can be created.
- No backend test project yet.

---

## 3. What is PENDING

### Before real users (blocks the MVP)

- [ ] **Remove the test account from the public README and any database it reached.**
      The previous README published a test login and password. The repo is public, so
      treat that password as compromised. Delete the account, or change its password, in
      every environment that used it.
- [ ] **Verify the backend builds.** This session's changes to `Program.cs`,
      `LocalFileStorage.cs`, `FavoriteService.cs`, `PropertyService.cs` and
      `appsettings.json` were checked with the Roslyn compiler against the ASP.NET Core
      reference pack. NuGet was blocked in that sandbox, so there is no full restore or
      run. Run `dotnet build` locally, then run the API and sign in once.
- [ ] **Choose a host** for the API (with PostgreSQL) and the web app. Set
      `environment.production.ts` and the API's `Cors__AllowedOrigins__0` to match.
- [ ] **Photo storage.** Local disk on a platform host is lost on redeploy. Attach a
      persistent volume at minimum, or implement `IFileStorage` for Azure Blob or S3.
      `StorageKey` is already a key, not a URL, so no data migration is needed.
- [ ] **Delete superseded front-end files:** `src/app/core/services/user.ts` and
      `user.spec.ts`. They call `/api/Test/private`, which no longer exists. Also decide
      about `frontend/propyka-web/src.zip`, which looks like a stray snapshot. Remove it
      from the repo unless it is intentional.
- [ ] **Watch the first CI run** on GitHub. Fix anything that differs from the local build.
- [ ] **Replace the placeholder addresses:** `billing@propyka.example` (pricing page), `advertise@propyka.example` (ad slot) and `apiUrl` in `environment.production.ts`.
- [ ] **Payments:** no payment provider is connected. Pro is granted by an admin after a manual payment. Choose a provider before charging anyone.
- [ ] **Ad network:** the ad slot is a house placeholder. Replace its body with the network's tag once one is chosen. Check the network's policy on placement near listings.
- [ ] **Run `build.cmd` on the Windows machine.** The PowerShell script was written in this session and has not been run on Windows. Also run `dotnet build` once, as above.

### Next (product quality)

- [ ] **User menu** in the top bar instead of a bare Log out button (last item of P2)
- [ ] **Expose less owner data.** `PropertyDetailResponse.Owner` returns the owner's
      Identity user ID. Return only the display name, unless the front end needs the ID.
- [ ] **Favourites list:** hide listings that were archived after being saved. Right now
      `GET /api/favorites` shows them with their status.
- [ ] **Photo uploads:** check file content (magic bytes), not only the extension.
      Count-limit checks race under parallel uploads. A failed database save leaves an
      orphan file.
- [ ] **Search input:** escape `%` and `_` before building `ILIKE` patterns.
- [ ] **Role changes:** `SetRoles` does not update the security stamp, so a demoted admin
      keeps admin rights until their token expires. Update the stamp on role changes.
- [x] **Plans, ads and public browsing** (see section 2).
- [x] **Build script** `build.cmd` / `build.ps1`.
- [x] **Global error and toast notifications.** `Toasts` service and `<app-toaster>`, used for save, update and failure messages.
- [ ] **Listings section** of the admin console. The sidebar shows a "Soon" placeholder.
- [ ] **Tests:** backend integration tests (WebApplicationFactory with a test database),
      and component tests for the listing form and search.

### Cleanup (low risk)

- [ ] Rename namespace `Propyka.Api.Modules.Identity.Autorization` to `...Authorization`
      in `ActiveUserPolicy.cs` and its `using` in `IdentityModule.cs`. The folder is already
      spelled correctly. This is cosmetic.
- [ ] `PropertiesController` comment mentions a "drafts" route that does not exist.
- [ ] `ApplicationUser` and `PropertyDetailResponse` mapping: keep the document in sync
      with the Angular models when either changes.

### Deliberately out of scope for now

- Marketplace and trades pillars. Different enough (carts, orders, quotes, scheduling)
  that guessing at the schema now would produce throwaway tables.
- Agent or agency accounts. Any registered user can list a property.
- Full-text search. Postgres `tsvector` plus a GIN index is the right answer, but wait
  until there are real listings to search.
- Price history, saved searches, viewing appointments, messaging threads.
- Email confirmation and password reset (forgot-password) flows.

---

## 4. Decisions made, and why

Worth being able to defend these in an interview. They are the interesting part.

**One DbContext, not two.** `Property.OwnerId` is a foreign key to `Users`. Splitting
across contexts loses the FK, loses transactional consistency, and gains two migration
histories over one database. The class is still named `PropykaIdentityDbContext`;
renaming would mean editing the `[DbContext]` attribute on existing migrations for no
functional gain.

**`Guid` keys on listings, `string` on users.** Identity fixes user IDs as `string`.
Sequential int IDs in URLs let anyone walk `/properties/1`, `/properties/2` and count
your inventory. `Slug` is what appears in URLs.

**Enums stored as text** via `HasConversion<string>()`. The database reads `'Published'`
not `2`, and reordering the C# enum can never silently remap rows.

**No global query filter on users.** Tempting for soft delete, but `FindByEmailAsync`
would then return null for a deleted account and someone could re-register the same
email. Identity's unique index is on `NormalizedUserName`, not email. So deleted users
stay findable and are blocked at sign-in and per request instead.

**Soft delete needs both blocks.** `CanSignInAsync` stops new logins, but a token issued
before deletion stays valid for its remaining hour. The `ActiveUser` default policy closes
that window.

**Refresh is single-flight, and only expired tokens trigger it.** Five parallel requests
that all get a 401 cause one `/refresh` call. A request that was sent with an older token
retries with the newer one without refreshing again. `/login` and `/refresh` are excluded,
because a wrong password also returns 401 and refreshing on `/refresh` would loop.

**Forwarded headers are on by default, and that is a trade-off.** Behind a platform proxy
the app otherwise sees plain HTTP from the proxy's address. The HTTPS redirect then loops,
and every visitor shares one rate-limit bucket. Trusting `X-Forwarded-For` is only safe
when nothing reaches the app except through the proxy. Set `Proxy:TrustForwardedHeaders`
to `false` otherwise.

**Migrations run on startup.** This keeps MVP deployment to one step. It is the wrong
default once there are several instances racing to migrate. Turn it off then, and run
`dotnet ef database update` as a separate deploy step.

**Deleting a property cascades** to address, images, amenity links, favourites and enquiries.
Deleting a *user* is `Restrict`, because orphaned listings are worse than a failed delete.
Enquiries use `SetNull` on sender, so an enquiry survives when the sender closes their account.

**Separate admin shell, not pages in the main app.** The visual mode switch is what makes
it read as a real admin console.

**Design tokens over per-component styling.** Two blocks in `styles.css` control every
colour in the app. This is also why the palette can be swapped in about a minute.

**Custom `AccountController` for registration.** `MapIdentityApi`'s built-in `/register`
accepts only email and password. The custom endpoint returns `ValidationProblem`, whose
`errors` dictionary is keyed by `IdentityError.Code`, which the Register component maps
to readable messages.

**Stored photo keys, not URLs.** `StorageKey` is a key, so local disk now and blob storage
later needs no migration. URLs are built at read time from the storage backend.

---

## 5. Gotchas already hit — do not rediscover these

**Swagger 500 on `/swagger/v1/swagger.json`.** Two types sharing a short name.
`MapIdentityApi` publishes `RegisterRequest`, `LoginRequest`, `RefreshRequest`,
`InfoRequest`. Never name your own DTO any of those. Mitigated with
`options.CustomSchemaIds(type => type.FullName?.Replace("+", "."))`. The real error is
always in the terminal, never the browser.

**The bearer token is NOT a JWT.** `MapIdentityApi` issues an encrypted `ClaimsPrincipal`
ticket. It will not decode at jwt.io. Do not claim "JWT authentication" on a CV or README
for this project. An interviewer will poke at it.

**Role claims are baked into the token at login.** After granting a role, the user must
sign out and back in. Otherwise `/api/account/me` reports the new role (it reads the
database) while admin endpoints return 403 (they read the token). This looks like a bug and
is not one.

**Seeding runs only at startup.** Register the admin email first, restart the API, then
sign in again.

**`/login` returns 401 for a wrong password.** Any 401 handling must exclude `/login`, or a
mistyped password triggers a refresh.

**Npgsql timestamps.** `DateTimeOffset` maps to `timestamptz` cleanly. Switching to
`DateTime` throws unless `Kind` is `Utc`. Always store UTC.

**`decimal` needs `HasPrecision(18, 2)`** or EF warns and Postgres picks its own scale.

**Angular CLI needs Node 22.22.3+ or 24.15+.** Older 22.x patch releases fail with a
version error before any build starts.

**Production builds inline Google Fonts,** which needs internet access during `ng build`.
An offline or proxied build fails with a 403 on the fonts URL. Set `optimization.fonts` to
`false` in a local, uncommitted copy of `angular.json` to build offline.

**`dotnet dev-certs https --trust`** on any new machine, or every API call from Angular
fails in a way that looks like CORS.

**Angular CLI puts services in folders.** `ng g s core/services/thing` may create
`core/services/thing/thing.ts`, which breaks relative import depth. Use `--flat`.

**`--flat` on components too.** Without it, `ng g c features/admin/shell/admin-shell`
creates a nested `admin-shell` folder.

**`100dvh` not `100vh`** on mobile, and 16px minimum input font size or iOS Safari zooms
the page on focus.

---

## 6. Key file locations

```
backend/Propyka/Propyka.Api/
  Program.cs                                  pipeline, forwarded headers, migrations, seeding
  appsettings.json                            CORS, Database, Proxy (secrets are NOT here)
  Modules/Identity/IdentityModule.cs          Identity options, ActiveUser default policy
  Modules/Identity/Controllers/AccountController.cs   register, me, update, self-delete
  Modules/Identity/Controllers/AdminController.cs     user management
  Modules/Identity/IdentitySeeder.cs          roles and first admin
  Modules/Identity/Authorization/ActiveUserPolicy.cs  ActiveUser requirement (see cleanup)
  Modules/Identity/SignInManager.cs           blocks deleted accounts at sign-in
  Modules/Listings/Controllers/               properties, enquiries, favourites, amenities
  Modules/Listings/Services/                  property, image, enquiry, favourite services
  Modules/Listings/Domain/                    entities and enums
  Modules/Listings/Configurations/            EF mappings
  Common/Storage/LocalFileStorage.cs          photo storage (swap for blob storage here)
  Common/GlobalExceptionHandler.cs            maps DomainException and NotFoundException
  Persistence/PropykaDbContext.cs
  Persistence/Migrations/

frontend/propyka-web/
  angular.json                                build configs and environment replacements
  src/environments/                           environment.ts (dev), environment.development.ts,
                                              environment.production.ts (set apiUrl before deploy)
  src/styles.css                              ALL design tokens + primitives
  src/app/app.routes.ts
  src/app/core/guards/{auth,guest,admin}-guard.ts
  src/app/core/interceptors/auth-interceptor.ts       attach token, refresh on expiry
  src/app/core/services/{auth,admin,theme}.ts
  src/app/core/services/listings/listings.service.ts  all listing, enquiry and favourite calls
  src/app/features/                           auth, home, listings, favourites, enquiries, account, admin
  src/app/shared/theme-toggle/
  src/app/shared/topbar/                      shared header: nav, account menu, mobile tab bar
  src/app/shared/toaster/                     toast notifications
  src/app/core/services/toast.ts              toast queue (max four, auto-dismiss)

.github/workflows/ci.yml                      API build, web build, web tests
```

Local secrets (not in repo, per machine): `ConnectionStrings:PropykaDatabase`,
`Propyka:AdminEmail`.

---

## 7. Briefing a fresh assistant

Paste this file, then say what you want to work on. If it is front-end work, also paste
`styles.css` so the tokens and primitives are known. Otherwise you will get hardcoded hex
values that break dark mode. For back-end work, paste `Program.cs` and the relevant
controller and service.

Useful sentence to include: *"Do not re-do anything in section 2. Start from section 3."*
