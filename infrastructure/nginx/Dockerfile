FROM nginx:alpine

# Upgrade packages and install utilities for health check and fallback TLS generation
RUN apk update && apk upgrade --no-cache && \
    apk add --no-cache openssl curl

# Remove default distribution configuration
RUN rm -f /etc/nginx/conf.d/default.conf

# Generate fallback self-signed TLS certificate for container boot validation
RUN mkdir -p /etc/nginx/ssl && \
    openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout /etc/nginx/ssl/tls.key \
    -out /etc/nginx/ssl/tls.crt \
    -subj "/CN=localhost/O=SOCAR Dispatch"

# Copy custom configurations
COPY nginx.conf /etc/nginx/nginx.conf
COPY conf.d/ /etc/nginx/conf.d/

EXPOSE 80 443

HEALTHCHECK --interval=15s --timeout=3s --retries=3 \
    CMD curl -f http://localhost:80/healthz || exit 1

CMD ["nginx", "-g", "daemon off;"]
