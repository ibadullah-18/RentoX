"use client";

import { useT } from "@/lib/i18n/client";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { Check, Loader2, X } from "lucide-react";
import type { Done } from "@/app/admin/(panel)/actions";
import { ConfirmDialog } from "../ConfirmDialog";

/** Approve / reject (with a reason) for a listing or a store. */
export function ModerationActions({
  approve,
  reject,
  backHref,
  noun,
}: {
  approve: () => Promise<Done>;
  reject: (reason: string) => Promise<Done>;
  /** Where to go once it is decided. */
  backHref: string;
  noun: string;
}) {
  const t = useT();
  const router = useRouter();
  const [busy, setBusy] = useState<"approve" | "reject" | null>(null);
  const [asking, setAsking] = useState(false);
  const [reason, setReason] = useState("");
  const [error, setError] = useState<string | null>(null);

  async function run(kind: "approve" | "reject", call: () => Promise<Done>) {
    setBusy(kind);
    setError(null);
    const result = await call();
    setBusy(null);
    if (result.ok) {
      router.push(backHref);
      router.refresh();
    } else if (result.unauthorized) {
      router.push("/giris");
    } else {
      setError(result.message);
    }
  }

  return (
    <div className="space-y-3">
      {error && (
        <p role="alert" className="rounded-xl bg-danger/10 px-4 py-3 text-sm font-semibold text-danger">
          {error}
        </p>
      )}
      <button
        type="button"
        disabled={busy !== null}
        onClick={() => void run("approve", approve)}
        className="flex h-11 w-full items-center justify-center gap-2 rounded-xl bg-success font-bold text-white hover:opacity-90 disabled:opacity-60"
      >
        {busy === "approve" ? <Loader2 className="size-5 animate-spin" aria-hidden /> : <Check className="size-5" aria-hidden />}
        {t("Təsdiq et")}</button>
      <button
        type="button"
        disabled={busy !== null}
        onClick={() => setAsking(true)}
        className="flex h-11 w-full items-center justify-center gap-2 rounded-xl border border-danger font-bold text-danger hover:bg-danger/10 disabled:opacity-60"
      >
        <X className="size-5" aria-hidden />
        {t("Rədd et")}</button>

      <ConfirmDialog
        open={asking}
        title={`${noun} rədd edilsin?`}
        message="Rədd səbəbi sahibinə bildiriş kimi göndəriləcək."
        confirmLabel="Rədd et"
        destructive
        busy={busy === "reject"}
        onClose={() => setAsking(false)}
        onConfirm={() => {
          if (!reason.trim()) {
            setError("Rədd səbəbini yazın.");
            setAsking(false);
            return;
          }
          setAsking(false);
          void run("reject", () => reject(reason));
        }}
      />

      <label className="block">
        <span className="mb-1.5 block text-sm font-bold">{t("Rədd səbəbi")}</span>
        <textarea
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          rows={3}
          maxLength={1000}
          placeholder={t("Rədd edəcəksinizsə, səbəbi yazın")}
          className="w-full resize-none rounded-xl border border-line bg-surface px-4 py-3 text-[16px] outline-none focus:border-primary"
        />
      </label>
    </div>
  );
}

/** Takes a live listing down: a reason is required and the owner is told. */
export function TakeDownPanel({
  take,
  statusText,
  noun = "elan",
}: {
  take: (reason: string) => Promise<Done>;
  statusText: string;
  /** "elan" or "mağaza": only the wording changes. */
  noun?: string;
}) {
  const router = useRouter();
  const [reason, setReason] = useState("");
  const [asking, setAsking] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function run() {
    setBusy(true);
    setError(null);
    const result = await take(reason);
    setBusy(false);
    if (result.ok) {
      setAsking(false);
      setReason("");
      router.refresh();
    } else if (result.unauthorized) router.push("/");
    else {
      setAsking(false);
      setError(result.message);
    }
  }

  return (
    <div className="space-y-3 rounded-xl border border-line bg-surface p-4">
      <p className="text-sm text-ink-2">
        Bu {noun} indi <b>{statusText}</b> vəziyyətindədir. Dayandırılan {noun} saytdan çıxır; sahibi səbəbi bildirişdə görür.
      </p>
      <textarea
        value={reason}
        onChange={(e) => setReason(e.target.value)}
        rows={3}
        maxLength={1000}
        disabled={busy}
        placeholder="Dayandırma səbəbi (məcburi)"
        className="w-full rounded-lg border border-line bg-surface px-3.5 py-2.5 text-[15px] outline-none focus:border-primary"
      />
      {error && (
        <p role="alert" className="text-sm font-semibold text-danger">
          {error}
        </p>
      )}
      <button
        type="button"
        disabled={busy || !reason.trim()}
        onClick={() => setAsking(true)}
        className="flex h-10 w-full items-center justify-center gap-2 rounded-lg border border-danger font-bold text-danger hover:bg-danger/10 disabled:opacity-50"
      >
        {noun === "elan" ? "Elanı dayandır" : "Mağazanı dayandır"}
      </button>
      <ConfirmDialog
        open={asking}
        title={noun === "elan" ? "Elan dayandırılsın?" : "Mağaza dayandırılsın?"}
        message={`${noun === "elan" ? "Elan" : "Mağaza"} dərhal saytdan çıxacaq və sahibinə səbəb göndəriləcək.`}
        confirmLabel="Dayandır"
        destructive
        busy={busy}
        onClose={() => setAsking(false)}
        onConfirm={() => void run()}
      />
    </div>
  );
}

