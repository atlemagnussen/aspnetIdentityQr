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
- Roles are the `UserRoles` enum (`AspAuth.Lib/Models/UserRoles.cs`), currently only `Admin`. Roles are created on demand by `UserDataService.EnsureRoleExist`.
- UserAdmin authorization: policy `Policies.RequiresAdmin` = authenticated + `role` claim `Admin`. **Every API controller must carry `[Authorize(Policies.RequiresAdmin)]`.** Razor pages only use `[Authorize]`, because they're shells that load Lit components and all data goes through the API.
- Duende is configured **without** config-store caching, so client/redirect/CORS changes written by UserAdmin take effect on the ID server immediately.
- Both apps sit behind a reverse proxy: forwarded headers trust all proxies (`KnownProxies/KnownIPNetworks` cleared).

## UserAdmin layout

```
UserAdmin/
  server/                      ASP.NET Core (Razor Pages shell + API controllers)
    Program.cs
    Auth/                      OIDC + cookie, policies
    Persistence/               registers ApplicationDbContext, IdentityCore+roles, ConfigurationDbContext
    Api/
      ApiStartup.cs            controllers, JSON options, OpenAPI (3.1, served at /openapi/v1.json in Development)
      GlobalExceptionHandler.cs
      Users/                   UsersController -> UserDataService (UserManager/RoleManager) -> UserDTO
      Configuration/
        Clients/ ApiResources/ IdentityResources/ IdentityProviders/   Controller + Service each
    Pages/                     Index, Manager (<users-list>), Permissions (<clients-list>), UserInfo, Logout
    wwwroot/                   BUILD OUTPUT of client (gitignored)
  client/                      Vite + TS + Lit
    src/main.ts                entry; imports views & components
    src/services/httpService.ts  fetch wrapper (cookies, ProblemDetails errors)
    src/views/users/           users-list, user details dialog, userService.ts
    src/views/configuration/   clients-list, client edit dialog, configurationService.ts
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

JSON conventions: camelCase, case-insensitive input, enums as strings (`JsonStringEnumConverter(allowIntegerValues: false)`).

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
