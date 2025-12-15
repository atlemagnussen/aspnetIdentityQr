# asp.net core Identity

## Passkeys

https://learn.microsoft.com/en-us/aspnet/core/security/authentication/passkeys/

update the Identity configuration to use schema version 3, which includes passkey support:
```cs
options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
```

## QR code 2Fa

With QR code enabled and removed all default jquery and bootstrap

Also examples of usage of logging in with Microsoft and Google

Then also added Duende Identity Server on top to provide OAUTH2 and OIDC capabilities

Just intended as an example

And build/run examples with podman as container

## Just some EF commands

dotnet tool install --global dotnet-ef
or
dotnet tool update --global dotnet-ef

dotnet ef migrations add InitialCreatePostgres

dotnet ef migrations add AddCryptoSigningKeys --project ../AspAuth.Lib -c ApplicationDbContext

dotnet ef migrations add Addpasskeys --project ../AspAuth.Lib -c ApplicationDbContext

dotnet ef database update -c ApplicationDbContext




package add Microsoft.Identity.Web
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.11

dotnet add package Microsoft.AspNetCore.DataProtection.EntityFrameworkCore --version 8.0.15

dotnet ef migrations add InitialDataProtection --context DataProtectionContext
dotnet ef database update --context DataProtectionContext


dotnet ef migrations add InitialIdentityServerConfiguration --project ../AspAuth.Lib -c ConfigurationDbContext
dotnet ef migrations add InitialIdentityServerOperational --project ../AspAuth.Lib -c PersistedGrantDbContext

dotnet ef database update -c ConfigurationDbContext
dotnet ef database update -c PersistedGrantDbContext
## clear

drop table public."AspNetRoleClaims"
drop table public."AspNetUserClaims"
drop table public."AspNetUserRoles"
drop table public."AspNetUserTokens"
drop table public."AspNetUserLogins"
drop table public."AspNetRoles"
drop table public."AspNetUsers"