"use client";

import { useT } from "@/lib/i18n/client";
import { useState } from "react";
import { Phone } from "lucide-react";

/**
 * Phone: one tap calls. Computer: first shows the number (so it can be
 * copied), then it is a normal tel: link.
 */
export function PhoneButton({ phone }: { phone: string }) {
  const t = useT();
  const [shown, setShown] = useState(false);
  const base =
    "flex h-10 w-full items-center justify-center gap-1.5 rounded-lg bg-success px-3 text-[14px] font-bold text-white hover:opacity-90";

  return (
    <>
      <a href={`tel:${phone}`} className={`${base} md:hidden`}>
        <Phone className="size-4" aria-hidden />
        {t("Zəng et")}
      </a>
      {shown ? (
        <a href={`tel:${phone}`} className={`${base} max-md:hidden`}>
          <Phone className="size-4" aria-hidden />
          {phone}
        </a>
      ) : (
        <button
          type="button"
          onClick={() => setShown(true)}
          className={`${base} max-md:hidden`}
        >
          <Phone className="size-4" aria-hidden />
          {t("Nömrəni göstər")}
        </button>
      )}
    </>
  );
}
