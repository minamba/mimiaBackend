# Build depuis la racine de la solution :
#   docker compose build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY SchoolWebApp.IdentityServer/*.csproj SchoolWebApp.IdentityServer/
RUN dotnet restore SchoolWebApp.IdentityServer/SchoolWebApp.IdentityServer.csproj

COPY SchoolWebApp.IdentityServer/ SchoolWebApp.IdentityServer/
RUN dotnet publish SchoolWebApp.IdentityServer/SchoolWebApp.IdentityServer.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

USER app

ENTRYPOINT ["dotnet", "SchoolWebApp.IdentityServer.dll"]
