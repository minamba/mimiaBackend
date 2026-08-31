# Build depuis la racine de la solution :
#   docker compose build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Les .csproj d'abord : cette couche n'est reconstruite que si les dépendances
# changent, donc le restore est mis en cache entre deux déploiements.
COPY SchoolWebApp.Domain/*.csproj SchoolWebApp.Domain/
COPY SchoolWebApp.Dal/*.csproj SchoolWebApp.Dal/
COPY SchoolWebApp/*.csproj SchoolWebApp/
RUN dotnet restore SchoolWebApp/SchoolWebApp.Api.csproj

COPY SchoolWebApp.Domain/ SchoolWebApp.Domain/
COPY SchoolWebApp.Dal/ SchoolWebApp.Dal/
COPY SchoolWebApp/ SchoolWebApp/
RUN dotnet publish SchoolWebApp/SchoolWebApp.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Utilisateur non-root : l'image aspnet fournit `app` (uid 1654).
USER app

ENTRYPOINT ["dotnet", "SchoolWebApp.Api.dll"]
