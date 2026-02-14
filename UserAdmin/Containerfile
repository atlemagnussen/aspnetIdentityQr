
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM node:24-slim AS buildnode

WORKDIR /usr/build
COPY UserAdmin/client .
RUN mkdir -p ../server/wwwroot

WORKDIR /usr/build/client
RUN npm install
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY . .

RUN mkdir -p UserAdmin/server/wwwroot
WORKDIR "/src/UserAdmin/server/wwwroot"
COPY --from=buildnode /usr/server/wwwroot .

WORKDIR "/src/UserAdmin/server"
RUN dotnet build "./UserAdmin.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./UserAdmin.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "UserAdmin.dll"]
