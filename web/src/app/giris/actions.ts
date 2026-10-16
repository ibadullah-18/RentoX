"use server";

import { getT } from "@/lib/i18n/server";
import { redirect } from "next/navigation";
import { clientIpHeaders } from "@/lib/client-ip";
import { problem } from "@/lib/authed";
import { isValidPhone, normalizePhone } from "@/lib/phone";
import {
  clearSession,
  getAccessToken,
  getRefreshToken,
  saveSession,
  type AuthTokens,
} from "@/lib/session";

const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5155";

const tr = async (text: string, vars?: Record<string, string | number>) => (await getT())(text, vars);

export type CodeSent =
  | {
      ok: true;
      challengeId: string;
      isRegistration: boolean;
      /** When the code may be asked for again (epoch ms). */
      resendAt: number;
    }
  | { ok: false; message: string };

export type Verified = { ok: true } | { ok: false; message: string };

type Challenge = { challengeId: string; resendAvailableAtUtc: string };

async function post(path: string, body: unknown) {
  return fetch(`${API_BASE}${path}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...(await clientIpHeaders()) },
    body: JSON.stringify(body),
    cache: "no-store",
  });
}

async function sorry(status: number, detail: string): Promise<string> {
  if (status === 429 || detail.toLowerCase().includes("wait")) {
    return tr("Çox tez-tez cəhd olunub. Bir az gözləyib yenidən yoxlayın.");
  }
  return tr("Əməliyyat alınmadı. Yenidən cəhd edin.");
}

/** Sends the code. A phone without an account is offered registration. */
export async function requestCode(phoneInput: string): Promise<CodeSent> {
  if (!isValidPhone(phoneInput)) {
    return { ok: false, message: await tr("Nömrəni tam yazın: 9 rəqəm (məs. 50 123 45 67).") };
  }
  const phone = normalizePhone(phoneInput);

  try {
    let res = await post("/api/auth/login/otp", { phoneNumber: phone });
    let isRegistration = false;

    if (res.status === 400) {
      const detail = await problem(res.clone());
      if (detail.toLowerCase().includes("not found")) {
        res = await post("/api/auth/registration/otp", { phoneNumber: phone });
        isRegistration = true;
      } else {
        return { ok: false, message: await sorry(res.status, detail) };
      }
    }
    if (!res.ok) return { ok: false, message: await sorry(res.status, await problem(res)) };

    const challenge = (await res.json()) as Challenge;
    return {
      ok: true,
      challengeId: challenge.challengeId,
      isRegistration,
      resendAt: new Date(challenge.resendAvailableAtUtc).getTime(),
    };
  } catch {
    return { ok: false, message: await tr("Serverə qoşulmaq olmadı. İnternetinizi yoxlayın.") };
  }
}

/** Checks the code (and the name of a new account) and signs in. */
export async function verifyCode(input: {
  challengeId: string;
  code: string;
  isRegistration: boolean;
  fullName?: string;
}): Promise<Verified> {
  const code = input.code.replace(/\D/g, "");
  if (code.length !== 6) return { ok: false, message: await tr("6 rəqəmli kodu yazın.") };

  const name = input.fullName?.trim() ?? "";
  if (input.isRegistration && (name.length < 2 || name.length > 100)) {
    return { ok: false, message: await tr("Adınızı yazın (2–100 hərf).") };
  }

  try {
    const res = input.isRegistration
      ? await post("/api/auth/registration/complete", {
          challengeId: input.challengeId,
          code,
          fullName: name,
          preferredLanguage: 1,
        })
      : await post("/api/auth/login/complete", {
          challengeId: input.challengeId,
          code,
        });

    if (!res.ok) {
      if (res.status === 429) return { ok: false, message: await sorry(429, "") };
      // An admin blocked the account: say so, instead of "wrong code".
      if (res.status === 400 && /blocked/i.test(await problem(res.clone()))) {
        return { ok: false, message: await tr("Hesabınız bloklanıb. Ətraflı məlumat üçün dəstəyə yazın.") };
      }
      return { ok: false, message: await tr("Kod səhvdir və ya vaxtı bitib.") };
    }

    const data = (await res.json()) as AuthTokens & {
      userId: string;
      phoneNumber: string;
    };
    await saveSession(data, { userId: data.userId, phone: data.phoneNumber });
    return { ok: true };
  } catch {
    return { ok: false, message: await tr("Serverə qoşulmaq olmadı. İnternetinizi yoxlayın.") };
  }
}

export async function logout() {
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
  redirect("/");
}
