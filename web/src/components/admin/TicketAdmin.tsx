"use client";

import { Select } from "@/components/ui/Select";
import { useT } from "@/lib/i18n/client";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { Loader2, Send } from "lucide-react";
import { replyAsAdmin, reviewChatReport, reviewReport, setTicketPriority, setTicketStatus, type Done } from "@/app/admin/(panel)/actions";
import { priorityLabel, reportStatus } from "@/lib/admin-labels";
import { ticketStatus } from "@/lib/support";


function useRun() {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function run(call: () => Promise<Done>, after?: () => void) {
    setBusy(true);
    setError(null);
    const result = await call();
    setBusy(false);
    if (result.ok) {
      after?.();
      router.refresh();
    } else if (result.unauthorized) router.push("/giris");
    else setError(result.message);
  }
  return { busy, error, run };
}

/** Reply to a support request, and change its status and priority. */
export function TicketAdmin({ id, status, priority }: { id: string; status: number; priority: number }) {
  const t = useT();
  const { busy, error, run } = useRun();
  const [text, setText] = useState("");

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap gap-3">
        <div className="flex items-center gap-2 text-sm font-bold">
          {t("Status")}
          <div className="w-44">
            <Select
              value={String(status)}
              disabled={busy}
              onChange={(v) => void run(() => setTicketStatus(id, Number(v)))}
              ariaLabel={t("Status")}
              options={Object.entries(ticketStatus).map(([v, label]) => ({ value: v, label: t(label) }))}
            />
          </div>
        </div>
        <div className="flex items-center gap-2 text-sm font-bold">
          {t("Prioritet")}
          <div className="w-44">
            <Select
              value={String(priority)}
              disabled={busy}
              onChange={(v) => void run(() => setTicketPriority(id, Number(v)))}
              ariaLabel={t("Prioritet")}
              options={Object.entries(priorityLabel).map(([v, label]) => ({ value: v, label: t(label) }))}
            />
          </div>
        </div>
      </div>

      <form
        onSubmit={(e) => {
          e.preventDefault();
          if (text.trim()) void run(() => replyAsAdmin(id, text), () => setText(""));
        }}
        className="space-y-3"
      >
        <textarea
          value={text}
          onChange={(e) => setText(e.target.value)}
          rows={4}
          maxLength={4000}
          disabled={busy}
          placeholder={t("İstifadəçiyə cavab yazın")}
          className="w-full rounded-xl border border-line bg-surface px-4 py-3 text-[16px] outline-none focus:border-primary"
        />
        <button
          type="submit"
          disabled={busy || !text.trim()}
          className="flex h-11 items-center gap-2 rounded-xl bg-primary px-6 font-bold text-white hover:bg-primary-press disabled:opacity-60"
        >
          {busy ? <Loader2 className="size-5 animate-spin" aria-hidden /> : <Send className="size-5" aria-hidden />}
          {t("Cavab göndər")}</button>
      </form>

      {error && (
        <p role="alert" className="text-sm font-semibold text-danger">
          {error}
        </p>
      )}
    </div>
  );
}

/** Mark a report as under review, resolved or rejected. Closing it needs a note; a closed report is final. */
export function ReportReview({ id, status, kind = "content" }: { id: string; status: number; kind?: "content" | "chat" }) {
  const { busy, error, run } = useRun();
  const [note, setNote] = useState("");
  const [problem, setProblem] = useState<string | null>(null);

  const closed = status === 3 || status === 4;
  const options: [number, string][] = [
    [2, reportStatus[2]],
    [3, reportStatus[3]],
    [4, reportStatus[4]],
  ];

  if (closed) {
    return (
      <p className="rounded-lg bg-line px-3 py-2.5 text-sm font-semibold text-ink-2">
        Şikayət bağlanıb ({reportStatus[status]}). Bağlanmış şikayəti dəyişmək olmaz.
      </p>
    );
  }

  function choose(value: number) {
    if ((value === 3 || value === 4) && !note.trim()) {
      setProblem("Şikayəti bağlamaq üçün qərar qeydi yazın.");
      return;
    }
    setProblem(null);
    void run(() => (kind === "chat" ? reviewChatReport(id, value, note) : reviewReport(id, value, note)));
  }

  return (
    <div className="space-y-3">
      <textarea
        value={note}
        onChange={(e) => {
          setNote(e.target.value);
          setProblem(null);
        }}
        rows={3}
        maxLength={1000}
        disabled={busy}
        placeholder="Qərar qeydi (həll və ya rədd üçün məcburidir)"
        className="w-full rounded-lg border border-line bg-surface px-3.5 py-2.5 text-[15px] outline-none focus:border-primary"
      />
      <div className="flex flex-wrap gap-2">
        {options.map(([value, label]) => (
          <button
            key={value}
            type="button"
            disabled={busy || status === value}
            onClick={() => choose(value)}
            className={`h-10 rounded-lg border px-4 text-sm font-bold disabled:opacity-50 ${
              value === 3
                ? "border-success text-success hover:bg-success-soft"
                : value === 4
                  ? "border-danger text-danger hover:bg-danger/10"
                  : "border-line hover:border-primary"
            }`}
          >
            {label}
          </button>
        ))}
      </div>
      {(problem || error) && (
        <p role="alert" className="text-sm font-semibold text-danger">
          {problem ?? error}
        </p>
      )}
    </div>
  );
}
