"use client";

import { useT } from "@/lib/i18n/client";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { Loader2 } from "lucide-react";
import { createStore, updateStore } from "@/app/magaza/actions";
import {
  StoreRules,
  validateStoreForm,
  type StoreErrors,
  type StoreFormValues,
} from "@/lib/store-form";

const input =
  "h-11 w-full rounded-xl border border-line bg-surface px-4 text-[16px] outline-none focus:border-primary disabled:opacity-60";

function Row({
  label,
  hint,
  error,
  children,
}: {
  label: string;
  hint?: string;
  error?: string;
  children: React.ReactNode;
}) {
  return (
    <label className="block">
      <span className="mb-2 block text-sm font-bold">{label}</span>
      {children}
      {hint && !error && <span className="mt-1.5 block text-xs text-ink-2">{hint}</span>}
      {error && (
        <span role="alert" className="mt-1.5 block text-sm font-semibold text-danger">
          {error}
        </span>
      )}
    </label>
  );
}

/** Create the store, or change it while it is a draft or was rejected. */
export function StoreForm({
  initial,
  mode,
}: {
  initial: StoreFormValues;
  mode: "create" | "edit";
}) {
  const t = useT();
  const router = useRouter();
  const [values, setValues] = useState(initial);
  const [errors, setErrors] = useState<StoreErrors>({});
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<{ ok: boolean; text: string } | null>(null);

  const set = (key: keyof StoreFormValues) => (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    setValues((v) => ({ ...v, [key]: e.target.value }));
    if (errors[key]) setErrors((er) => ({ ...er, [key]: undefined }));
  };

  async function save() {
    if (busy) return;
    const found = validateStoreForm(values, t);
    setErrors(found);
    if (Object.keys(found).length > 0) {
      setMessage(null);
      return;
    }
    setBusy(true);
    setMessage(null);
    const result = await (mode === "create" ? createStore(values) : updateStore(values));
    setBusy(false);
    if (result.ok) {
      setMessage({ ok: true, text: mode === "create" ? t("Mağaza yaradıldı.") : t("Yadda saxlanıldı.") });
      router.refresh();
    } else if (result.unauthorized) {
      router.push("/giris?next=/magaza");
    } else {
      setMessage({ ok: false, text: result.message });
    }
  }

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        void save();
      }}
      noValidate
      className="space-y-4 rounded-2xl border border-line bg-surface p-5 md:p-6"
    >
      <Row label={t("Mağaza adı")} error={errors.name}>
        <input value={values.name} onChange={set("name")} maxLength={StoreRules.nameMax} disabled={busy} className={input} />
      </Row>

      <Row label={t("Təsvir")} hint={t("Mağazanın nə təklif etdiyini yazın (ən azı {min} simvol).", { min: StoreRules.descriptionMin })} error={errors.description}>
        <textarea
          value={values.description}
          onChange={set("description")}
          maxLength={StoreRules.descriptionMax}
          rows={5}
          disabled={busy}
          className={`${input} h-auto py-3`}
        />
      </Row>

      <div className="grid gap-4 sm:grid-cols-2">
        <Row label={t("Telefon")} error={errors.phone}>
          <input
            value={values.phone}
            onChange={set("phone")}
            inputMode="tel"
            maxLength={StoreRules.phoneMax}
            placeholder="+994 50 123 45 67"
            disabled={busy}
            className={input}
          />
        </Row>
        <Row label={t("E-poçt (ixtiyari)")} error={errors.email}>
          <input
            value={values.email}
            onChange={set("email")}
            type="email"
            maxLength={StoreRules.emailMax}
            disabled={busy}
            className={input}
          />
        </Row>
      </div>

      <Row label={t("Ünvan (ixtiyari)")} error={errors.address}>
        <input value={values.address} onChange={set("address")} maxLength={StoreRules.addressMax} disabled={busy} className={input} />
      </Row>

      <div className="grid gap-4 sm:grid-cols-2">
        <Row label={t("Sayt (ixtiyari)")} error={errors.website}>
          <input value={values.website} onChange={set("website")} placeholder={t("https://")} disabled={busy} className={input} />
        </Row>
        <Row label={t("Instagram (ixtiyari)")} error={errors.instagram}>
          <input value={values.instagram} onChange={set("instagram")} placeholder={t("https://instagram.com/…")} disabled={busy} className={input} />
        </Row>
        <Row label={t("TikTok (ixtiyari)")} error={errors.tiktok}>
          <input value={values.tiktok} onChange={set("tiktok")} placeholder={t("https://tiktok.com/@…")} disabled={busy} className={input} />
        </Row>
        <Row label={t("Facebook (ixtiyari)")} error={errors.facebook}>
          <input value={values.facebook} onChange={set("facebook")} placeholder={t("https://facebook.com/…")} disabled={busy} className={input} />
        </Row>
      </div>

      {message && (
        <p
          role={message.ok ? "status" : "alert"}
          className={`rounded-xl px-4 py-3 text-sm font-semibold ${
            message.ok ? "bg-success-soft text-success" : "bg-danger/10 text-danger"
          }`}
        >
          {message.text}
        </p>
      )}

      <button
        type="submit"
        disabled={busy}
        className="flex h-11 w-full items-center justify-center gap-2 rounded-xl bg-primary font-bold text-white hover:bg-primary-press disabled:opacity-60"
      >
        {busy && <Loader2 className="size-5 animate-spin" aria-hidden />}
        {mode === "create" ? t("Mağaza yarat") : t("Yadda saxla")}
      </button>
    </form>
  );
}
