ARG DOTNET_VERSION=10.0

# Stage 1: Runtime base with security hardening
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-alpine AS base

RUN apk update && apk upgrade --no-cache && \
    apk add --no-cache \
    icu-libs \
    tzdata \
    curl \
    ca-certificates

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_URLS=http://+:8080

RUN addgroup -g 10001 -S appgroup && \
    adduser -u 10001 -S appuser -G appgroup

WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Stage 2: SDK build & dependency restore
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-alpine AS build
WORKDIR /src

COPY SocarDispatch.slnx ./
COPY src/SocarDispatch.Domain/SocarDispatch.Domain.csproj src/SocarDispatch.Domain/
COPY src/SocarDispatch.Application/SocarDispatch.Application.csproj src/SocarDispatch.Application/
COPY src/SocarDispatch.Infrastructure/SocarDispatch.Infrastructure.csproj src/SocarDispatch.Infrastructure/
COPY src/SocarDispatch.API/SocarDispatch.API.csproj src/SocarDispatch.API/

RUN dotnet restore src/SocarDispatch.API/SocarDispatch.API.csproj

COPY src/ src/

WORKDIR /src/src/SocarDispatch.API
RUN dotnet publish SocarDispatch.API.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# Stage 3: Final production image
FROM base AS final
WORKDIR /app

COPY --from=build /app/publish .
RUN chown -R appuser:appgroup /app

USER appuser

HEALTHCHECK --interval=15s --timeout=5s --start-period=20s --retries=3 \
    CMD curl -f http://localhost:8080/healthz || exit 1

ENTRYPOINT ["dotnet", "SocarDispatch.API.dll"]
