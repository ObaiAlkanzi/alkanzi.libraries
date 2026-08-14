# syntax=docker/dockerfile:1
#
# Image for the ERP Blazor Server app.
#
# The build context is the REPO ROOT, not this folder — the app has a
# ProjectReference into src/Alkanzi.ErpServices, so the context has to be able to
# see both. Build it with:
#
#   docker build -f apps/Alkanzi.Erp.Web/Dockerfile -t alkanzi-erp-web:local .
#
# or just `docker compose build` from the root, which sets that up for you.

# ---- build -------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Restore against the project files alone first, so editing a .cs or .razor file
# reuses the cached restore layer instead of re-downloading every package.
COPY apps/Alkanzi.Erp.Web/Alkanzi.Erp.Web.csproj apps/Alkanzi.Erp.Web/
COPY src/Alkanzi.ErpServices/Alkanzi.ErpServices.csproj src/Alkanzi.ErpServices/
RUN dotnet restore apps/Alkanzi.Erp.Web/Alkanzi.Erp.Web.csproj

COPY apps/Alkanzi.Erp.Web/ apps/Alkanzi.Erp.Web/
COPY src/Alkanzi.ErpServices/ src/Alkanzi.ErpServices/

# UseAppHost=false: the entrypoint below runs the .dll through `dotnet`, so the
# native launcher is dead weight.
RUN dotnet publish apps/Alkanzi.Erp.Web/Alkanzi.Erp.Web.csproj \
    --configuration $BUILD_CONFIGURATION \
    --no-restore \
    --output /app/publish \
    -p:UseAppHost=false

# ---- runtime -----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Deliberately NOT the -alpine tag and deliberately no
# DOTNET_SYSTEM_GLOBALIZATION_INVARIANT: Oracle's managed ADO.NET driver needs a
# real ICU, and throws on an invariant-globalization runtime.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# The aspnet image ships no HTTP client, so the healthcheck below has nothing to
# probe with until we add one. curl is the smallest thing that does the job.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# start-period covers first-request JIT and the initial Oracle pool warm-up, so a
# cold container is not reported unhealthy before it has had a chance to answer.
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD curl -fsS http://localhost:8080/healthz || exit 1

COPY --from=build /app/publish .

# Pre-create the Data Protection key directory owned by the app user. Blazor
# Server signs antiforgery tokens and circuit state with these keys, so they have
# to outlive the container — compose mounts a volume here. Docker only inherits
# ownership for a volume path that already exists in the image; without this the
# mount lands as root and the app cannot read or write its own key ring.
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys \
    && chown -R $APP_UID:$APP_UID /home/app

# APP_UID is baked into the aspnet image (non-root, uid 1654).
USER $APP_UID

ENTRYPOINT ["dotnet", "Alkanzi.Erp.Web.dll"]
