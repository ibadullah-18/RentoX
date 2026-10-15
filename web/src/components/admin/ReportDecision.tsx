"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { CheckCircle2, Eye, Loader2, ShieldAlert, ShieldCheck } from "lucide-react";
import { decideReport, type Decision } from "@/app/admin/(panel)/report-actions";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { adminHref } from "@/lib/admin-path";

type Props = {
  kind: "content" | "chat";
  id: string;
  status: number;
  /** What it is about, in a word: "elan", "mağaza", "yazışma". */
  noun: string;
  /** What can follow from an upheld report, only what this admin may do and what is still possible. */
  listing?: { id: string } | null;
  store?: { id: string } | null;
  person?: { id: string; name: string } | null;
  /** Other open reports on the same thing. */
  others: { id: string }[];
  listUrl: string;
};

const presets: Record<"upheld" | "dismissed", string[]> = {
  upheld: ["Qayda pozuntusu təsdiqləndi.", "Aldadıcı məlumat aşkar edildi.", "Fırıldaq əlamətləri var.", "Təkrar şikayətlər təsdiqləndi."],
  dismissed: ["Pozuntu aşkar edilmədi.", "Şikayətdə əsas yoxdur.", "Təqdim olunan məlumat kifayət deyil."],
};

/**
 * Deciding a report: first the verdict, then what follows from it, with the exact consequences shown
 * before anything is done. Closed reports show their outcome instead.
 */
