# Propyka

Propyka is a property marketplace for buying and renting homes. Visitors can browse published listings, save favourites and send enquiries. Any signed-in member can list a property, and admins manage accounts from a separate console.

The construction-materials marketplace and the contractor platform from the original plan are not built yet. `Propyka-ProjectStatus.md` tracks what is built, what is pending and what comes next.

## What works today

- **Accounts:** register, sign in, silent session refresh, edit your name, change your password, delete your account (soft delete; an admin can restore it).
- **Listings:** create, edit, publish and archive. Upload up to 20 photos, choose the cover and reorder them.
- **Browsing:** search, filter by city, sale or rent, type, price, bedrooms and amenities, sort, and view listing detail pages with view counts.
- **Favourites** and **enquiries** from listing pages, with an inbox for listing owners.
- **Admin console:** search users, change roles, deactivate and restore accounts.
- Light and dark themes.

## Stack

- **API:** ASP.NET Core 8 Web API, ASP.NET Core Identity, EF Core 8 with Npgsql, PostgreSQL, Swagger.
- **Web app:** Angular 22 with standalone components and signals, TypeScript, Vitest for unit tests.
- **Auth:** bearer tokens issued by Identity's `MapIdentityApi`. They are encrypted tickets, not JWTs.

## Repository layout

```
backend/Propyka/
├── Propyka.sln
└── Propyka.Api/
    ├── Modules/Identity/    accounts, roles, admin endpoints
    ├── Modules/Listings/    properties, images, favourites, enquiries
    ├── Common/              errors, rate limits, file storage, slugs
    ├── Persistence/         DbContext and migrations
    └── Program.cs           pipeline and module registration

frontend/propyka-web/src/app/
├── core/                    auth service, interceptor, guards, API services
├── features/                auth, home, listings, favourites, enquiries, account, admin
└── shared/                  shared components
```

## Run locally

**Prerequisites:** .NET 8 SDK, Node.js 22.22.3 or newer (or 24.15 or newer), PostgreSQL.

### 1. API

```bash
cd backend/Propyka/Propyka.Api
dotnet user-secrets set "ConnectionStrings:PropykaDatabase" "Host=localhost;Port=5432;Database=Propyka;Username=postgres;Password=YOUR_PASSWORD"
dotnet user-secrets set "Propyka:AdminEmail" "you@example.com"
dotnet dev-certs https --trust
dotnet run
```

The schema is created and updated on startup (`Database:ApplyMigrationsOnStartup`). Swagger is at https://localhost:7063/swagger.

**Making an admin:** register the account first, restart the API so the seeder promotes the email in `Propyka:AdminEmail`, then sign in again. Roles are baked into tokens at sign-in, so a role change only shows up in a new session.

### 2. Web app

```bash
cd frontend/propyka-web
npm ci
npm start
```

Open http://localhost:4200. Development builds call the API at https://localhost:7063. Run the unit tests with:

```bash
npx ng test --watch=false
```

## Deploying the MVP

### API

Configuration keys map to environment variables by replacing `:` with `__`.

| Variable | Value |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` (turns Swagger off) |
| `ConnectionStrings__PropykaDatabase` | PostgreSQL connection string |
| `Propyka__AdminEmail` | Email of the first admin. Register that account before the first start. |
| `Cors__AllowedOrigins__0` | Public origin of the web app, for example `https://app.example.com`. Replaces the localhost entry. Use `__1`, `__2` for more origins. |
| `Proxy__TrustForwardedHeaders` | `true` when a hosting proxy sits in front of the API. This is the default in `appsettings.json`. Set `false` only if clients can reach the API directly. |
| `Database__ApplyMigrationsOnStartup` | `true` for a single instance. Set `false` if you run migrations as a separate step. |

The health check is at `/health`.

**Uploaded photos are written to `wwwroot/uploads` on the instance's disk.** Most hosts discard that disk on redeploy. Attach a persistent volume, or move to blob storage, before real users upload photos.

### Web app

1. Set `apiUrl` in `frontend/propyka-web/src/environments/environment.production.ts` to the API's public HTTPS address, with no trailing slash.
2. Run `npm run build`. The output is in `dist/propyka-web/browser`.
3. Host that folder on any static host. Configure a single-page-app fallback so that direct links such as `/listings/some-slug` serve `index.html`.
4. Add the web app's origin to `Cors__AllowedOrigins__0` on the API.

## Security notes

- Keep secrets out of the repository. Connection strings and the admin email belong in user secrets or environment variables.
- Do not add test accounts or passwords to this file. Create local test users through the register screen.
- Access tokens last about an hour and the web app refreshes them silently. Tokens are kept in `localStorage`. That is simple, but any script running on the page can read them. Move to httpOnly cookies if that risk matters for your deployment.

## API overview

```
Accounts
  POST   /api/account/register          create an account (rate limited)
  GET    /api/account/me                current profile and roles
  PUT    /api/account/me                update name
  DELETE /api/account/me                soft delete (password required)
  POST   /login  /refresh               issue and refresh tokens (MapIdentityApi)
  POST   /manage/info                   change password (MapIdentityApi)

Listings
  GET    /api/properties                search (public, published only)
  GET    /api/properties/{slug}         detail (public, increments view count)
  GET    /api/properties/mine           your listings, drafts included
  GET    /api/properties/mine/{id}      one of your listings
  POST   /api/properties                create (draft)
  PUT    /api/properties/{id}           update (owner)
  POST   /api/properties/{id}/publish   draft to published
  POST   /api/properties/{id}/archive   hide from search
  POST   /api/properties/{id}/images    upload photo (owner, rate limited)
  PUT    /api/properties/{id}/images/{imageId}
  DELETE /api/properties/{id}/images/{imageId}
  POST   /api/properties/{id}/enquiries enquire (anonymous allowed, rate limited)
  GET    /api/amenities                 amenity list for forms and filters

Owner inbox, favourites
  GET    /api/enquiries/received        enquiries on your listings
  GET    /api/enquiries/sent            enquiries you sent
  PUT    /api/enquiries/{id}/status     New, Responded or Closed
  GET    /api/favorites                 your saved listings
  GET    /api/favorites/ids             saved ids, for heart icons
  PUT    /api/favorites/{propertyId}    save (idempotent)
  DELETE /api/favorites/{propertyId}    unsave

Admin (Admin and Moderator read; Admin writes)
  GET    /api/admin/users               search, status filter, paging
  POST   /api/admin/users/{id}/trash
  POST   /api/admin/users/{id}/restore
  PUT    /api/admin/users/{id}/roles
```

## Continuous integration

`.github/workflows/ci.yml` builds the API, then runs the production build and the unit tests for the web app. It runs on every push and pull request.
