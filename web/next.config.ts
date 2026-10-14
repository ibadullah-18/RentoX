import type { NextConfig } from "next";

// The .NET API. Server components call it directly; the browser (and the
// photos in <img>) go through /api/* on this same origin, so there is no CORS
// to configure and a CDN in front of the site can cache the photos.
const apiBase = process.env.API_BASE_URL ?? "http://localhost:5155";

const nextConfig: NextConfig = {
  // A second copy of the site (another port) needs its own build folder.
  distDir: process.env.NEXT_DIST_DIR ?? ".next",
  turbopack: {
    rules: {
      "*.css": {
        loaders: ["@tailwindcss/turbopack"],
        as: "*.css",
      },
    },
  },
  async rewrites() {
    return [
      { source: "/api/:path*", destination: `${apiBase}/api/:path*` },
      // The live connection (messages, typing). In production the reverse
      // proxy sends /hubs straight to the API, with WebSocket support.
      { source: "/hubs/:path*", destination: `${apiBase}/hubs/:path*` },
    ];
  },
};

export default nextConfig;
