# syntax=docker/dockerfile:1
#
# The website (Next.js). Build from the web folder:
#   docker build -t rentox-web --build-arg API_BASE_URL=http://api:8080 .
# API_BASE_URL is the address of the API as the website reaches it inside the server (it is also baked into the
# routing rules at build time, so it has to be given here).

FROM node:22-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci

FROM node:22-alpine AS build
WORKDIR /app
ARG API_BASE_URL=http://api:8080
ENV API_BASE_URL=${API_BASE_URL} \
    NEXT_TELEMETRY_DISABLED=1
COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN npm run build

FROM node:22-alpine AS run
WORKDIR /app
ARG API_BASE_URL=http://api:8080
ENV NODE_ENV=production \
    NEXT_TELEMETRY_DISABLED=1 \
    API_BASE_URL=${API_BASE_URL} \
    HOSTNAME=0.0.0.0 \
    PORT=3000
RUN addgroup -S web && adduser -S web -G web
COPY --from=build --chown=web:web /app/.next/standalone ./
COPY --from=build --chown=web:web /app/.next/static ./.next/static
COPY --from=build --chown=web:web /app/public ./public
USER web
EXPOSE 3000
CMD ["node", "server.js"]
