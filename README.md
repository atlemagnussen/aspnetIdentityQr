# auth local

dotnet tool install --global dotnet-ef
or
dotnet tool update --global dotnet-ef

dotnet ef migrations add InitialCreatePostgres

dotnet ef database update

package add Microsoft.Identity.Web
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.11

## clear

drop table public."AspNetRoleClaims"
drop table public."AspNetUserClaims"
drop table public."AspNetUserRoles"
drop table public."AspNetUserTokens"
drop table public."AspNetUserLogins"
drop table public."AspNetRoles"
drop table public."AspNetUsers"