"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { Loader2 } from "lucide-react";
import { updateStoreContact, type ContactInput } from "@/app/magaza/actions";
import { useT } from "@/lib/i18n/client";
import { FacebookIcon, InstagramIcon, TikTokIcon, WhatsAppIcon, YouTubeIcon } from "../SocialIcons";
import { checkUrl, defaultWhatsAppMessage, hhmm, toMinutes, weekdayNames, type OpenDay } from "@/lib/store-contact";

const input =
  "h-11 w-full rounded-xl border border-line bg-surface px-4 text-[16px] outline-none focus:border-primary disabled:opacity-60";

type DayRow = { open: boolean; from: string; to: string };

const fromHours = (hours: OpenDay[]): DayRow[] =>
  weekdayNames.map((_, i) => {
    const d = hours.find((h) => h.day === i + 1);
    return d ? { open: true, from: hhmm(d.openMinute), to: hhmm(d.closeMinute) } : { open: false, from: "09:00", to: "18:00" };
  });

/**
 * The contact side of the store: phone, e-mail, address, the networks, WhatsApp and the working hours. It can be
 * changed at any time (a live store stays live).
 */
export function StoreContactForm({ initial }: { initial: ContactInput & { hours: OpenDay[] } }) {
  const t = useT();
  const router = useRouter();
  const [v, setV] = useState({ ...initial });
  const [days, setDays] = useState<DayRow[]>(fromHours(initial.hours));
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<{ ok: boolean; text: string } | null>(null);

  const set = (key: keyof ContactInput) => (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
    setV((s) => ({ ...s, [key]: e.target.value }));

  const setDay = (i: number, patch: Partial<DayRow>) => setDays((all) => all.map((d, k) => (k === i ? { ...d, ...patch } : d)));

  function applyToAll() {
    const first = days.find((d) => d.open) ?? days[0];
    setDays((all) => all.map((d) => ({ ...d, open: d.open, from: first.from, to: first.to })));
  }

  async function save() {
    if (busy) return;
    const hours: OpenDay[] = [];
    for (let i = 0; i < days.length; i++) {
      if (!days[i].open) continue;
      const a = toMinutes(days[i].from);
      const b = toMinutes(days[i].to);
      if (a === null || b === null || b <= a) {
        setMessage({ ok: false, text: t("{day} üçün saatları düzgün yazın: bağlanış açılışdan sonra olmalıdır.", { day: t(weekdayNames[i]) }) });
        return;
      }
      hours.push({ day: i + 1, openMinute: a, closeMinute: b });
    }
    for (const url of [v.instagram, v.tiktok, v.facebook, v.youtube, v.website]) {
      const problem = checkUrl(url, t);
      if (problem) {
        setMessage({ ok: false, text: problem });
        return;
      }
    }

    setBusy(true);
    setMessage(null);
    const res = await updateStoreContact({ ...v }, hours);
    setBusy(false);
    if (res.ok) {
      setMessage({ ok: true, text: t("Yadda saxlanıldı.") });
      router.refresh();
    } else if (res.unauthorized) router.push("/giris?next=/magaza");
    else setMessage({ ok: false, text: res.message });
  }

  const field = (label: string, key: keyof ContactInput, props: React.InputHTMLAttributes<HTMLInputElement> = {}, icon?: React.ReactNode) => (
    <label className="block">
      <span className="mb-1.5 flex items-center gap-2 text-sm font-bold">
        {icon}
        {label}
      </span>
      <input value={v[key]} onChange={set(key)} disabled={busy} className={input} {...props} />
    </label>
  );

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        void save();
      }}
      className="space-y-5 rounded-2xl border border-line bg-surface p-5"
    >
      <div>
        <h2 className="text-lg font-extrabold">{t("Əlaqə, sosial şəbəkələr və iş saatları")}</h2>
        <p className="text-sm text-ink-2">{t("Bunları istənilən vaxt dəyişə bilərsiniz, mağaza yoxlamaya qayıtmır. Doldurmadığınız şəbəkə mağaza səhifəsində göstərilmir.")}</p>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        {field(t("Telefon"), "phone", { inputMode: "tel", maxLength: 30 })}
        {field(t("E-poçt"), "email", { type: "email", maxLength: 254 })}
      </div>
      {field(t("Ünvan"), "address", { maxLength: 500 })}

      <div className="space-y-3 rounded-xl border border-line bg-bg p-4">
        <p className="flex items-center gap-2 font-bold text-[#25D366]">
          <WhatsAppIcon className="size-5" />
          WhatsApp
        </p>
        {field(t("WhatsApp nömrəsi"), "whatsApp", { inputMode: "tel", placeholder: "050 123 45 67", maxLength: 20 })}
        <label className="block">
          <span className="mb-1.5 block text-sm font-bold">{t("İlk mesaj")}</span>
          <textarea
            value={v.whatsAppMessage}
            onChange={set("whatsAppMessage")}
            disabled={busy}
            rows={2}
            maxLength={300}
            placeholder={t(defaultWhatsAppMessage)}
            className="w-full resize-none rounded-xl border border-line bg-surface px-4 py-2.5 text-[16px] outline-none focus:border-primary"
          />
          <span className="mt-1 block text-xs text-ink-2">
            {t("Ziyarətçi WhatsApp düyməsinə basanda bu mətn yazılmış halda açılır. Boş qoysanız, mətn olmadan açılır.")}
          </span>
        </label>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        {field("Instagram", "instagram", { placeholder: "https://instagram.com/…" }, <InstagramIcon className="size-5 text-[#E4405F]" />)}
        {field("TikTok", "tiktok", { placeholder: "https://tiktok.com/@…" }, <TikTokIcon className="size-5" />)}
        {field("YouTube", "youtube", { placeholder: "https://youtube.com/@…" }, <YouTubeIcon className="size-5 text-[#FF0000]" />)}
        {field("Facebook", "facebook", { placeholder: "https://facebook.com/…" }, <FacebookIcon className="size-5 text-[#1877F2]" />)}
      </div>
      {field(t("Sayt"), "website", { placeholder: "https://…" })}

      <fieldset className="space-y-2">
        <legend className="mb-1 flex w-full items-center justify-between text-sm font-bold">
          {t("İş saatları")}
          <button type="button" onClick={applyToAll} className="text-xs font-bold text-primary hover:underline">
            {t("İlk açıq günün saatını hamısına yaz")}
          </button>
        </legend>
        {days.map((d, i) => (
          <div key={i} className="flex flex-wrap items-center gap-3 rounded-xl border border-line px-3 py-2">
            <label className="flex w-44 cursor-pointer items-center gap-2 text-sm font-semibold">
              <input type="checkbox" checked={d.open} onChange={(e) => setDay(i, { open: e.target.checked })} disabled={busy} className="size-4 accent-primary" />
              {t(weekdayNames[i])}
            </label>
            {d.open ? (
              <span className="flex items-center gap-2">
                <input type="time" value={d.from} onChange={(e) => setDay(i, { from: e.target.value })} disabled={busy} className="h-10 rounded-lg border border-line bg-surface px-2" />
                <span aria-hidden>–</span>
                <input type="time" value={d.to} onChange={(e) => setDay(i, { to: e.target.value })} disabled={busy} className="h-10 rounded-lg border border-line bg-surface px-2" />
              </span>
            ) : (
              <span className="text-sm text-ink-2">{t("Bağlıdır")}</span>
            )}
          </div>
        ))}
      </fieldset>

      <div className="flex flex-wrap items-center gap-3">
        <button
          type="submit"
          disabled={busy}
          className="flex h-11 items-center gap-2 rounded-xl bg-primary px-6 font-bold text-white hover:bg-primary-press disabled:opacity-60"
        >
          {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
          {t("Yadda saxla")}
        </button>
        {message && (
          <p role={message.ok ? "status" : "alert"} className={`text-sm font-semibold ${message.ok ? "text-success" : "text-danger"}`}>
            {message.text}
          </p>
        )}
      </div>
    </form>
  );
}
