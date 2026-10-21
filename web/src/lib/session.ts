import "server-only";
import { cookies } from "next/headers";

// The tokens live in httpOnly cookies: the browser sends them back, but no
// script on the page (and so no injected script) can ever read them.
export const COOKIE = {
  access: "rx_at",
  refresh: "rx_rt",
  /** When the access token ends (epoch milliseconds), for the refresh check. */
  accessExpires: "rx_exp",
  /** Who is signed in, as JSON, for showing the name and phone. */
  user: "rx_user",
} as const;

export type AuthTokens = {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAtUtc: string;
  refreshTokenExpiresAtUtc: string;
};

export type Session = { userId: string; phone: string };

const base = {
  httpOnly: true,
  sameSite: "lax" as const,
  // Cookies are "secure" (sent only over HTTPS) on a real site. COOKIE_SECURE=false lets a private test over plain
  // http://<IP> keep the sign-in: a browser throws a secure cookie away when the address is not HTTPS.
  secure: process.env.COOKIE_SECURE ? process.env.COOKIE_SECURE === "true" : process.env.NODE_ENV === "production",
  path: "/",
};

/** Only from a server action or route handler: cookies cannot be set while rendering. */
export async function saveSession(tokens: AuthTokens, session: Session) {
  const jar = await cookies();
  const accessEnds = new Date(tokens.accessTokenExpiresAtUtc);
  const refreshEnds = new Date(tokens.refreshTokenExpiresAtUtc);

  jar.set(COOKIE.access, tokens.accessToken, { ...base, expires: refreshEnds });
  jar.set(COOKIE.accessExpires, String(accessEnds.getTime()), {
    ...base,
    expires: refreshEnds,
  });
  jar.set(COOKIE.refresh, tokens.refreshToken, { ...base, expires: refreshEnds });
  jar.set(COOKIE.user, JSON.stringify(session), { ...base, expires: refreshEnds });
}

export async function clearSession() {
  const jar = await cookies();
  for (const name of Object.values(COOKIE)) jar.delete(name);
}

/** The signed-in person, or null. Cheap: reads a cookie, calls nothing. */
export async function getSession(): Promise<Session | null> {
  const jar = await cookies();
  if (!jar.get(COOKIE.refresh)?.value) return null;
  try {
    const parsed = JSON.parse(jar.get(COOKIE.user)?.value ?? "") as Session;
    return parsed.userId ? parsed : null;
  } catch {
    return null;
  }
}

export async function getAccessToken(): Promise<string | null> {
  const jar = await cookies();
  return jar.get(COOKIE.access)?.value ?? null;
}

export async function getRefreshToken(): Promise<string | null> {
  const jar = await cookies();
  return jar.get(COOKIE.refresh)?.value ?? null;
}

const ROLE_CLAIM = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

/**
 * The roles in the access token. Only for deciding what to show: the API
 * checks the real token on every admin call, so a forged cookie gets nothing.
 */
export function rolesOf(token: string | null | undefined): string[] {
  if (!token) return [];
  try {
    const payload = JSON.parse(Buffer.from(token.split(".")[1], "base64url").toString("utf8")) as Record<
      string,
      unknown
    >;
    const raw = payload[ROLE_CLAIM] ?? payload.role;
    return Array.isArray(raw) ? raw.map(String) : raw ? [String(raw)] : [];
  } catch {
    return [];
  }
}

export async function getRoles(): Promise<string[]> {
  return rolesOf(await getAccessToken());
}

/**
 * The sections of the admin panel this person may use: null means all of them
 * (a SuperAdmin, or an Admin nobody limited). Only for deciding what to show;
 * the API checks every call.
 */
export async function getSections(): Promise<string[] | null> {
  const roles = await getRoles();
  if (roles.includes("SuperAdmin")) return null;

  const token = await getAccessToken();
  if (!token) return [];
  try {
    const payload = JSON.parse(Buffer.from(token.split(".")[1], "base64url").toString("utf8")) as Record<string, unknown>;
    const raw = payload["rx_section"];
    const list = Array.isArray(raw) ? raw.map(String) : raw ? [String(raw)] : [];
    return list.length === 0 ? null : list;
  } catch {
    return null;
  }
}

/**
 * May this person look after the staff (the table, the days off, the requests)? The owner, or an Admin who was
 * given that section on purpose: "no section chosen" does not include it.
 */
export async function canSeeHr(): Promise<boolean> {
  const sections = await getSections();
  if ((await getRoles()).includes("SuperAdmin")) return true;
  return sections?.includes("hr") ?? false;
}

export async function isAdmin(): Promise<boolean> {
  const roles = await getRoles();
  return roles.includes("Admin") || roles.includes("SuperAdmin");
}