export function ReportDecision({ kind, id, status, noun, listing, store, person, others, listUrl }: Props) {
  const router = useRouter();
  const [verdict, setVerdict] = useState<"upheld" | "dismissed" | null>(null);
  const [note, setNote] = useState("");
  const [takeDown, setTakeDown] = useState(true);
  const [suspend, setSuspend] = useState(true);
  const [block, setBlock] = useState(false);
  const [closeOthers, setCloseOthers] = useState(false);
  const [asking, setAsking] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState<string[] | null>(null);

  const open = status === 1 || status === 2;

  if (done) {
    return (
      <div className="space-y-3 p-4">
        <p className="flex items-center gap-2 font-bold text-success">
          <CheckCircle2 className="size-5" aria-hidden /> Qərar tətbiq olundu
        </p>
        <ul className="list-disc space-y-1 pl-5 text-sm">
          {done.map((s) => (
            <li key={s}>{s}</li>
          ))}
        </ul>
        <Link href={listUrl} className="inline-block text-sm font-bold text-primary hover:underline">
          Növbəti şikayətlərə keç
        </Link>
      </div>
    );
  }

  if (!open) {
    return (
      <p className="m-4 rounded-lg bg-line px-3 py-2.5 text-sm font-semibold text-ink-2">
        Şikayət bağlanıb. Bağlanmış şikayəti dəyişmək olmaz.
      </p>
    );
  }

  const upheld = verdict === "upheld";
  const consequences: string[] = [];
  if (upheld && listing && takeDown) consequences.push("Elan dayandırılacaq (sahibinə səbəb bildirilir)");
  if (upheld && store && suspend) consequences.push("Mağaza dayandırılacaq (sahibinə səbəb bildirilir)");
  if (upheld && person && block) consequences.push(`${person.name} bloklanacaq: hesaba giriş bağlanır, elanları götürülür`);
  if (verdict && closeOthers && others.length > 0) consequences.push(`Eyni mövzudakı digər ${others.length} açıq şikayət də bağlanacaq`);

  async function send(decision: Decision) {
    setBusy(true);
    setError(null);
    const res = await decideReport({
      kind,
      id,
      decision,
      note,
      takeDownListingId: decision === "upheld" && listing && takeDown ? listing.id : null,
      suspendStoreId: decision === "upheld" && store && suspend ? store.id : null,
      blockUserId: decision === "upheld" && person && block ? person.id : null,
      alsoClose: decision !== "reviewing" && closeOthers ? others.map((o) => o.id) : [],
    });
    setBusy(false);
    setAsking(false);
    if (res.ok) {
      if (decision === "reviewing") router.refresh();
      else setDone(res.steps);
      router.refresh();
    } else if (res.unauthorized) router.push(adminHref("/giris"));
    else setError(res.message);
  }

  return (
    <div className="space-y-4 p-4">
      {status === 1 && (
        <button
          type="button"
          disabled={busy}
          onClick={() => void send("reviewing")}
          className="flex h-10 w-full items-center justify-center gap-2 rounded-lg border border-line text-sm font-bold hover:border-primary hover:text-primary disabled:opacity-60"
        >
          <Eye className="size-4" aria-hidden />
          Baxmağa başla (komandaya «baxılır» görünsün)
        </button>
      )}

      <fieldset className="space-y-2">
        <legend className="mb-1 text-sm font-extrabold">1. Şikayət əsaslıdırmı?</legend>
        <button
          type="button"
          aria-pressed={verdict === "upheld"}
          onClick={() => {
            setVerdict("upheld");
            // Other reports on the same thing are usually the same problem: close them together.
            setCloseOthers(true);
          }}
          className={`flex w-full items-start gap-3 rounded-xl border-2 p-3 text-left ${
            verdict === "upheld" ? "border-danger bg-danger/10" : "border-line hover:border-danger"
          }`}
        >
          <ShieldAlert className="mt-0.5 size-5 shrink-0 text-danger" aria-hidden />
          <span>
            <span className="block font-bold">Əsaslıdır</span>
            <span className="block text-xs text-ink-2">Qayda pozulub; {noun} üzrə tədbir görülə bilər.</span>
          </span>
        </button>
        <button
          type="button"
          aria-pressed={verdict === "dismissed"}
          onClick={() => {
            setVerdict("dismissed");
            // "No violation" on one report says nothing about the others: they stay for their own look.
            setCloseOthers(false);
          }}
          className={`flex w-full items-start gap-3 rounded-xl border-2 p-3 text-left ${
            verdict === "dismissed" ? "border-success bg-success-soft" : "border-line hover:border-success"
          }`}
        >
          <ShieldCheck className="mt-0.5 size-5 shrink-0 text-success" aria-hidden />
          <span>
            <span className="block font-bold">Əsassızdır</span>
            <span className="block text-xs text-ink-2">Pozuntu yoxdur; heç nə dəyişmir, şikayət bağlanır.</span>
          </span>
        </button>
      </fieldset>

      {verdict && (
        <div className="space-y-2">
          <label htmlFor="report-note" className="block text-sm font-extrabold">
            2. Qərar qeydi (məcburi)
          </label>
          <div className="flex flex-wrap gap-1.5">
            {presets[verdict].map((p) => (
              <button
                key={p}
                type="button"
                onClick={() => setNote((n) => (n.trim() ? `${n.trim()} ${p}` : p))}
                className="rounded-full border border-line px-2.5 py-1 text-xs font-semibold text-ink-2 hover:border-primary hover:text-primary"
              >
                + {p}
              </button>
            ))}
          </div>
          <textarea
            id="report-note"
            value={note}
            onChange={(e) => setNote(e.target.value)}
            rows={3}
            maxLength={1000}
            disabled={busy}
            placeholder="Nə üçün belə qərar verdiniz? Bu qeyd tarixçədə qalır."
            className="w-full resize-none rounded-lg border border-line bg-surface px-3 py-2.5 text-[15px] outline-none focus:border-primary"
          />
        </div>
      )}

      {upheld && (listing || store || person) && (
        <div className="space-y-2">
          <p className="text-sm font-extrabold">3. Nə edilsin?</p>
          {listing && (
            <Check checked={takeDown} onChange={setTakeDown} label="Elanı dayandır" hint="Sahibi səbəbi görür və elanı düzəldib yenidən göndərə bilər." />
          )}
          {store && (
            <Check checked={suspend} onChange={setSuspend} label="Mağazanı dayandır" hint="Mağaza saytdan çıxır; sahibi səbəbi görür." />
          )}
          {person && (
            <Check
              checked={block}
              onChange={setBlock}
              label={`${person.name} hesabını blokla`}
              hint="Ağır pozuntu üçündür: giriş bağlanır, elanlar götürülür, mağaza dayandırılır."
              danger
            />
          )}
        </div>
      )}

      {verdict && others.length > 0 && (
        <Check
          checked={closeOthers}
          onChange={setCloseOthers}
          label={`Eyni mövzudakı digər ${others.length} açıq şikayəti də bağla`}
          hint="Eyni qərar və qeyd onlara da yazılır."
        />
      )}

      {error && (
        <p role="alert" className="rounded-lg bg-danger/10 px-3 py-2 text-sm font-semibold text-danger">
          {error}
        </p>
      )}

      <button
        type="button"
        disabled={busy || !verdict || !note.trim()}
        onClick={() => setAsking(true)}
        className="flex h-11 w-full items-center justify-center gap-2 rounded-xl bg-primary font-bold text-white hover:bg-primary-press disabled:opacity-50"
      >
        {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
        Qərarı tətbiq et
      </button>

      <ConfirmDialog
        open={asking}
        title={upheld ? "Şikayət əsaslı sayılsın?" : "Şikayət əsassız sayılsın?"}
        message={
          consequences.length > 0
            ? `Bunlar ediləcək: ${consequences.join("; ")}. Şikayət bağlanacaq və geri alına bilməz.`
            : "Şikayət bağlanacaq və geri alına bilməz. Heç bir tədbir görülmür."
        }
        confirmLabel="Tətbiq et"
        destructive={upheld && consequences.length > 1}
        busy={busy}
        onClose={() => setAsking(false)}
        onConfirm={() => verdict && void send(verdict)}
      />
    </div>
  );
}

function Check({
  checked,
  onChange,
  label,
  hint,
  danger = false,
}: {
  checked: boolean;
  onChange: (value: boolean) => void;
  label: string;
  hint: string;
  danger?: boolean;
}) {
  return (
    <label className={`flex cursor-pointer items-start gap-3 rounded-xl border p-3 ${checked ? (danger ? "border-danger bg-danger/5" : "border-primary bg-primary-soft") : "border-line"}`}>
      <input type="checkbox" checked={checked} onChange={(e) => onChange(e.target.checked)} className="mt-0.5 size-5 shrink-0 accent-[var(--color-primary)]" />
      <span>
        <span className="block text-sm font-bold">{label}</span>
        <span className="block text-xs text-ink-2">{hint}</span>
      </span>
    </label>
  );
}
