"use client";

import { useState } from "react";
import { Check, ChevronDown } from "lucide-react";
import { Popup } from "./Popup";

export type Option = { value: string; label: string };

/**
 * A drop-down list in our own style. It fills a hidden field with the chosen
 * value, so it works inside a plain form (the filters) and, with `onChange`,
 * inside React forms.
 */
export function Select({
  name,
  options,
  value,
  defaultValue = "",
  onChange,
  placeholder = "",
  ariaLabel,
  disabled = false,
  className = "h-11",
}: {
  name?: string;
  options: Option[];
  value?: string;
  defaultValue?: string;
  onChange?: (value: string) => void;
  placeholder?: string;
  ariaLabel?: string;
  disabled?: boolean;
  className?: string;
}) {
  const [inner, setInner] = useState(defaultValue);
  const current = value ?? inner;
  const picked = options.find((o) => o.value === current);

  return (
    <>
      {name && <input type="hidden" name={name} value={current} />}
      <Popup
        fullWidth
        align="left"
        disabled={disabled}
        ariaLabel={ariaLabel ?? placeholder}
        title={ariaLabel ?? placeholder}
        panelClassName=""
        buttonClassName={`flex w-full items-center justify-between gap-2 rounded-lg border border-line bg-surface px-3 text-left text-[15px] outline-none hover:border-primary focus-visible:border-primary disabled:opacity-60 ${className}`}
        button={(open) => (
          <>
            <span className={`truncate ${picked ? "" : "text-muted"}`}>{picked?.label ?? placeholder}</span>
            <ChevronDown className={`size-4 shrink-0 text-ink-2 transition-transform ${open ? "rotate-180" : ""}`} aria-hidden />
          </>
        )}
      >
        {(close) => (
          <ul role="listbox" className="py-1">
            {options.map((o) => {
              const on = o.value === current;
              return (
                <li key={o.value} role="option" aria-selected={on}>
                  <button
                    type="button"
                    onClick={() => {
                      setInner(o.value);
                      onChange?.(o.value);
                      close();
                    }}
                    className={`flex w-full items-center justify-between gap-3 px-4 py-3 text-left text-[15px] md:py-2.5 ${
                      on ? "bg-primary-soft font-bold text-primary" : "hover:bg-primary-soft"
                    }`}
                  >
                    <span className="truncate">{o.label}</span>
                    {on && <Check className="size-4 shrink-0" aria-hidden />}
                  </button>
                </li>
              );
            })}
          </ul>
        )}
      </Popup>
    </>
  );
}
