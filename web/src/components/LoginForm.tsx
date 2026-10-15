"use client";

import { useT } from "@/lib/i18n/client";
import { useEffect, useRef, useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { ArrowLeft, Loader2 } from "lucide-react";
import { requestCode, verifyCode } from "@/app/giris/actions";
import Link from "next/link";
import { formatNational } from "@/lib/phone";

type Step =
  | { name: "phone" }
  | { name: "code"; challengeId: string; isRegistration: boolean; resendAt: number };

/** Phone → code (→ name for a new account) → signed in. */
export function LoginForm({ next }: { next: string }) {
  const t = useT();
  const router = useRouter();
  const [step, setStep] = useState<Step>({ name: "phone" });
  const [phone, setPhone] = useState("");
  const [code, setCode] = useState("");
  const [fullName, setFullName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [pending, start] = useTransition();
  const [now, setNow] = useState(() => Date.now());
  const codeInput = useRef<HTMLInputElement>(null);

  // The countdown until "send again" is allowed.
  useEffect(() => {
    if (step.name !== "code") return;
    const id = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(id);
  }, [step]);

  useEffect(() => {
    if (step.name === "code") codeInput.current?.focus();
  }, [step.name]);

  const field =
    "h-11 w-full rounded-xl border border-line bg-surface px-4 text-[16px] outline-none focus:border-primary";
  const button =
    "flex h-11 w-full items-center justify-center gap-2 rounded-xl bg-primary text-[15px] font-bold text-white hover:bg-primary-press disabled:opacity-60";

  function send() {
    setError(null);
    start(async () => {
      const result = await requestCode(phone);
      if (!result.ok) return setError(result.message);
      setCode("");
      setNow(Date.now());
      setStep({
        name: "code",
        challengeId: result.challengeId,
        isRegistration: result.isRegistration,
        resendAt: result.resendAt,
      });
    });
  }

  function verify(value = code) {
    if (step.name !== "code") return;
    setError(null);
    start(async () => {
      const result = await verifyCode({
        challengeId: step.challengeId,
        code: value,
        isRegistration: step.isRegistration,
        fullName,
      });
      if (!result.ok) return setError(result.message);
      router.replace(next);
      router.refresh();
    });
  }

  if (step.name === "phone") {
    return (
      <form
        onSubmit={(e) => {
          e.preventDefault();
          send();
        }}
        className="space-y-4"
      >
        <label className="block">
          <span className="mb-2 block text-sm font-bold">{t("Mobil nömrə")}</span>
          <div className="flex gap-2">
            <span className="flex h-11 items-center rounded-xl border border-line bg-surface px-4 font-semibold text-ink-2">
              +994
            </span>
            <input
              type="tel"
              inputMode="numeric"
              autoComplete="tel-national"
              autoFocus
              value={phone}
              onChange={(e) => setPhone(formatNational(e.target.value))}
              placeholder="50 123 45 67"
              className={field}
            />
          </div>
        </label>
        {error && <p role="alert" className="text-sm font-semibold text-danger">{error}</p>}
        <button type="submit" disabled={pending} className={button}>
          {pending && <Loader2 className="size-5 animate-spin" aria-hidden />}
          {t("Kodu göndər")}</button>
        <p className="text-center text-xs text-ink-2">
          {t("Nömrənizə təsdiq kodu SMS ilə göndəriləcək. Hesabınız yoxdursa, avtomatik yaradılacaq.")}</p>
        <p className="text-center text-xs text-ink-2">
          {t("Davam etməklə {terms} və {privacy} ilə razılaşırsınız.", { terms: "\u0001", privacy: "\u0002" })
            .split(/(\u0001|\u0002)/)
            .map((part, i) =>
              part === "\u0001" ? (
                <Link key={i} href="/sertler" className="font-semibold text-primary hover:underline">
                  {t("İstifadə şərtləri")}
                </Link>
              ) : part === "\u0002" ? (
                <Link key={i} href="/mexfilik" className="font-semibold text-primary hover:underline">
                  {t("Məxfilik siyasəti")}
                </Link>
              ) : (
                part
              ),
            )}
        </p>
      </form>
    );
  }

  const left = Math.max(0, Math.ceil((step.resendAt - now) / 1000));

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        verify();
      }}
      className="space-y-4"
    >
      <button
        type="button"
        onClick={() => {
          setStep({ name: "phone" });
          setError(null);
        }}
        className="flex items-center gap-1 text-sm font-semibold text-ink-2 hover:text-primary"
      >
        <ArrowLeft className="size-4" aria-hidden />
        +994 {phone}
      </button>

      {step.isRegistration && (
        <label className="block">
          <span className="mb-2 block text-sm font-bold">{t("Adınız")}</span>
          <input
            autoComplete="name"
            value={fullName}
            onChange={(e) => setFullName(e.target.value)}
            placeholder={t("Ad və soyad")}
            maxLength={100}
            className={field}
          />
        </label>
      )}

      <label className="block">
        <span className="mb-2 block text-sm font-bold">{t("SMS kodu")}</span>
        <input
          ref={codeInput}
          inputMode="numeric"
          autoComplete="one-time-code"
          maxLength={6}
          value={code}
          onChange={(e) => {
            const value = e.target.value.replace(/\D/g, "").slice(0, 6);
            setCode(value);
            // A full code is sent at once (a new account still needs its name).
            if (value.length === 6 && !step.isRegistration) verify(value);
          }}
          placeholder="••••••"
          className={`${field} text-center text-2xl font-extrabold tracking-[0.5em]`}
        />
      </label>

      {error && <p role="alert" className="text-sm font-semibold text-danger">{error}</p>}

      <button type="submit" disabled={pending || code.length !== 6} className={button}>
        {pending && <Loader2 className="size-5 animate-spin" aria-hidden />}
        {step.isRegistration ? t("Qeydiyyatdan keç") : t("Daxil ol")}
      </button>

      <button
        type="button"
        disabled={left > 0 || pending}
        onClick={send}
        className="w-full text-center text-sm font-semibold text-primary disabled:text-ink-2"
      >
        {left > 0 ? t("Yenidən göndər ({n} san.)", { n: left }) : t("Kodu yenidən göndər")}
      </button>
    </form>
  );
}
