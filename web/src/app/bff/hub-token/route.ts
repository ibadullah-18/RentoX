import { getAccessToken } from "@/lib/session";

/**
 * The live connection (SignalR) cannot send our httpOnly cookie to the API,
 * so the page asks for the access token here, keeps it in memory only and
 * asks again whenever it reconnects. The proxy has already renewed it if it
 * was about to end.
 */
export async function GET() {
  const token = await getAccessToken();
  if (!token) return Response.json({ message: "unauthorized" }, { status: 401 });

  return Response.json(
    { token },
    { headers: { "Cache-Control": "private, no-store" } },
  );
}
