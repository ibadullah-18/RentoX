import "server-only";
import { headers } from "next/headers";

/**
 * The address of the visitor, as the reverse proxy in front of the site reports it. The site calls the API from its
 * own server, so without this the API would see one address for every visitor and its per-address limits (one-time
 * codes, sign-in) would count everybody together.
 */
export async function clientIpHeaders(): Promise<Record<string, string>> {
  const h = await headers();
  const ip = h.get("x-real-ip") ?? h.get("x-forwarded-for")?.split(",")[0]?.trim();
  return ip ? { "X-Forwarded-For": ip } : {};
}
