"use client";

import { useRouter } from "next/navigation";
import { useTransition } from "react";
import { Check } from "lucide-react";
import { setLocale } from "@/lib/i18n/actions";
import { useLocale, useT } from "@/lib/i18n/client";
import { LOCALES, type Locale } from "@/lib/i18n/locales";
import { Popup } from "./ui/Popup";

/** Picks the site's language (AZ / EN / RU); the page is rendered again in it. */
export function LanguageSwitcher({ buttonClassName }: { buttonClassName?: string }) {
  const t = useT();
  const locale = useLocale();
  const router = useRouter();
  const [pending, start] = useTransition();

  const change = (next: Locale) =>
    start(async () => {
      await setLocale(next);
      router.refresh();
    });

  return (
    <Popup
      ariaLabel={t("Dil")}
      disabled={pending}
      phone="dropdown"
      panelClassName="w-28"
      buttonClassName={
        buttonClassName ??
        "flex h-10 min-w-10 items-center justify-center rounded-lg px-2 text-sm font-extrabold uppercase text-ink-2 hover:bg-primary-soft hover:text-primary"
      }
      button={() => <span>{locale}</span>}
    >
      {(close) => (
        <ul className="py-1">
          {LOCALES.map((code) => (
            <li key={code}>
              <button
                type="button"
                onClick={() => {
                  close();
                  if (code !== locale) change(code);
                }}
                className={`flex w-full items-center gap-3 px-4 py-3 text-left text-[15px] md:py-2.5 ${
                  code === locale ? "bg-primary-soft font-bold text-primary" : "hover:bg-primary-soft"
                }`}
              >
                <span className="flex-1 font-extrabold uppercase">{code}</span>
                {code === locale && <Check className="size-4" aria-hidden />}
              </button>
            </li>
          ))}
        </ul>
      )}
    </Popup>
  );
}
