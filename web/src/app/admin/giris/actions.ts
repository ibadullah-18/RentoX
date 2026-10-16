"use server";

import { isValidPhone, normalizePhone } from "@/lib/phone";
import { redirect } from "next/navigation";
import { clientIpHeaders } from "@/lib/client-ip";
import { adminHref } from "@/lib/admin-path";
import { clearSession, getAccessToken, getRefreshToken, rolesOf, saveSession, type AuthTokens } from "@/lib/session";

const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5155";

export type AdminCodeSent =
  | { ok: true; challengeId: string; resendAt: number }
  | { ok: false; message: string };
export type AdminVerified = { ok: true } | { ok: false; message: string };

async function post(path: string, body: unknown) {
  return fetch(`${API_BASE}${path}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...(await clientIpHeaders()) },
    body: JSON.stringify(body),
    cache: "no-store",
  });
}

// One deliberately vague answer: the page must not tell a stranger whether a
// number is an admin's.
const refused = "Giriş mümkün olmadı. Məlumatları yoxlayın.";
const tooMany = "Çox tez-tez cəhd olunub. Bir az gözləyib yenidən yoxlayın.";

/** Sends the code. Only existing accounts: an admin panel never creates one. */
export async function adminRequestCode(phoneInput: string): Promise<AdminCodeSent> {
  if (!isValidPhone(phoneInput)) return { ok: false, message: "Nömrəni tam yazın: 9 rəqəm (məs. 50 123 45 67)." };

  try {
    const res = await post("/api/auth/login/otp", { phoneNumber: normalizePhone(phoneInput) });
    if (res.status === 429) return { ok: false, message: tooMany };
    if (!res.ok) return { ok: false, message: refused };
    const challenge = (await res.json()) as { challengeId: string; resendAvailableAtUtc: string };
    return {
      ok: true,
      challengeId: challenge.challengeId,
      resendAt: new Date(challenge.resendAvailableAtUtc).getTime(),
    };
  } catch {
    return { ok: false, message: "Serverə qoşulmaq olmadı." };
  }
}

/** Checks the code and signs in, but only an Admin or SuperAdmin is let in. */
export async function adminVerifyCode(input: { challengeId: string; code: string }): Promise<AdminVerified> {
  const code = input.code.replace(/\D/g, "");
  if (code.length !== 6) return { ok: false, message: "6 rəqəmli kodu yazın." };

  try {
    const res = await post("/api/auth/login/complete", { challengeId: input.challengeId, code });
    if (res.status === 429) return { ok: false, message: tooMany };
    if (!res.ok) return { ok: false, message: "Kod səhvdir və ya vaxtı bitib." };

    const data = (await res.json()) as AuthTokens & { userId: string; phoneNumber: string };
    const roles = rolesOf(data.accessToken);
    // Not an admin: nothing is saved, so this browser is not signed in at all.
    if (!roles.includes("Admin") && !roles.includes("SuperAdmin")) return { ok: false, message: refused };

    // The owner can close an Admin's access to the panel: then nothing is saved and the person is told so.
    if (!roles.includes("SuperAdmin")) {
      const access = await fetch(`${API_BASE}/api/admin/hr/me/access`, {
        headers: { Authorization: `Bearer ${data.accessToken}` },
        cache: "no-store",
      });
      if (access.ok && ((await access.json()) as { allowed: boolean }).allowed === false) {
        return { ok: false, message: "Admin panelə giriş icazəniz bağlıdır. Rəhbərlə əlaqə saxlayın." };
      }
    }

    await saveSession(data, { userId: data.userId, phone: data.phoneNumber });
    return { ok: true };
  } catch {
    return { ok: false, message: "Serverə qoşulmaq olmadı." };
  }
}

/** Signs out and returns to the admin sign-in page. */
export async function adminLogout() {
  const refresh = await getRefreshToken();
  const access = await getAccessToken();
  if (refresh) {
    try {
      await fetch(`${API_BASE}/api/auth/logout`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          ...(access ? { Authorization: `Bearer ${access}` } : {}),
        },
        body: JSON.stringify({ refreshToken: refresh }),
        cache: "no-store",
      });
    } catch {
      // The session is closed here even when the server cannot be reached.
    }
  }
  await clearSession();
  redirect(adminHref("/giris"));
}
