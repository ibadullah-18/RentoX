import { readFile } from "node:fs/promises";
import path from "node:path";
import { getBranding } from "@/lib/api";

const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5155";

/** /favicon.ico: the icon the owner uploaded, or the one that comes with the site. Browsers and search engines ask for this address. */
export async function GET() {
  const branding = await getBranding();
  if (branding.iconUrl) {
    try {
      const res = await fetch(new URL(branding.iconUrl, API_BASE), { next: { revalidate: 300, tags: ["branding"] } });
      if (res.ok) {
        return new Response(await res.arrayBuffer(), {
          headers: {
            "Content-Type": res.headers.get("Content-Type") ?? "image/png",
            "Cache-Control": "public, max-age=3600",
          },
        });
      }
    } catch {
      // The API is not reachable: the icon that comes with the site is shown instead.
    }
  }
  const file = await readFile(path.join(process.cwd(), "public", "brand", "favicon.ico"));
  return new Response(new Uint8Array(file), {
    headers: { "Content-Type": "image/x-icon", "Cache-Control": "public, max-age=3600" },
  });
}
