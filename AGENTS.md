# AGENTS.md

Guidance for coding agents (and humans) working in this repo.

## What this is

Two deployable apps sharing one PostgreSQL database and one class library:

| Project | Role |
|---|---|
| `AspAuth.Local/` | **The ID server.** ASP.NET Core Identity (Razor Pages UI, scaffolded under `Areas/Identity`) with **Duende IdentityServer 7** on top for OAuth2/OIDC. Google + Microsoft external logins, TOTP 2FA with QR, passkeys (Identity schema v3). Deployed as `id.logout.work`. |
| `UserAdmin/` | **Admin app** for managing the data behind the ID server: users/roles (ASP.NET Identity tables) and Duende configuration (clients, API/identity resources, identity providers). Logs in *via* the ID server over OIDC. Deployed as `user.logout.work`. |
| `AspAuth.Lib/` | Shared library: `DbContext`s, **all EF migrations**, models, Identity/Duende/cookie/OTel setup extensions. Referenced by both apps. |
| `AspAuth.ConsoleApp/` | Scratch app for testing the MailKit sender. Not deployed. |

Solutions: `AspAuthLocal.slnx` (ID server + Lib + console) and `UserAdmin/UserAdmin.slnx` (UserAdmin server + Lib).

Stack: .NET 10 (`net10.0`), EF Core 10 + Npgsql, Duende IdentityServer 7.4, OpenTelemetry. UserAdmin client: TypeScript + Lit + Vite, Web Awesome components.

## Data model (one Postgres DB, four contexts)

All contexts use connection string `ConnectionStrings:AuthDb` (env: `ConnectionStrings__AuthDb`). Migrations live in `AspAuth.Lib/Migrations/`.

| Context | Owns | Migrations folder |
|---|---|---|
| `ApplicationDbContext` (`IdentityDbContext<ApplicationUser>`) | `AspNet*` Identity tables (incl. passkeys), `UserProfile` (1:1 with user, key `UserId`, holds `FullName`), `SigningKeys` (`CryptoSigningKey`, JWK stored as `jsonb`) | `Migrations/` |
| `ConfigurationDbContext` (Duende) | Clients, ApiResources, ApiScopes, IdentityResources, IdentityProviders | `Migrations/ConfigurationDb/` |
| `PersistedGrantDbContext` (Duende) | Grants, device codes, server-side sessions | `Migrations/PersistedGrantDb/` |
| `DataProtectionContext` | ASP.NET Data Protection keys (app name `aspauth`) | `Migrations/DataProtection/` |

`ApplicationConfigurationDbContext` exists only so `dotnet ef` can target the configuration store; don't use it at runtime.

Rules:
- **Schema is owned by `AspAuth.Lib`.** UserAdmin must never add migrations; add them in Lib and apply them manually.
- Neither app auto-migrates (`MigrateDb()` is commented out). Migrations are applied by hand (commands below).
- `SeedIdentityServerSetup` holds the original seed (clients `web`, scopes, resources) but is not called at startup. Production config is now edited through UserAdmin, so treat the seed as historical.

## How auth ties the two apps together

