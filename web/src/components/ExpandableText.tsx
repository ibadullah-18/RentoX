"use client";

import { useT } from "@/lib/i18n/client";
import { useState } from "react";

/** Long text is cut after a few lines, with "Davamını oxu". */
export function ExpandableText({ text }: { text: string }) {
  const t = useT();
  const [open, setOpen] = useState(false);
  const long = text.length > 280 || text.split("\n").length > 5;

  return (
    <div>
      <p
        className={`whitespace-pre-line text-[15px] leading-relaxed text-ink ${
          long && !open ? "line-clamp-5" : ""
        }`}
      >
        {text}
      </p>
      {long && (
        <button
          type="button"
          onClick={() => setOpen((v) => !v)}
          className="mt-2 text-sm font-bold text-primary hover:underline"
        >
          {open ? t("Daha az") : t("Davamını oxu")}
        </button>
      )}
    </div>
  );
}
