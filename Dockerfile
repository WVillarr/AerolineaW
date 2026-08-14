# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, from just the csproj files and the central package management props, so this
# layer only rebuilds when a dependency changes — not on every source edit.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/AerolineaW.Domain/AerolineaW.Domain.csproj src/AerolineaW.Domain/
COPY src/AerolineaW.Application/AerolineaW.Application.csproj src/AerolineaW.Application/
COPY src/AerolineaW.Infrastructure/AerolineaW.Infrastructure.csproj src/AerolineaW.Infrastructure/
COPY src/AerolineaW.Api/AerolineaW.Api.csproj src/AerolineaW.Api/
RUN dotnet restore src/AerolineaW.Api/AerolineaW.Api.csproj

COPY src/ src/
RUN dotnet publish src/AerolineaW.Api/AerolineaW.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN adduser --disabled-password --gecos "" appuser \
    && chown -R appuser /app
USER appuser

COPY --from=build --chown=appuser /app .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AerolineaW.Api.dll"]
