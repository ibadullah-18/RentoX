"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { Loader2 } from "lucide-react";
import { saveSiteInfo, type SiteInfoInput } from "@/app/admin/(panel)/content-actions";
import { adminHref } from "@/lib/admin-path";

export type SiteInfoRow = {
  operatorName?: string | null;
  voen?: string | null;
  address?: string | null;
  email?: string | null;
  phone?: string | null;
  hotline?: string | null;
  whatsApp?: string | null;
  instagram?: string | null;
  tikTok?: string | null;
  facebook?: string | null;
};

const field =
  "h-10 w-full rounded-lg border border-line bg-surface px-3 text-[15px] outline-none focus:border-primary disabled:opacity-60";

type Key = keyof SiteInfoInput;

const groups: { title: string; hint?: string; fields: { key: Key; label: string; placeholder: string; hint?: string }[] }[] = [
  {
    title: "Operator (şərtlərdə və məxfilik siyasətində göstərilir)",
    fields: [
      { key: "operatorName", label: "Ad, soyad (fərdi sahibkar)", placeholder: "Əliyev Əli Əli oğlu" },
      { key: "voen", label: "VÖEN", placeholder: "1234567890" },
      { key: "address", label: "Ünvan", placeholder: "Bakı şəhəri, ..." },
    ],
  },
  {
    title: "Əlaqə vasitələri",
    hint: "Boş buraxdığınız vasitə saytda və tətbiqdə heç yerdə göstərilmir.",
    fields: [
      { key: "email", label: "E-poçt", placeholder: "info@rentox.az" },
      { key: "phone", label: "Əlaqə nömrəsi", placeholder: "+994 12 345 67 89" },
      { key: "hotline", label: "Qaynar xətt", placeholder: "1234" },
      { key: "whatsApp", label: "WhatsApp nömrəsi", placeholder: "+994 50 123 45 67", hint: "Ölkə kodu ilə. Sayt «wa.me» linki düzəldəcək." },
    ],
  },
  {
    title: "Sosial şəbəkələr",
    fields: [
      { key: "instagram", label: "Instagram", placeholder: "rentox.az və ya https://instagram.com/rentox.az", hint: "Hesabın adı və ya tam link." },
      { key: "tikTok", label: "TikTok", placeholder: "rentox.az və ya https://www.tiktok.com/@rentox.az" },
      { key: "facebook", label: "Facebook", placeholder: "rentox.az və ya https://facebook.com/rentox.az" },
    ],
  },
];

/** The operator's details and the contact channels; what is empty is not shown. */
export function SiteInfoForm({ current }: { current: SiteInfoRow }) {
  const router = useRouter();
  const initial: SiteInfoInput = {
    operatorName: current.operatorName ?? "",
    voen: current.voen ?? "",
    address: current.address ?? "",
    email: current.email ?? "",
    phone: current.phone ?? "",
    hotline: current.hotline ?? "",
    whatsApp: current.whatsApp ?? "",
    instagram: current.instagram ?? "",
    tikTok: current.tikTok ?? "",
    facebook: current.facebook ?? "",
  };
  const [v, setV] = useState(initial);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);
  const changed = (Object.keys(initial) as Key[]).some((k) => v[k] !== initial[k]);

  async function save() {
    setBusy(true);
    setError(null);
    setSaved(false);
    const res = await saveSiteInfo(v);
    setBusy(false);
    if (res.ok) {
      setSaved(true);
      router.refresh();
    } else if (res.unauthorized) router.push(adminHref("/giris"));
    else setError(res.message);
  }

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        void save();
      }}
      className="space-y-5"
    >
      {groups.map((g) => (
        <section key={g.title} className="space-y-3 rounded-xl border border-line bg-surface p-4 md:p-5">
          <div>
            <h2 className="text-[15px] font-extrabold">{g.title}</h2>
            {g.hint && <p className="mt-0.5 text-xs text-ink-2">{g.hint}</p>}
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            {g.fields.map((f) => (
              <label key={f.key} className="block">
                <span className="mb-1 block text-sm font-bold">{f.label}</span>
                <input
                  value={v[f.key]}
                  onChange={(e) => {
                    setV({ ...v, [f.key]: e.target.value });
                    setSaved(false);
                  }}
                  placeholder={f.placeholder}
                  disabled={busy}
                  className={field}
                />
                {f.hint && <span className="mt-1 block text-xs text-ink-2">{f.hint}</span>}
              </label>
            ))}
          </div>
        </section>
      ))}

      {!v.operatorName.trim() && (
        <p className="rounded-lg bg-warning-soft px-3 py-2 text-xs">
          Operatorun adı yazılmayıb: İstifadə şərtləri və Məxfilik siyasətində operator yerində «—» göstəriləcək. Qeydiyyatdan sonra mütləq doldurun.
        </p>
      )}

      <div className="flex flex-wrap items-center gap-3">
        <button
          type="submit"
          disabled={busy || !changed}
          className="flex h-10 items-center gap-2 rounded-lg bg-primary px-5 text-sm font-bold text-white hover:bg-primary-press disabled:opacity-50"
        >
          {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
          Yadda saxla
        </button>
        {error && (
          <p role="alert" className="text-sm font-semibold text-danger">
            {error}
          </p>
        )}
        {saved && <p role="status" className="text-sm font-semibold text-success">Saxlanıldı, saytda və tətbiqdə görünür.</p>}
      </div>
    </form>
  );
}
