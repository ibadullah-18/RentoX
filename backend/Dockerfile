# syntax=docker/dockerfile:1
#
# One file, three images (build with --target):
#   api      the web API
#   worker   the background worker (push notifications, listing clean-up)
#   migrate  a one-shot program that brings the database up to date
#
# Build from the backend folder:  docker build -t rentox-api --target api --build-arg SIXLABORS_LICENSE_KEY=... .
#
# ImageSharp (the image library) needs a licence key for a Release build: without it the build stops. Get the free
# Community key (or buy one) at https://sixlabors.com/pricing/ and pass it as the build argument above. The key is
# only used in the build stage and is not in the finished images.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
ARG SIXLABORS_LICENSE_KEY=""
# Release for the real server. "Debug" only to try the deploy files on a machine without the licence key.
ARG BUILD_CONFIGURATION=Release
# MSBuild reads environment variables as properties.
ENV SixLaborsLicenseKey=${SIXLABORS_LICENSE_KEY} \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1

COPY Directory.Build.props .editorconfig ./
COPY src/ src/

RUN dotnet publish src/RentoX.Api -c ${BUILD_CONFIGURATION} -o /out/api -p:NuGetAudit=false
RUN dotnet publish src/RentoX.Worker -c ${BUILD_CONFIGURATION} -o /out/worker -p:NuGetAudit=false

# The database migrations as one self-contained program (no SDK needed to run it).
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"
# The EF tool starts the configuration of the API to find the database model, so the settings the API insists on
# need some value here. They are only used to build the bundle (nothing connects), never the real ones.
RUN ConnectionStrings__Database="Host=build" ConnectionStrings__Redis="build:6379" \
    Jwt__SigningKey="build-only-build-only-build-only-build-only-build-only-build-only-1234" Otp__HashingKey="build-only-build-only-build-only-1234" \
    dotnet ef migrations bundle \
        --project src/RentoX.Infrastructure \
        --startup-project src/RentoX.Api \
        --configuration ${BUILD_CONFIGURATION} \
        --self-contained -r linux-x64 \
        -o /out/efbundle

# ---- API ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Storage__RootPath=/data/storage
# curl: for the container health check.
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /data/storage && chown -R app:app /data
COPY --from=build /out/api ./
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "RentoX.Api.dll"]

# ---- Worker ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS worker
WORKDIR /app
ENV DOTNET_ENVIRONMENT=Production \
    Storage__RootPath=/data/storage
RUN mkdir -p /data/storage && chown -R app:app /data
COPY --from=build /out/worker ./
USER app
ENTRYPOINT ["dotnet", "RentoX.Worker.dll"]

# ---- Migrations ----
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS migrate
WORKDIR /app
COPY --from=build /out/efbundle ./efbundle
RUN chmod +x ./efbundle
# The connection string comes as the argument:  ./efbundle --connection "Host=...;..."
ENTRYPOINT ["./efbundle"]