- UserAdmin authenticates with cookie + OIDC (`UserAdmin/server/Auth/AuthenticationStartup.cs`). Authority comes from env `AUTH_SERVER` (default `https://id.logout.work`) and client from `AUTH_CLIENT` (default `web`). Scopes: `openid profile roles`. `MapInboundClaims = false`, so claim types are raw JWT names (`role`, `name`, `sub`).
- The ID server emits `role` claims (and `fullname`) from `CustomProfileService` (`AspAuth.Lib/Services/ProfileService.cs`), using the user's ASP.NET Identity roles.
- Roles are free-form (managed on the Roles page). The `UserRoles` enum (`AspAuth.Lib/Models/UserRoles.cs`, currently only `Admin`) lists the *system* roles, which can't be renamed or deleted.
- UserAdmin authorization: policy `Policies.RequiresAdmin` = authenticated + `role` claim `Admin`. **Every API controller and admin page must carry `[Authorize(Policies.RequiresAdmin)]`.** Logged-in non-admins land on `/AccessDenied`.
- Challenges: API calls (`/api/*`) get plain 401/403 from the cookie handler. Pages are challenged with OIDC (a redirect to the ID server). A policy scheme in `AuthenticationStartup` does the routing. Don't call `options.Events.RedirectToX` from inside `OnRedirectToX`; that recursion crashes the process.
- **Shared Data Protection key ring:** UserAdmin calls `AddDataProtectionAuth` (same DB table + app name `aspauth` as the ID server). Password-reset tokens generated in UserAdmin therefore validate on the ID server's `/Identity/Account/ResetPassword` page. Because the key ring is shared, UserAdmin's auth cookie uses its own scheme name (`UserAdmin`, cookie `.UserAdmin.Auth`). The scheme name is part of the protector purpose, so an ID server cookie can never be accepted here. Keep it distinct.
- Self-protection, enforced server-side: an admin can't delete or lock themselves or remove their own Admin role. The client UserAdmin signs in with (`AUTH_CLIENT`) can't be disabled or deleted.
- Duende is configured **without** config-store caching, so client/redirect/CORS changes written by UserAdmin take effect on the ID server immediately.
- Both apps sit behind a reverse proxy: forwarded headers trust all proxies (`KnownProxies/KnownIPNetworks` cleared).

## UserAdmin layout

```
UserAdmin/
  server/                      ASP.NET Core (Razor Pages shell + API controllers)
    Program.cs
    Auth/                      OIDC + cookie + challenge policy scheme, policies, UserAdminOptions, CurrentUser (sub)
    Persistence/               ApplicationDbContext, IdentityCore (+roles, token providers, schema v3), shared
                               Data Protection, ConfigurationDbContext
    Api/
      ApiStartup.cs            controllers, JSON options, OpenAPI (3.1, served at /openapi/v1.json in Development)
      GlobalExceptionHandler.cs
      Users/                   UsersService (via UserManager): CRUD, roles, claims, lock/unlock, 2FA reset,
                               external logins, passkeys, sign-out-everywhere, set-password links
      Roles/                   RolesService: free-form roles, system roles protected
      Configuration/
        Clients/               ClientsService: full client CRUD (ClientDto <-> EF entity, collections synced),
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

1. **Server service** in `Api/<Area>/<Thing>Service.cs` (primary-constructor style, inject `ApplicationDbContext`, `ConfigurationDbContext`, `UserManager`, etc.). Register it in that area's `*Startup.cs`.
2. **Controller** in the same folder: `[Authorize(Policies.RequiresAdmin)] [ApiController] [Route("api/[controller]")]`.
3. Duende entities: load with explicit `.Include(...)` for every child collection you need, then convert with `Duende.IdentityServer.EntityFramework.Mappers` `.ToModel()` / `.ToEntity()`. `ToModel()` on an entity without its collections included silently returns empty lists. See `ClientsService.FullClient`.
4. Errors: throw `ApplicationException` / `ArgumentException` for 400, `SecurityException` for 403. `GlobalExceptionHandler` turns them into ProblemDetails. Don't hand-roll error responses.
5. **Regenerate client types**: run the server in Development on `http://localhost:5057`, then `cd UserAdmin/client && npm run open-api`. This writes `api/v1.json`, `types.gen.ts`, `zod.gen.ts`. If you add new types, also update the re-export list in `api/index.ts`.
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
# Build
dotnet build AspAuthLocal.slnx
dotnet build UserAdmin/UserAdmin.slnx

# Run ID server (needs ConnectionStrings:AuthDb; AspAuth.Local has UserSecretsId, so use user-secrets)
dotnet run --project AspAuth.Local

# Run UserAdmin (http://localhost:5057, https://localhost:7028). Set ConnectionStrings__AuthDb,
# and optionally AUTH_SERVER/AUTH_CLIENT to point at a local ID server.
cd UserAdmin/client && npm ci && npm run build      # or: npm run watch
dotnet run --project UserAdmin/server

# Client typecheck / codegen
cd UserAdmin/client && npm run ts
cd UserAdmin/client && npm run open-api             # server must be running in Development

