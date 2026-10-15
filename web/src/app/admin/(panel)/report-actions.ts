"use server";

import { revalidatePath } from "next/cache";
import { blockUser, suspendStore, takeDownListing } from "@/app/admin/(panel)/actions";
import { authed } from "@/lib/authed";
import { getSections, isAdmin } from "@/lib/session";

export type Decision = "upheld" | "dismissed" | "reviewing";

export type DecideInput = {
  kind: "content" | "chat";
  id: string;
  decision: Decision;
  note: string;
  /** What follows from an upheld report, when the moderator chose it. */
  takeDownListingId?: string | null;
  suspendStoreId?: string | null;
  blockUserId?: string | null;
  /** Other open reports on the same thing, closed with the same decision. */
  alsoClose: string[];
};

export type DecideResult =
  | { ok: true; steps: string[] }
  | { ok: false; unauthorized: boolean; message: string; steps: string[] };

const ID = /^[0-9a-f-]{36}$/i;

/** The API's status number for each decision. */
const STATUS: Record<Decision, number> = { reviewing: 2, upheld: 3, dismissed: 4 };

/** The text a skipped step shows when the thing is already in the wanted state. */
const ALREADY = "Bu status üçün bu əməliyyat mümkün deyil.";

/**
 * The moderator's decision on a report, carried out in one go: first what follows from it (take the listing
 * down, suspend the store, block the person), then the report itself and any others waiting on the same thing.
 * A step that fails stops the rest, so the report is never closed while its consequence is missing.
 */
export async function decideReport(input: DecideInput): Promise<DecideResult> {
  const steps: string[] = [];
  const stop = (message: string, unauthorized = false): DecideResult => ({ ok: false, unauthorized, message, steps });

  if (!(await isAdmin())) return stop("", true);
  const sections = await getSections();
  const has = (section: string) => sections === null || sections.includes(section);
  if (!has("reports")) return stop("", true);

  const ids = [input.id, ...input.alsoClose];
  if (!ids.every((id) => ID.test(id)) || !["content", "chat"].includes(input.kind) || !(input.decision in STATUS)) {
    return stop("Yanlış sorğu.");
  }
  const note = input.note.trim();
  if (input.decision !== "reviewing" && !note) return stop("Qərar qeydini yazın: şikayəti bağlamaq üçün lazımdır.");
  if (input.decision !== "upheld" && (input.takeDownListingId || input.suspendStoreId || input.blockUserId)) {
    return stop("Tədbirlər yalnız şikayət əsaslı sayılanda tətbiq olunur.");
  }

  // 1. What follows from the decision.
  type Consequence = { section: string; done: string; already: string; failed: string; run: () => ReturnType<typeof takeDownListing> };
  const consequences: Consequence[] = [];
  if (input.takeDownListingId && ID.test(input.takeDownListingId)) {
    const id = input.takeDownListingId;
    consequences.push({
      section: "listings",
      done: "Elan dayandırıldı",
      already: "Elan artıq dayandırılıb (keçildi)",
      failed: "Elan dayandırılmadı",
      run: () => takeDownListing(id, note),
    });
  }
  if (input.suspendStoreId && ID.test(input.suspendStoreId)) {
    const id = input.suspendStoreId;
    consequences.push({
      section: "stores",
      done: "Mağaza dayandırıldı",
      already: "Mağaza artıq dayandırılıb (keçildi)",
      failed: "Mağaza dayandırılmadı",
      run: () => suspendStore(id, note),
    });
  }
  if (input.blockUserId && ID.test(input.blockUserId)) {
    const id = input.blockUserId;
    consequences.push({
      section: "users",
      done: "İstifadəçi bloklandı",
      already: "İstifadəçi artıq bloklanıb (keçildi)",
      failed: "İstifadəçi bloklanmadı",
      run: () => blockUser(id, note),
    });
  }

  for (const c of consequences) {
    if (!has(c.section)) return stop("Bu tədbir üçün icazəniz yoxdur.", true);
    const result = await c.run();
    if (result.ok) {
      steps.push(c.done);
    } else if (result.unauthorized) {
      return stop("", true);
    } else if (result.message === ALREADY) {
      steps.push(c.already);
    } else {
      return stop(`${c.failed}: ${result.message} Şikayət bağlanmadı.`);
    }
  }

  // 2. The report itself, then the others waiting on the same thing.
  const path = input.kind === "chat" ? "/api/admin/conversation-reports" : "/api/admin/content-reports";
  for (const [index, id] of ids.entries()) {
    const res = await authed(`${path}/${id}`, {
      method: "PUT",
      body: {
        status: STATUS[input.decision],
        resolutionNote: note ? (index === 0 ? note : `Eyni mövzuda qərar: ${note}`).slice(0, 1000) : null,
      },
    });
    if (res.ok) {
      steps.push(
        index === 0
          ? input.decision === "reviewing"
            ? "Şikayət baxılır"
            : input.decision === "upheld"
              ? "Şikayət əsaslı sayıldı"
              : "Şikayət əsassız sayıldı"
          : "Eyni mövzudakı digər şikayət bağlandı",
      );
      // The person who reported is told the same moment (a notification in the app, a push to the phone).
      if (index === 0) steps.push("Şikayətçiyə bildiriş göndərildi");
    } else if (res.status === 401 || res.status === 403) {
      return stop("", true);
    } else if (index === 0) {
      return stop("Şikayət bağlanmadı. Yenidən cəhd edin.");
    }
    // Another report that was closed meanwhile is simply left as it is.
  }

  revalidatePath("/admin/sikayetler");
  revalidatePath(input.kind === "chat" ? `/admin/yazisma-sikayetleri/${input.id}` : `/admin/sikayetler/${input.id}`);
  return { ok: true, steps };
}
