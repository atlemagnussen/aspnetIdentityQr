# auth local

dotnet tool install --global dotnet-ef
or
dotnet tool update --global dotnet-ef

dotnet ef migrations add InitialCreate
dotnet ef database update

package add Microsoft.Identity.Web
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.11