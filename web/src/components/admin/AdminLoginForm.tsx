"use client";

import { useRouter } from "next/navigation";
import { useEffect, useRef, useState, useTransition } from "react";
import { ArrowLeft, Loader2 } from "lucide-react";
import { adminRequestCode, adminVerifyCode } from "@/app/admin/giris/actions";
import { formatNational } from "@/lib/phone";

type Step = { name: "phone" } | { name: "code"; challengeId: string; resendAt: number };

const field =
  "h-11 w-full rounded-lg border border-line bg-surface px-3.5 text-[16px] outline-none focus:border-primary";
const button =
  "flex h-11 w-full items-center justify-center gap-2 rounded-lg bg-primary text-[15px] font-bold text-white hover:bg-primary-press disabled:opacity-60";

/** The admin's own sign-in: phone → SMS code. No sign-up here. */
export function AdminLoginForm({ next }: { next: string }) {
  const router = useRouter();
  const [step, setStep] = useState<Step>({ name: "phone" });
  const [phone, setPhone] = useState("");
  const [code, setCode] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [pending, start] = useTransition();
  const [now, setNow] = useState(() => Date.now());
  const codeInput = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (step.name !== "code") return;
    const id = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(id);
  }, [step]);

  useEffect(() => {
    if (step.name === "code") codeInput.current?.focus();
  }, [step.name]);

  function send() {
    setError(null);
    start(async () => {
      const res = await adminRequestCode(phone);
      if (!res.ok) return setError(res.message);
      setCode("");
      setNow(Date.now());
      setStep({ name: "code", challengeId: res.challengeId, resendAt: res.resendAt });
    });
  }

  function verify(value = code) {
    if (step.name !== "code") return;
    setError(null);
    start(async () => {
      const res = await adminVerifyCode({ challengeId: step.challengeId, code: value });
      if (!res.ok) return setError(res.message);
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
          <span className="mb-1.5 block text-sm font-bold">Mobil nömrə</span>
          <div className="flex gap-2">
            <span className="flex h-11 items-center rounded-lg border border-line bg-surface px-3.5 font-semibold text-ink-2">
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
        {error && (
          <p role="alert" className="text-sm font-semibold text-danger">
            {error}
          </p>
        )}
        <button type="submit" disabled={pending} className={button}>
          {pending && <Loader2 className="size-5 animate-spin" aria-hidden />}
          Kodu göndər
        </button>
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
      <label className="block">
        <span className="mb-1.5 block text-sm font-bold">SMS kodu</span>
        <input
          ref={codeInput}
          inputMode="numeric"
          autoComplete="one-time-code"
          maxLength={6}
          value={code}
          onChange={(e) => {
            const value = e.target.value.replace(/\D/g, "").slice(0, 6);
            setCode(value);
            if (value.length === 6) verify(value);
          }}
          placeholder="••••••"
          className={`${field} text-center text-2xl font-extrabold tracking-[0.5em]`}
        />
      </label>
      {error && (
        <p role="alert" className="text-sm font-semibold text-danger">
          {error}
        </p>
      )}
      <button type="submit" disabled={pending || code.length !== 6} className={button}>
        {pending && <Loader2 className="size-5 animate-spin" aria-hidden />}
        Daxil ol
      </button>
      <button
        type="button"
        disabled={left > 0 || pending}
        onClick={send}
        className="w-full text-center text-sm font-semibold text-primary disabled:text-ink-2"
      >
        {left > 0 ? `Yenidən göndər (${left} san.)` : "Kodu yenidən göndər"}
      </button>
    </form>
  );
}
