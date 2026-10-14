import { NextResponse, type NextRequest } from "next/server";

// Renews the access token before the page renders. Server components cannot
// set cookies, so this is the one place a short-lived token is swapped for a
// new one, for the current request and for the browser.
const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5155";
const AT = "rx_at";
const RT = "rx_rt";
const EXP = "rx_exp";
const USER = "rx_user";

const options = (expires: Date) => ({
  httpOnly: true,
  sameSite: "lax" as const,
  secure: process.env.NODE_ENV === "production",
  path: "/",
  expires,
});

type Tokens = {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAtUtc: string;
  refreshTokenExpiresAtUtc: string;
};
type Outcome = { tokens: Tokens } | { rejected: true } | { failed: true };

// A page load fires several requests at once, all carrying the same expired
// token. The API swaps a refresh token only once, so the first answer is
// shared with the others (and kept for a few seconds for late ones).
const shared = new Map<string, Promise<Outcome>>();

function refreshOnce(refresh: string): Promise<Outcome> {
  let call = shared.get(refresh);
  if (!call) {
    call = (async (): Promise<Outcome> => {
      try {
        const res = await fetch(`${API_BASE}/api/auth/refresh`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ refreshToken: refresh }),
          cache: "no-store",
        });
        if (res.status === 400 || res.status === 401) return { rejected: true };
        if (!res.ok) return { failed: true };
        return { tokens: (await res.json()) as Tokens };
      } catch {
        return { failed: true };
      }
    })();
    shared.set(refresh, call);
    setTimeout(() => shared.delete(refresh), 15_000);
  }
  return call;
}

export async function proxy(request: NextRequest) {
  const refresh = request.cookies.get(RT)?.value;
  if (!refresh) return NextResponse.next();

  const expires = Number(request.cookies.get(EXP)?.value ?? 0);
  const stillGood = request.cookies.get(AT)?.value && expires - Date.now() > 30_000;
  if (stillGood) return NextResponse.next();

  const outcome = await refreshOnce(refresh);

  if ("rejected" in outcome) {
    // The refresh token is finished: sign the person out.
    const out = NextResponse.next();
    for (const name of [AT, RT, EXP, USER]) out.cookies.delete(name);
    return out;
  }
  if ("failed" in outcome) return NextResponse.next(); // API trouble: keep the session

  const { tokens } = outcome;
  const refreshEnds = new Date(tokens.refreshTokenExpiresAtUtc);
  const accessEnds = String(new Date(tokens.accessTokenExpiresAtUtc).getTime());

  // The page being rendered must see the new tokens too.
  request.cookies.set(AT, tokens.accessToken);
  request.cookies.set(RT, tokens.refreshToken);
  request.cookies.set(EXP, accessEnds);
  const response = NextResponse.next({ request: { headers: request.headers } });

  response.cookies.set(AT, tokens.accessToken, options(refreshEnds));
  response.cookies.set(RT, tokens.refreshToken, options(refreshEnds));
  response.cookies.set(EXP, accessEnds, options(refreshEnds));
  return response;
}

export const config = {
  // Pages only: not static files, images or the API proxy.
  matcher: ["/((?!api|_next/static|_next/image|favicon.ico|.*\\..*).*)"],
};
