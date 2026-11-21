# Kontekst budowania: katalog główny repozytorium (API potrzebuje contracts/ i fixtures/).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY api/Directory.Build.props api/
COPY api/Domain/BabyRadnik.Domain.csproj api/Domain/
COPY api/Infrastructure/BabyRadnik.Infrastructure.csproj api/Infrastructure/
COPY api/Api/BabyRadnik.Api.csproj api/Api/
RUN dotnet restore api/Api/BabyRadnik.Api.csproj
COPY api/ api/
RUN dotnet publish api/Api/BabyRadnik.Api.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out ./
COPY contracts/ /app/contracts/
COPY fixtures/ /app/fixtures/
COPY docs/prawo/ /app/docs/prawo/
ENV Paths__Root=/app \
    ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "BabyRadnik.Api.dll"]
