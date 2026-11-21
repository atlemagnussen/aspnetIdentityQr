# AGENTS.md

Guidance for coding agents (and humans) working in this repo.

## What this is

An OAuth2/OIDC identity server and its admin app, sharing one PostgreSQL database. **Branch `develop/new-id-server` is replacing Duende IdentityServer with our own protocol code** (authorization code + PKCE first). Tables stay exactly as Duende created them, so the new server is a drop-in replacement.

| Project | Role |
|---|---|
| `Id.Server/` | *(not built yet)* The new ID server: ASP.NET Core Identity UI (from `old/AspAuth.Local`) plus our own OIDC endpoints. Will deploy as `id.logout.work`. |
| `Id.Admin/` | Admin app (users, roles, OAuth clients, read-only resources). Signs in *via* the ID server over OIDC. Deployed as `user.logout.work` (image name still `useradmin`). |
| `Id.Lib/` | Shared: entities, `IdDbContext`, **all migrations**, Identity/Data Protection setup, OIDC constants, OTel, ops endpoints. No Duende reference. |
| `Id.Tests/` | xunit v3 + Testcontainers (real Postgres). |
| `old/` | The Duende-based `AspAuth.*` projects, kept for reference until `Id.Server` replaces them. Don't extend. |

Solution: `Id.slnx` (root). Stack: .NET 10, EF Core 10 + Npgsql, OpenTelemetry. Admin client: TypeScript + Lit + Vite, Web Awesome.

## Data model (one Postgres DB, one context)

`IdDbContext` (`Id.Lib/Data`) maps everything: `AspNet*` Identity tables (schema v3, passkeys), `UserProfile`, `SigningKeys` (JWK as `jsonb`), clients/scopes/resources/identity providers, `PersistedGrants` and the other operational tables, and `DataProtectionKeys`. Connection string `ConnectionStrings:AuthDb` (env `ConnectionStrings__AuthDb`).

