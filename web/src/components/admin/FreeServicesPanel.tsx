"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { Gift, Loader2 } from "lucide-react";
import { setFreeServices } from "@/app/admin/(panel)/gift-actions";
import { adminHref } from "@/lib/admin-path";

/** The services the owner can make free for one person; the numbers are the bits the API keeps. */
const CHOICES = [
  { bit: 1, label: "Elan haqqı", hint: "Pulsuz limitdən sonrakı hər elan üçün aktivləşdirmə haqqı çıxılmır" },
  { bit: 2, label: "Elan VIP", hint: "Elanı VIP etmək pulsuzdur" },
  { bit: 4, label: "İrəli çəkmə", hint: "Elanı yuxarı qaldırmaq pulsuzdur" },
  { bit: 8, label: "VIP mağaza", hint: "Mağazanı ana səhifədə göstərmək pulsuzdur" },
];

/**
 * The owner picks, one by one, which paid services RentoX pays for one person. Nothing is taken from the wallet and
 * nothing is counted as income. The person sees "given free by RentoX" where they would have paid, and is told when
 * the choice changes.
 */
export function FreeServicesPanel({ userId, services }: { userId: string; services: number }) {
  const router = useRouter();
  const [chosen, setChosen] = useState(services);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<{ ok: boolean; text: string } | null>(null);
  const changed = chosen !== services;

  const toggle = (bit: number) => setChosen((now) => now ^ bit);

  async function save() {
    setBusy(true);
    setMessage(null);
    const res = await setFreeServices(userId, chosen);
    setBusy(false);
    if (res.ok) {
      setMessage({ ok: true, text: res.message });
      router.refresh();
    } else if (res.unauthorized) router.push(adminHref("/giris"));
    else setMessage({ ok: false, text: res.message });
  }

  return (
    <section className="space-y-3 rounded-xl border border-warning/40 bg-warning-soft/40 p-4">
      <div className="flex items-center gap-2">
        <Gift className="size-5 text-warning" aria-hidden />
        <h2 className="font-extrabold">RentoX-dan pulsuz imkanlar</h2>
        {services !== 0 && (
          <span className="rounded-full bg-success-soft px-2 py-0.5 text-xs font-bold text-success">Açıqdır</span>
        )}
      </div>
      <p className="text-xs text-ink-2">
        Seçdiyin xidmətlər bu istifadəçi üçün pulsuz olur. Cüzdandan pul çıxılmır və maliyyədə gəlir yazılmır.
        İstifadəçi “RentoX tərəfindən pulsuz verilib” yazısını görür. Bunu yalnız Super Admin dəyişə bilər.
      </p>

      <div className="space-y-2">
        {CHOICES.map((c) => (
          <label
            key={c.bit}
            className="flex cursor-pointer items-start gap-3 rounded-lg border border-line bg-surface p-3 has-[:checked]:border-primary"
          >
            <input
              type="checkbox"
              checked={(chosen & c.bit) !== 0}
              onChange={() => toggle(c.bit)}
              disabled={busy}
              className="mt-0.5 size-4 accent-primary"
            />
            <span className="min-w-0">
              <span className="block text-sm font-bold">{c.label}</span>
              <span className="block text-xs text-ink-2">{c.hint}</span>
            </span>
          </label>
        ))}
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <button
          type="button"
          disabled={busy || !changed}
          onClick={() => void save()}
          className="flex h-10 items-center justify-center gap-2 rounded-lg bg-primary px-4 text-sm font-bold text-white hover:bg-primary-press disabled:opacity-50"
        >
          {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
          Saxla
        </button>
        <button
          type="button"
          disabled={busy || chosen === 15}
          onClick={() => setChosen(15)}
          className="h-10 rounded-lg border border-line bg-surface px-3 text-sm font-bold hover:border-primary disabled:opacity-50"
        >
          Hamısını seç
        </button>
        <button
          type="button"
          disabled={busy || chosen === 0}
          onClick={() => setChosen(0)}
          className="h-10 rounded-lg border border-line bg-surface px-3 text-sm font-bold hover:border-primary disabled:opacity-50"
        >
          Hamısını götür
        </button>
      </div>

      {message && (
        <p role="status" className={`text-sm font-semibold ${message.ok ? "text-success" : "text-danger"}`}>
          {message.text}
        </p>
      )}
    </section>
  );
}