# EF migrations: run from AspAuth.Local (startup project), target the Lib
cd AspAuth.Local
dotnet ef migrations add <Name> --project ../AspAuth.Lib -c ApplicationDbContext
dotnet ef database update -c ApplicationDbContext
dotnet ef database update -c ConfigurationDbContext
dotnet ef database update -c PersistedGrantDbContext
dotnet ef database update -c DataProtectionContext

# Containers (from repo root; build context is the root)
podman build -f AspAuth.Local/Containerfile --tag localhost/aspauth:latest .
podman build -f UserAdmin/Containerfile    --tag localhost/useradmin:latest .
```

Ops endpoints (both apps, anonymous, defined in `AspAuth.Lib/Observe/OpsEndpoints.cs`):
- `/livez` is liveness. It checks no dependencies, so it never fails because of the DB.
- `/readyz` is readiness. It includes an EF `CanConnect` check on `ApplicationDbContext` and returns 503 when unhealthy.
- `/version` returns the build stamp as JSON.

These paths are excluded from OTel tracing. Add readiness-only checks with `tags: ["ready"]`.

Build stamp: CI passes `APP_VERSION` / `APP_COMMIT` / `APP_BUILD_TIME` as `--build-arg`s (never name them `VERSION` etc.: ARGs become env vars, which MSBuild reads as properties and which then override `$(Version)`). The Containerfiles forward them as `-p:BuildVersion/BuildCommit/BuildTime`, and the root `Directory.Build.props` writes them into the assemblies. `AspAuth.Lib/Services/BuildInfo.cs` reads them, and both layouts show `BuildInfo.Current.Display` in the footer. Images also get OCI `version`/`revision`/`created` labels. Keep the ARGs in both the `build` and `publish` stages, because ARGs aren't inherited across `FROM`.

CI: `.forgejo/workflows/*-build-push.yml` builds both images on push to `main` or on tags, and pushes them to `dev.logout.work/empire/{aspauth,useradmin}`. Remotes: `origin` (Forgejo, primary) and `github` (mirror).

There are **no automated tests**. Verify by building both solutions, `npm run ts`, and exercising the app.

## Configuration & secrets

- `ConnectionStrings:AuthDb` is required by both apps (startup throws if empty).
- ID server: `Authentication:Google:*`, `Authentication:EntraId:*`, and `Email:*` (MailKit SMTP). Secrets go in user-secrets or env vars, **never in `appsettings.json`**.
- `OTEL_EXPORTER_OTLP_ENDPOINT` enables OTLP export. Without it, OTel is wired but nothing is exported.
- Passkeys: `ServerDomain` is `logout.work` outside Development.

## Gotchas / known issues

- Bootstrapping the first admin: there is intentionally no self-service "make me admin" path. Grant `Admin` to a user directly in the DB (`AspNetRoles` + `AspNetUserRoles`) or from an existing admin via `PUT /api/users/{id}/roles`. Never add an endpoint that lets a non-admin assign roles.
- UserAdmin `Logout` only clears the local cookie. It doesn't do an OIDC sign-out at the ID server.
- UserAdmin registers `ConfigurationDbContext` manually with `new ConfigurationStoreOptions()` (it doesn't call `AddIdentityServer`). If Duende changes its store options/schema on upgrade, update `Persistence/PersistenceStartup.cs` to match.
- The npm `@awesome.me/webawesome` package is used for types/dev only. At runtime, components load from the pinned CDN URL in `thirdparty.ts` (3.2.1). Keep the two roughly in sync.
- `UserAdmin/Containerfile` node stage: `WORKDIR /usr/build/client` is an empty dir. It only works because npm walks up to `/usr/build/package.json`, and vite's `../server/wwwroot` then resolves to `/usr/server/wwwroot`. Be careful editing it.
- Upgrades deliberately held back (Nov 2026): .NET 11 / EF 11 (needs SDK + base-image bump), Duende IdentityServer 8 (major, migration guide + licensing), MailKit 5, TypeScript 7, Vite 8, `@hey-api/openapi-ts` 0.99 (fixes the remaining dev-only `js-yaml` audit finding, but may change the codegen config/output).
