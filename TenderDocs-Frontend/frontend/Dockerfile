# ─────────────────────────────────────────────────────────────
# TenderDocs frontend — build the Vite SPA and serve it with nginx.
# The gateway (in TenderDocs-Backend) proxies / to this container on :80.
# ─────────────────────────────────────────────────────────────

# ---- Build stage ----
FROM node:20-alpine AS build
WORKDIR /app

# Install deps first (better layer caching)
COPY package*.json ./
RUN npm ci

# Build the app. The SPA calls the API at VITE_API_BASE_URL (default /api, same-origin).
# VITE_GOOGLE_CLIENT_ID is baked into the bundle at build time so the "Sign in with Google"
# button works — without it the login screen shows "Google sign-in isn't configured".
COPY . .
ARG VITE_API_BASE_URL=/api
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ARG VITE_GOOGLE_CLIENT_ID=VITE_GOOGLE_CLIENT_ID 
ENV VITE_GOOGLE_CLIENT_ID=$VITE_GOOGLE_CLIENT_ID
RUN npm run build

# ---- Runtime stage ----
FROM nginx:1.27-alpine AS runtime
# SPA-aware nginx config (history fallback + asset caching + gzip)
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
  CMD wget -qO- http://localhost:80/ >/dev/null 2>&1 || exit 1
CMD ["nginx", "-g", "daemon off;"]