Rules:
- **The schema is frozen to what Identity + Duende created.** Names, lengths, nullability and indexes in `IdDbContext` must match the live DB. Entity property names and CLR defaults follow Duende's EF entities; enum ints (`AccessTokenType`, `TokenUsage`, `TokenExpiration`) are Duende's values.
- Schema changes go through a migration in `Id.Lib/Migrations`. `Model_matches_migrations` (Id.Tests) goes red when a mapping changes without one.
- `Baseline` reproduces the old schema exactly (verified by comparing catalogs). An existing DB is adopted once with `Id.Lib/Migrations/adopt-existing-db.sql` instead of running `Baseline`.
- Nothing auto-migrates. Migrations are applied by hand.
- Identity's schema version comes from `IdentityOptions` in DI. Every app calls `DatabaseSetup.ConfigureIdentityOptions`; contexts created outside an app use `DatabaseSetup.StandaloneOptions`. `IdDbContext` throws if passkeys drop out of the model.
- Client secrets are stored as `Hashing.Sha256` (base64 SHA-256, Duende's format), so existing secrets keep validating.

## How auth ties the two apps together

- Id.Admin authenticates with cookie + OIDC (`Id.Admin/server/Auth/AuthenticationStartup.cs`). Authority comes from env `AUTH_SERVER` (default `https://id.logout.work`) and client from `AUTH_CLIENT` (default `web`). Scopes: `openid profile roles`. `MapInboundClaims = false`, so claim types are raw JWT names (`role`, `name`, `sub`).
- The ID server emits `role` claims (and `fullname`) from the user's ASP.NET Identity roles and `UserProfile` (old: `old/AspAuth.Lib/Services/ProfileService.cs`). `Id.Server` must emit the same claims.
- Roles are free-form (managed on the Roles page). The `UserRoles` enum (`Id.Lib/Entities/Users.cs`, currently only `Admin`) lists the *system* roles, which can't be renamed or deleted.
- Admin authorization: policy `Policies.RequiresAdmin` = authenticated + `role` claim `Admin`. **Every API controller and admin page must carry `[Authorize(Policies.RequiresAdmin)]`.** Logged-in non-admins land on `/AccessDenied`.
- Challenges: API calls (`/api/*`) get plain 401/403 from the cookie handler. Pages are challenged with OIDC (a redirect to the ID server). A policy scheme in `AuthenticationStartup` does the routing. Don't call `options.Events.RedirectToX` from inside `OnRedirectToX`; that recursion crashes the process.
- **Shared Data Protection key ring:** both apps call `AddIdDataProtection` (same table + app name `aspauth`). Password-reset tokens generated in Id.Admin therefore validate on the ID server's `/Identity/Account/ResetPassword` page. Because the key ring is shared, Id.Admin's auth cookie uses its own scheme name (`UserAdmin`, cookie `.UserAdmin.Auth`). The scheme name is part of the protector purpose, so an ID server cookie can never be accepted here. Keep it distinct.
- Self-protection, enforced server-side: an admin can't delete or lock themselves or remove their own Admin role. The client Id.Admin signs in with (`AUTH_CLIENT`) can't be disabled or deleted.
- The ID server reads clients from the DB on every request (no cache), so client/redirect/CORS changes made in Id.Admin apply immediately. Keep it that way.
- Both apps sit behind a reverse proxy: forwarded headers trust all proxies (`KnownProxies/KnownIPNetworks` cleared).

## Id.Admin layout

```
Id.Admin/
  server/                      ASP.NET Core (Razor Pages shell + API controllers)
    Program.cs
    Auth/                      OIDC + cookie + challenge policy scheme, policies, AdminOptions, CurrentUser (sub)
    Persistence/               IdDbContext, IdentityCore (+roles, token providers), shared Data Protection
    Api/
      ApiStartup.cs            controllers, JSON options, OpenAPI (3.1, served at /openapi/v1.json in Development)
      GlobalExceptionHandler.cs
      Users/                   UsersService (via UserManager): CRUD, roles, claims, lock/unlock, 2FA reset,
                               external logins, passkeys, sign-out-everywhere, set-password links
      Roles/                   RolesService: free-form roles, system roles protected
      Configuration/
        Clients/               ClientsService: full client CRUD (ClientDto <-> entity, collections synced),
                               secrets (generated, SHA-256 stored, shown once), ScopesController (picker lookup)
        ApiResources/ IdentityResources/ IdentityProviders/   read-only
    Pages/                     Index, Users (<users-list>), Roles (<roles-list>), Clients (<clients-list>),
                               AccessDenied, UserInfo, Logout
    wwwroot/                   BUILD OUTPUT of client (gitignored)
  client/                      Vite + TS + Lit
    src/main.ts                entry; imports views & components
    src/services/httpService.ts  fetch wrapper (cookies; ProblemDetails -> error message)
    src/components/            AdminElement (busy/error/notice + run()), appDialog (openDialog/confirmDialog/
                               promptDialog), listEditors (string-list-editor, key-value-editor), one-time-value,
                               sharedStyles (adminStyles for shadow DOM)
    src/views/users/           users-list, user-editor (tabs), user-create, usersService.ts
    src/views/roles/           roles-list, rolesService.ts
    src/views/configuration/   clients-list, client-editor (tabs, single save), client-secrets, clientTemplates,
                               clientsService.ts
    src/thirdparty.ts          loads Web Awesome at RUNTIME from https://static.logout.work/webawesome/3.2.1
    api/                       GENERATED OpenAPI types + zod schemas (do not hand-edit types.gen.ts / zod.gen.ts)
    generation/openApi.ts      codegen script
```

Import aliases (tsconfig + vite): `@db/client/*` → `client/src/*`, `@db/api` → `client/api/index.ts`. Relative imports use `.js` extensions (NodeNext).

### Adding an admin feature (the usual loop)

1. **Server service** in `Api/<Area>/<Thing>Service.cs` (primary-constructor style, inject `IdDbContext`, `UserManager`, etc.). Register it in that area's `*Startup.cs`.
2. **Controller** in the same folder: `[Authorize(Policies.RequiresAdmin)] [ApiController] [Route("api/[controller]")]`.
3. Child collections (client redirect URIs, scopes, ...) load only with explicit `.Include(...)`; without it they're silently empty. Return DTOs, never entities. See `ClientsService.FullClient`.
4. Errors: throw `ApplicationException` / `ArgumentException` for 400, `SecurityException` for 403. `GlobalExceptionHandler` turns them into ProblemDetails. Don't hand-roll error responses.
5. **Regenerate client types**: run the server in Development on `http://localhost:5057`, then `cd Id.Admin/client && npm run open-api`. This writes `api/v1.json`, `types.gen.ts`, `zod.gen.ts`. If you add new types, also update the re-export list in `api/index.ts`.
6. **Client**: add calls to the area's `*Service.ts` using `apiService` (base path `api`), then a Lit component (`@customElement`, legacy decorators: `experimentalDecorators: true`, `useDefineForClassFields: false`). Import it from `main.ts` or the view's `index.ts`.
7. Build client (`npm run build`) so it lands in `server/wwwroot`, then run the server.

JSON conventions: camelCase, case-insensitive input, enums as strings, strict numbers. They're configured once in `ApiStartup.ConfigureJson`, for both MVC and the minimal-API JSON options, because the OpenAPI generator reads the latter. If they diverge, the generated types are wrong (numeric enums, `number | string`).

### Client gotchas (Web Awesome 3.2.1 + Lit)

- `wa-checkbox` / `wa-switch`: bind **both** `?checked=${v} .checked=${v}`. On first update they reset `checked` from the `checked` attribute, so a property-only binding renders unchecked, and a later save would silently write `false`.
- `wa-select`: give `wa-option` **static** `value="..."` strings, not bindings. Lit sets the select's `.value` before bound option values, and the select drops values that don't match an option.
- `wa-button type="submit"` doesn't reliably submit a `<form>` inside a component's shadow root. Use explicit `@click` and Enter handlers.
- `wa-input`'s inner `<input>` lives in its own shadow root, so a `<datalist>` from your component can't reach it. `string-list-editor` shows suggestions as quick-add buttons instead.
- Never name a component method `remove()`, because it shadows `HTMLElement.remove()`.
- Close a dialog from its content with `this.dispatchEvent(new CustomEvent("close-dialog"))`.

## Commands

```bash
dotnet build Id.slnx
dotnet test --project Id.Tests          # needs Docker (Testcontainers starts postgres:17)

# Run Id.Admin (http://localhost:5057). Set ConnectionStrings__AuthDb, optionally AUTH_SERVER/AUTH_CLIENT.
cd Id.Admin/client && npm ci && npm run build      # or: npm run watch
dotnet run --project Id.Admin/server

cd Id.Admin/client && npm run ts                   # typecheck
cd Id.Admin/client && npm run open-api             # codegen; server must be running in Development

# EF migrations: Id.Lib has a design-time factory, no startup project needed
dotnet ef migrations add <Name> --project Id.Lib -o Migrations
ConnectionStrings__AuthDb="..." dotnet ef database update --project Id.Lib

# Containers (from repo root; build context is the root)
podman build -f Id.Admin/Containerfile --tag localhost/useradmin:latest .
```

Ops endpoints (both apps, anonymous, defined in `Id.Lib/Observe/OpsEndpoints.cs`):
- `/livez` is liveness. It checks no dependencies, so it never fails because of the DB.
- `/readyz` is readiness. It includes an EF `CanConnect` check on `IdDbContext` and returns 503 when unhealthy.
- `/version` returns the build stamp as JSON.

These paths are excluded from OTel tracing. Add readiness-only checks with `tags: ["ready"]`.

Build stamp: CI passes `APP_VERSION` / `APP_COMMIT` / `APP_BUILD_TIME` as `--build-arg`s (never name them `VERSION` etc.: ARGs become env vars, which MSBuild reads as properties and which then override `$(Version)`). The Containerfiles forward them as `-p:BuildVersion/BuildCommit/BuildTime`, and the root `Directory.Build.props` writes them into the assemblies. `Id.Lib/Services/BuildInfo.cs` reads them, and both layouts show `BuildInfo.Current.Display` in the footer. Images also get OCI `version`/`revision`/`created` labels. Keep the ARGs in both the `build` and `publish` stages, because ARGs aren't inherited across `FROM`.

CI: `.forgejo/workflows/*-build-push.yml` builds both images on push to `main` or on tags, and pushes them to `dev.logout.work/empire/{aspauth,useradmin}`. `auth-build-push.yml` still points at the old `AspAuth.Local/Containerfile` path; repoint it to `Id.Server` before merging. Remotes: `origin` (Forgejo, primary) and `github` (mirror).

Verify with `dotnet build Id.slnx`, `dotnet test --project Id.Tests`, `npm run ts`, and by exercising the app. Protocol behaviour (redirect URI matching, PKCE, code reuse, token claims) gets an integration test for every rule.

## Configuration & secrets

- `ConnectionStrings:AuthDb` is required by both apps (startup throws if empty).
- ID server: `Authentication:Google:*`, `Authentication:EntraId:*`, and `Email:*` (MailKit SMTP). Secrets go in user-secrets or env vars, **never in `appsettings.json`**.
- `OTEL_EXPORTER_OTLP_ENDPOINT` enables OTLP export. Without it, OTel is wired but nothing is exported.
- Passkeys: `ServerDomain` is `logout.work` outside Development.

## Gotchas / known issues

- Bootstrapping the first admin: there is intentionally no self-service "make me admin" path. Grant `Admin` to a user directly in the DB (`AspNetRoles` + `AspNetUserRoles`) or from an existing admin via `PUT /api/users/{id}/roles`. Never add an endpoint that lets a non-admin assign roles.
- Id.Admin `Logout` only clears the local cookie. It doesn't do an OIDC sign-out at the ID server.
- The npm `@awesome.me/webawesome` package is used for types/dev only. At runtime, components load from the pinned CDN URL in `thirdparty.ts` (3.2.1). Keep the two roughly in sync.
- `Id.Admin/Containerfile` node stage: `WORKDIR /usr/build/client` is an empty dir. It only works because npm walks up to `/usr/build/package.json`, and vite's `../server/wwwroot` then resolves to `/usr/server/wwwroot`. Be careful editing it.
- Upgrades deliberately held back (Nov 2026): .NET 11 / EF 11 (needs SDK + base-image bump), MailKit 5, TypeScript 7, Vite 8, `@hey-api/openapi-ts` 0.99 (fixes the remaining dev-only `js-yaml` audit finding, but may change the codegen config/output).