/** Brings a suspended store back. */
export function ReinstateButton({ reinstate, reason }: { reinstate: () => Promise<Done>; reason?: string | null }) {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function run() {
    setBusy(true);
    setError(null);
    const result = await reinstate();
    setBusy(false);
    if (result.ok) router.refresh();
    else setError(result.unauthorized ? "Giriş lazımdır." : result.message);
  }

  return (
    <div className="space-y-3 rounded-xl border border-line bg-surface p-4">
      <p className="text-sm text-ink-2">
        Bu mağaza dayandırılıb{reason ? `: “${reason}”` : ""}. Bərpa etsəniz, yenidən saytda görünəcək.
      </p>
      {error && (
        <p role="alert" className="text-sm font-semibold text-danger">
          {error}
        </p>
      )}
      <button
        type="button"
        disabled={busy}
        onClick={() => void run()}
        className="flex h-10 w-full items-center justify-center gap-2 rounded-lg bg-success font-bold text-white hover:opacity-90 disabled:opacity-60"
      >
        {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
        Mağazanı bərpa et
      </button>
    </div>
  );
}

/** Blocks an account. A reason is required; what happens is listed so nobody blocks by accident. */
export function BlockUserPanel({ block }: { block: (reason: string) => Promise<Done> }) {
  const router = useRouter();
  const [reason, setReason] = useState("");
  const [asking, setAsking] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function run() {
    setBusy(true);
    setError(null);
    const result = await block(reason);
    setBusy(false);
    setAsking(false);
    if (result.ok) {
      setReason("");
      router.refresh();
    } else setError(result.unauthorized ? "İcazə yoxdur." : result.message);
  }

  return (
    <div className="space-y-3 rounded-xl border border-line bg-surface p-4">
      <p className="font-bold">Hesabı blokla</p>
      <ul className="list-disc space-y-0.5 pl-5 text-sm text-ink-2">
        <li>Şəxs daxil ola bilmir, bütün cihazlardan çıxarılır.</li>
        <li>Bütün elanları saytdan çıxarılır (rədd edilmiş olur).</li>
        <li>Mağazası (varsa) dayandırılır.</li>
        <li>Blokdan çıxarmaq hesabı qaytarır, elanları isə yox: sahibi onları düzəldib yenidən göndərməlidir.</li>
      </ul>
      <textarea
        value={reason}
        onChange={(e) => setReason(e.target.value)}
        rows={3}
        maxLength={1000}
        disabled={busy}
        placeholder="Blok səbəbi (məcburi)"
        className="w-full rounded-lg border border-line bg-surface px-3.5 py-2.5 text-[15px] outline-none focus:border-primary"
      />
      {error && (
        <p role="alert" className="text-sm font-semibold text-danger">
          {error}
        </p>
      )}
      <button
        type="button"
        disabled={busy || !reason.trim()}
        onClick={() => setAsking(true)}
        className="flex h-10 w-full items-center justify-center rounded-lg border border-danger font-bold text-danger hover:bg-danger/10 disabled:opacity-50"
      >
        Hesabı blokla
      </button>
      <ConfirmDialog
        open={asking}
        title="Hesab bloklansın?"
        message="Şəxs dərhal sistemdən çıxarılacaq, elanları və mağazası saytdan gedəcək."
        confirmLabel="Blokla"
        destructive
        busy={busy}
        onClose={() => setAsking(false)}
        onConfirm={() => void run()}
      />
    </div>
  );
}

/** Lets a blocked account in again. */
export function UnblockPanel({ unblock, reason }: { unblock: () => Promise<Done>; reason?: string | null }) {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function run() {
    setBusy(true);
    setError(null);
    const result = await unblock();
    setBusy(false);
    if (result.ok) router.refresh();
    else setError(result.unauthorized ? "İcazə yoxdur." : result.message);
  }

  return (
    <div className="space-y-3 rounded-xl border border-danger/40 bg-danger/5 p-4">
      <p className="font-bold text-danger">Hesab bloklanıb</p>
      {reason && <p className="text-sm">Səbəb: {reason}</p>}
      <p className="text-xs text-ink-2">Blokdan çıxarsanız, şəxs yenidən daxil ola bilər. Saytdan çıxarılan elanlar qaytarılmır.</p>
      {error && (
        <p role="alert" className="text-sm font-semibold text-danger">
          {error}
        </p>
      )}
      <button
        type="button"
        disabled={busy}
        onClick={() => void run()}
        className="flex h-10 w-full items-center justify-center gap-2 rounded-lg bg-success font-bold text-white hover:opacity-90 disabled:opacity-60"
      >
        {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
        Blokdan çıxar
      </button>
    </div>
  );
}
