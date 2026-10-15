"use server";

import { authed } from "@/lib/authed";
import { getSections } from "@/lib/session";

export type AnnounceResult =
  | { ok: true; recipients: number }
  | { ok: false; unauthorized: boolean; message: string };

const known: [RegExp, string][] = [
  [/nobody to send/i, "Mesaj göndəriləcək aktiv istifadəçi yoxdur."],
  [/not found or is not active/i, "İstifadəçi tapılmadı və ya aktiv deyil."],
  [/title cannot exceed/i, "Başlıq 160 simvoldan çox ola bilməz."],
  [/body cannot exceed/i, "Mətn 1000 simvoldan çox ola bilməz."],
];

/** Sends a message to one person (`userId`) or to everyone. A users-section Admin only; the API checks too. */
export async function sendAnnouncement(
  userId: string | null,
  audience: number,
  title: string,
  body: string,
  link: string,
): Promise<AnnounceResult> {
  const sections = await getSections();
  if (sections !== null && !sections.includes("users")) return { ok: false, unauthorized: true, message: "" };

  const t = title.trim();
  const b = body.trim();
  if (![1, 2, 3].includes(audience)) return { ok: false, unauthorized: false, message: "Kimə göndəriləcəyini seçin." };
  if (!t) return { ok: false, unauthorized: false, message: "Başlıq yazın." };
  if (t.length > 160) return { ok: false, unauthorized: false, message: "Başlıq 160 simvoldan çox ola bilməz." };
  if (!b) return { ok: false, unauthorized: false, message: "Mətn yazın." };
  if (b.length > 1000) return { ok: false, unauthorized: false, message: "Mətn 1000 simvoldan çox ola bilməz." };

  // Only a place on the site itself: "/destek", not an outside address.
  const l = link.trim();
  if (l && (!l.startsWith("/") || l.startsWith("//") || l.length > 500)) {
    return { ok: false, unauthorized: false, message: "Keçid saytın daxilində olmalıdır, məsələn: /destek" };
  }

  const res = await authed<{ recipients: number }>("/api/admin/announcements", {
    method: "POST",
    body: { userId, audience, title: t, body: b, actionUrl: l || null },
  });
  if (!res.ok) {
    if (res.status === 401 || res.status === 403) return { ok: false, unauthorized: true, message: "" };
    const text = known.find(([re]) => re.test(res.message))?.[1];
    return { ok: false, unauthorized: false, message: text ?? "Əməliyyat alınmadı. Yenidən cəhd edin." };
  }
  return { ok: true, recipients: res.data.recipients };
}
