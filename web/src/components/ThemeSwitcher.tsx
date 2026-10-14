"use client";

import { useState } from "react";
import { Check, Monitor, Moon, Sun } from "lucide-react";
import { useT } from "@/lib/i18n/client";
import { tx } from "@/lib/i18n/locales";
import type { Theme } from "@/lib/theme";
import { setTheme } from "@/lib/theme-actions";
import { Popup } from "./ui/Popup";

const items: { id: Theme; label: string; Icon: typeof Sun }[] = [
  { id: "light", label: tx("Gündüz"), Icon: Sun },
  { id: "dark", label: tx("Gecə"), Icon: Moon },
  { id: "auto", label: tx("Avtomatik"), Icon: Monitor },
];

/** Day, night or automatic (as the device), like the app. Applied at once and remembered. */
export function ThemeSwitcher({ initial, buttonClassName }: { initial: Theme; buttonClassName?: string }) {
  const t = useT();
  const [theme, setChosen] = useState<Theme>(initial);
  const Current = items.find((i) => i.id === theme)?.Icon ?? Monitor;

  function choose(next: Theme) {
    setChosen(next);
    const root = document.documentElement;
    if (next === "auto") root.removeAttribute("data-theme");
    else root.setAttribute("data-theme", next);
    void setTheme(next);
  }

  return (
    <Popup
      ariaLabel={t("Görünüş")}
      phone="dropdown"
      panelClassName="w-48"
      buttonClassName={
        buttonClassName ??
        "flex size-10 items-center justify-center rounded-lg text-ink-2 hover:bg-primary-soft hover:text-primary"
      }
      button={() => <Current className="size-5" aria-hidden />}
    >
      {(close) => (
        <ul className="py-1">
          {items.map(({ id, label, Icon }) => (
            <li key={id}>
              <button
                type="button"
                onClick={() => {
                  choose(id);
                  close();
                }}
                className={`flex w-full items-center gap-3 px-4 py-3 text-left text-[15px] md:py-2.5 ${
                  theme === id ? "bg-primary-soft font-bold text-primary" : "hover:bg-primary-soft"
                }`}
              >
                <Icon className="size-5" aria-hidden />
                <span className="flex-1">{t(label)}</span>
                {theme === id && <Check className="size-4" aria-hidden />}
              </button>
            </li>
          ))}
        </ul>
      )}
    </Popup>
  );
}
