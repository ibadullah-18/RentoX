import "server-only";
import { getAccessToken } from "./session";

const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5155";

export type Result<T> =
  | { ok: true; data: T }
  | { ok: false; status: number; message: string };

/**
 * A call to the API on behalf of the signed-in person. Never cached (it is
 * personal). The proxy renews an expiring token before the page renders, so a
 * 401 here means the session really is over.
 */
export async function authed<T = void>(
  path: string,
  init: {
    method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
    query?: Record<string, string | number | undefined>;
    body?: unknown;
  } = {},
): Promise<Result<T>> {
  const token = await getAccessToken();
  if (!token) return { ok: false, status: 401, message: "unauthorized" };

  const url = new URL(path, API_BASE);
  for (const [key, value] of Object.entries(init.query ?? {})) {
    if (value !== undefined && value !== "") url.searchParams.set(key, String(value));
  }

  try {
    const res = await fetch(url, {
      method: init.method ?? "GET",
      headers: {
        Authorization: `Bearer ${token}`,
        ...(init.body !== undefined ? { "Content-Type": "application/json" } : {}),
      },
      body: init.body !== undefined ? JSON.stringify(init.body) : undefined,
      cache: "no-store",
    });

    if (!res.ok) return { ok: false, status: res.status, message: await problem(res) };
    if (res.status === 204) return { ok: true, data: undefined as T };
    const text = await res.text();
    return { ok: true, data: (text ? JSON.parse(text) : undefined) as T };
  } catch {
    return { ok: false, status: 0, message: "network" };
  }
}

/** The human part of a ProblemDetails answer. */
export async function problem(res: Response): Promise<string> {
  try {
    const data = (await res.json()) as {
      detail?: string;
      title?: string;
      errors?: Record<string, string[]>;
    };
    const first = data.errors ? Object.values(data.errors)[0]?.[0] : undefined;
    return first ?? data.detail ?? data.title ?? "unknown";
  } catch {
    return "unknown";
  }
}
