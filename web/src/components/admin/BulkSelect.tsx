"use client";

import { useRouter } from "next/navigation";
import { createContext, useContext, useState } from "react";
import { Check, Loader2, X } from "lucide-react";
import { bulkReviewListings } from "@/app/admin/(panel)/actions";
import { adminHref } from "@/lib/admin-path";
import { ConfirmDialog } from "@/components/ConfirmDialog";

type Bulk = {
  selected: Set<string>;
  toggle: (id: string) => void;
  all: string[];
  setAll: (on: boolean) => void;
};

const BulkContext = createContext<Bulk | null>(null);

/** Keeps which listings of the page are ticked; the rows and the bar read it. */
export function BulkProvider({ ids, children }: { ids: string[]; children: React.ReactNode }) {
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const value: Bulk = {
    selected,
    all: ids,
    toggle: (id) =>
      setSelected((s) => {
        const next = new Set(s);
        if (!next.delete(id)) next.add(id);
        return next;
      }),
    setAll: (on) => setSelected(on ? new Set(ids) : new Set()),
  };
  return <BulkContext.Provider value={value}>{children}</BulkContext.Provider>;
}

function useBulk() {
  const ctx = useContext(BulkContext);
  if (!ctx) throw new Error("BulkProvider is missing");
  return ctx;
}

/** The tick box in front of a row. */
export function BulkCheck({ id, label }: { id: string; label: string }) {
  const { selected, toggle } = useBulk();
  return (
    <label className="flex shrink-0 cursor-pointer items-center self-stretch px-1" title="Seç">
      <input
        type="checkbox"
        checked={selected.has(id)}
        onChange={() => toggle(id)}
        aria-label={`Seç: ${label}`}
        className="size-5 accent-[var(--color-primary)]"
      />
    </label>
  );
}

/** "Select all" and, once something is ticked, approve / reject for all of them. */
export function BulkBar() {
  const router = useRouter();
  const { selected, all, setAll } = useBulk();
  const [asking, setAsking] = useState(false);
  const [rejecting, setRejecting] = useState(false);
  const [reason, setReason] = useState("");
  const [busy, setBusy] = useState<"approve" | "reject" | null>(null);
  const [message, setMessage] = useState<{ tone: "ok" | "bad"; text: string } | null>(null);

  const count = selected.size;
  const everyone = count > 0 && count === all.length;

  async function run(decision: "approve" | "reject") {
    setBusy(decision);
    setMessage(null);
    const res = await bulkReviewListings([...selected], decision, reason);
    setBusy(null);
    setAsking(false);
    if (!res.ok) {
      if (res.unauthorized) router.push(adminHref("/giris"));
      else setMessage({ tone: "bad", text: res.message });
      return;
    }
    const failed = res.failed.length;
    setMessage(
      failed === 0
        ? { tone: "ok", text: `${res.done} elan ${decision === "approve" ? "təsdiqləndi" : "rədd edildi"}.` }
        : {
            tone: "bad",
            text: `${res.done} elan işləndi, ${failed} elan alınmadı (${res.failed[0].message})${failed > 1 ? " və s." : ""}`,
          },
    );
    setAll(false);
    setRejecting(false);
    setReason("");
    router.refresh();
  }

  return (
    <div className="no-print sticky bottom-3 z-10 space-y-2 rounded-xl border border-line bg-surface p-3 shadow-lg">
      <div className="flex flex-wrap items-center gap-3">
        <label className="flex cursor-pointer items-center gap-2 text-sm font-semibold">
          <input
            type="checkbox"
            checked={everyone}
            onChange={(e) => setAll(e.target.checked)}
            className="size-5 accent-[var(--color-primary)]"
          />
          Bu səhifədəkilərin hamısı
        </label>
        <span className="text-sm text-ink-2">{count > 0 ? `${count} seçilib` : "Seçilməyib"}</span>
        <span className="ml-auto flex gap-2">
          <button
            type="button"
            disabled={count === 0 || busy !== null}
            onClick={() => setAsking(true)}
            className="flex h-10 items-center gap-1.5 rounded-lg bg-success px-4 text-sm font-bold text-white hover:opacity-90 disabled:opacity-50"
          >
            {busy === "approve" ? <Loader2 className="size-4 animate-spin" aria-hidden /> : <Check className="size-4" aria-hidden />}
            Təsdiqlə
          </button>
          <button
            type="button"
            disabled={count === 0 || busy !== null}
            onClick={() => setRejecting((v) => !v)}
            className="flex h-10 items-center gap-1.5 rounded-lg border border-danger px-4 text-sm font-bold text-danger hover:bg-danger/10 disabled:opacity-50"
          >
            <X className="size-4" aria-hidden />
            Rədd et
          </button>
        </span>
      </div>

      {rejecting && count > 0 && (
        <div className="space-y-2">
          <textarea
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            rows={2}
            maxLength={1000}
            placeholder="Rədd səbəbi (hər elanın sahibinə bildiriş kimi gedəcək)"
            className="w-full resize-none rounded-lg border border-line bg-surface px-3 py-2 text-[15px] outline-none focus:border-primary"
          />
          <button
            type="button"
            disabled={!reason.trim() || busy !== null}
            onClick={() => void run("reject")}
            className="flex h-10 items-center gap-1.5 rounded-lg bg-danger px-4 text-sm font-bold text-white hover:opacity-90 disabled:opacity-50"
          >
            {busy === "reject" && <Loader2 className="size-4 animate-spin" aria-hidden />}
            {count} elan rədd edilsin
          </button>
        </div>
      )}

      {message && (
        <p role="status" className={`text-sm font-semibold ${message.tone === "ok" ? "text-success" : "text-danger"}`}>
          {message.text}
        </p>
      )}

      <ConfirmDialog
        open={asking}
        title={`${count} elan təsdiqlənsin?`}
        message="Elanlar dərhal yayımlanacaq və sahiblərinə bildiriş gedəcək."
        confirmLabel="Təsdiqlə"
        busy={busy === "approve"}
        onClose={() => setAsking(false)}
        onConfirm={() => void run("approve")}
      />
    </div>
  );
}
