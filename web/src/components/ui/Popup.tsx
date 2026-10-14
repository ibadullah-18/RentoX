"use client";

import { useEffect, useId, useRef, useState } from "react";
import { X } from "lucide-react";
import { useT } from "@/lib/i18n/client";

/**
 * Our own pop-up window (never the browser's): a small panel under its button
 * on a computer, a sheet rising from the bottom on a phone. Closes on a tap
 * outside, on Escape, and when a child asks to.
 */
export function Popup({
  button,
  buttonClassName,
  ariaLabel,
  title,
  align = "right",
  panelClassName = "md:w-80",
  fullWidth = false,
  phone = "sheet",
  disabled = false,
  onOpenChange,
  children,
}: {
  button: (open: boolean) => React.ReactNode;
  buttonClassName: string;
  ariaLabel: string;
  /** Shown at the top of the phone sheet. */
  title?: string;
  align?: "left" | "right";
  panelClassName?: string;
  /** The panel is as wide as the button (a drop-down list). */
  fullWidth?: boolean;
  /**
   * On a phone: "sheet" rises from the bottom (long lists), "dropdown" opens
   * small under its button, "top" is a small window across the top.
   */
  phone?: "sheet" | "dropdown" | "top";
  disabled?: boolean;
  onOpenChange?: (open: boolean) => void;
  children: (close: () => void) => React.ReactNode;
}) {
  const t = useT();
  const id = useId();
  const [open, setOpenState] = useState(false);
  const root = useRef<HTMLDivElement>(null);

  const setOpen = (value: boolean) => {
    setOpenState(value);
    onOpenChange?.(value);
  };

  useEffect(() => {
    if (!open) return;
    const onDown = (e: PointerEvent) => {
      if (root.current && !root.current.contains(e.target as Node)) {
        setOpenState(false);
        onOpenChange?.(false);
      }
    };
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        setOpenState(false);
        onOpenChange?.(false);
      }
    };
    document.addEventListener("pointerdown", onDown);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("pointerdown", onDown);
      document.removeEventListener("keydown", onKey);
    };
  }, [open, onOpenChange]);

  return (
    <div ref={root} className={fullWidth ? "relative block" : "relative inline-block"}>
      <button
        type="button"
        aria-label={ariaLabel}
        aria-haspopup="dialog"
        aria-expanded={open}
        aria-controls={open ? id : undefined}
        disabled={disabled}
        onClick={() => setOpen(!open)}
        className={buttonClassName}
      >
        {button(open)}
      </button>

      {open && (
        <>
          {phone === "sheet" && <div className="fixed inset-0 z-[80] bg-black/50 md:hidden" aria-hidden />}
          <div
            id={id}
            role="dialog"
            aria-label={title ?? ariaLabel}
            className={`z-[90] overflow-hidden border border-line bg-surface text-ink shadow-xl md:absolute md:top-full md:mt-2 md:rounded-xl ${
              phone === "sheet"
                ? "max-md:fixed max-md:inset-x-0 max-md:bottom-0 max-md:max-h-[85dvh] max-md:rounded-t-2xl max-md:border-b-0"
                : phone === "top"
                  ? "max-md:fixed max-md:inset-x-3 max-md:top-14 max-md:rounded-xl"
                  : "max-md:absolute max-md:top-full max-md:mt-2 max-md:rounded-xl"
            } ${align === "right" ? "md:right-0" : "md:left-0"} ${
              phone === "dropdown" ? (align === "right" ? "max-md:right-0" : "max-md:left-0") : ""
            } ${fullWidth ? "w-full" : ""} ${panelClassName}`}
          >
            {phone === "sheet" && (
              <div className="flex items-center justify-between px-4 pb-1 pt-3 md:hidden">
                <span className="font-extrabold">{title ?? ariaLabel}</span>
                <button
                  type="button"
                  onClick={() => setOpen(false)}
                  aria-label={t("Bağla")}
                  className="flex size-9 items-center justify-center rounded-lg text-ink-2 hover:bg-primary-soft"
                >
                  <X className="size-5" />
                </button>
              </div>
            )}
            <div className="max-h-[60dvh] overflow-y-auto overscroll-contain md:max-h-[min(70vh,520px)]">
              {children(() => setOpen(false))}
            </div>
          </div>
        </>
      )}
    </div>
  );
}
