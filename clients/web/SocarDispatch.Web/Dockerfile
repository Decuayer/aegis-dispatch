ARG DOTNET_VERSION=10.0
ARG NGINX_VERSION=alpine

# Stage 1: Build Blazor WebAssembly static bundle
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-alpine AS build
WORKDIR /src

# Copy project file and restore dependencies (layer cache optimization)
COPY SocarDispatch.Web.csproj ./
RUN dotnet restore SocarDispatch.Web.csproj

# Copy remaining source code and publish
COPY . ./
RUN dotnet publish SocarDispatch.Web.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# Stage 2: Serve via hardened Nginx Alpine image
FROM nginx:${NGINX_VERSION} AS final

LABEL org.opencontainers.image.title="socar-dispatch-web" \
      org.opencontainers.image.description="SOCAR Dispatch Web Panel (Blazor WebAssembly)" \
      org.opencontainers.image.vendor="SOCAR" \
      org.opencontainers.image.source="https://github.com/Decuayer/socar-dispatch"

# Remove default Nginx welcome page and configuration
RUN rm -rf /usr/share/nginx/html/* /etc/nginx/conf.d/default.conf

# Copy custom Nginx configuration for Blazor WASM
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy published static files from build stage
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

EXPOSE 80

HEALTHCHECK --interval=15s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:80/healthz || exit 1

CMD ["nginx", "-g", "daemon off;"]
