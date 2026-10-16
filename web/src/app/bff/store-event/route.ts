import { getAccessToken } from "@/lib/session";

const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5155";
const KINDS = new Set(["view", "call", "whatsapp", "message"]);

/**
 * The store page tells that it was opened or that a contact button was used. The signed-in person's token rides
 * along (when there is one), so the owner looking at their own store is not counted. Never an error for the visitor.
 */
export async function POST(request: Request) {
  try {
    const body = (await request.json()) as { slug?: string; kind?: string };
    const slug = (body.slug ?? "").trim();
    const kind = (body.kind ?? "").trim();
    if (!slug || slug.length > 120 || !KINDS.has(kind)) return new Response(null, { status: 204 });

    const token = await getAccessToken();
    await fetch(`${API_BASE}/api/stores/${encodeURIComponent(slug)}/events`, {
      method: "POST",
      headers: { "Content-Type": "application/json", ...(token ? { Authorization: `Bearer ${token}` } : {}) },
      body: JSON.stringify({ kind }),
      cache: "no-store",
    });
  } catch {
    // Counting must never get in the way of the visitor.
  }
  return new Response(null, { status: 204 });
}
