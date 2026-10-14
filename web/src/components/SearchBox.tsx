"use client";

import { useLocale, useT } from "@/lib/i18n/client";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useEffect, useId, useRef, useState } from "react";
import { ArrowLeft, History, Search, Tag, X } from "lucide-react";

type Suggestion = {
  kind: "word" | "category";
  text: string;
  categoryId?: string | null;
  categoryPath: string[];
};

const RECENT_KEY = "rx_recent_searches";

function readRecent(): string[] {
  try {
    const parsed: unknown = JSON.parse(localStorage.getItem(RECENT_KEY) ?? "[]");
    return Array.isArray(parsed) ? parsed.filter((x): x is string => typeof x === "string").slice(0, 6) : [];
  } catch {
    return [];
  }
}

function remember(text: string) {
  try {
    const next = [text, ...readRecent().filter((x) => x.toLowerCase() !== text.toLowerCase())].slice(0, 6);
    localStorage.setItem(RECENT_KEY, JSON.stringify(next));
  } catch {
    // Private mode: recent searches are a convenience, not a need.
  }
}

/**
 * The search bar. It is a plain GET form (works before JavaScript loads);
 * with JavaScript it also suggests words with their category chain while you
 * type, and your last searches when it is empty. Picking a suggestion opens
 * the results inside that category.
 */
export function SearchBox({ className = "" }: { className?: string }) {
  const t = useT();
  const locale = useLocale();
  const pathname = usePathname();
  const params = useSearchParams();
  const router = useRouter();
  const listId = useId();
  const current = pathname === "/search" ? (params.get("q") ?? "") : "";

  const [text, setText] = useState(current);
  const [seenCurrent, setSeenCurrent] = useState(current);
  const [open, setOpen] = useState(false);
  const [found, setFound] = useState<{ for: string; list: Suggestion[] }>({ for: "", list: [] });
  const [recent, setRecent] = useState<string[]>([]);
  const [active, setActive] = useState(-1);
  const [sheet, setSheet] = useState(false);
  const box = useRef<HTMLDivElement>(null);

  // A new search (from a link, back/forward) replaces what is typed.
  if (current !== seenCurrent) {
    setSeenCurrent(current);
    setText(current);
  }

  const query = text.trim();

  useEffect(() => {
    if (query.length < 2) return;
    let cancelled = false;
    const timer = setTimeout(() => {
      fetch(`/api/listings/suggestions?q=${encodeURIComponent(query)}&language=${locale}`)
        .then((r) => (r.ok ? (r.json() as Promise<Suggestion[]>) : []))
        .then((list) => {
          if (!cancelled) setFound({ for: query, list: list.slice(0, 8) });
        })
        .catch(() => {});
    }, 250);
    return () => {
      cancelled = true;
      clearTimeout(timer);
    };
  }, [query, locale]);

  useEffect(() => {
    if (!open) return;
    const onDown = (e: MouseEvent) => {
      if (box.current && !box.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", onDown);
    return () => document.removeEventListener("mousedown", onDown);
  }, [open]);

  // What the list shows: suggestions for what is typed, or recent searches.
  const suggestions = query.length >= 2 && found.for === query ? found.list : [];
  const showRecent = query.length < 2 && recent.length > 0;
  const rows: { key: string; label: string; hint?: string; go: () => void; icon: "tag" | "history" | "search" }[] =
    showRecent
      ? recent.map((r) => ({
          key: `r-${r}`,
          label: r,
          icon: "history" as const,
          go: () => search(r),
        }))
      : suggestions.map((s) => ({
          key: `${s.kind}-${s.text}-${s.categoryId}`,
          label: s.text,
          hint: s.categoryPath.length > 0 ? s.categoryPath.join(" › ") : s.kind === "category" ? "Kateqoriya" : undefined,
          icon: s.kind === "category" ? ("tag" as const) : ("search" as const),
          go: () => pick(s),
        }));

  function search(value: string, categoryId?: string | null) {
    const q = value.trim();
    setOpen(false);
    setSheet(false);
    if (q) remember(q);
    const next = new URLSearchParams();
    if (q) next.set("q", q);
    if (categoryId) next.set("category", categoryId);
    router.push(next.size > 0 ? `/search?${next}` : "/search");
  }

  function pick(s: Suggestion) {
    setText(s.kind === "category" ? "" : s.text);
    // A category on its own opens that category; a word opens its results there.
    search(s.kind === "category" ? "" : s.text, s.categoryId);
  }

  function onKey(e: React.KeyboardEvent) {
    if (!open || rows.length === 0) return;
    if (e.key === "ArrowDown") {
      e.preventDefault();
      setActive((i) => (i + 1) % rows.length);
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      setActive((i) => (i <= 0 ? rows.length - 1 : i - 1));
    } else if (e.key === "Escape") {
      setOpen(false);
    } else if (e.key === "Enter" && active >= 0) {
      e.preventDefault();
      rows[active].go();
    }
  }

  const Icon = { tag: Tag, history: History, search: Search };

  const list = (
    <ul id={listId} role="listbox">
      {showRecent && <li className="px-4 pb-1 pt-3 text-xs font-bold uppercase text-ink-2">{t("Son axtarışlar")}</li>}
      {rows.map((row, i) => {
        const RowIcon = Icon[row.icon];
        return (
          <li key={row.key} role="option" aria-selected={i === active}>
            <button
              type="button"
              onMouseDown={(e) => e.preventDefault()}
              onClick={row.go}
              onMouseEnter={() => setActive(i)}
              className={`flex w-full items-center gap-3 px-4 py-3 text-left md:py-2.5 ${i === active ? "bg-primary-soft" : ""}`}
            >
              <RowIcon className="size-4 shrink-0 text-ink-2" aria-hidden />
              <span className="min-w-0">
                <span className="block truncate font-bold">{row.label}</span>
                {row.hint && <span className="block truncate text-xs text-ink-2">{row.hint}</span>}
              </span>
            </button>
          </li>
        );
      })}
    </ul>
  );

  return (
    <div ref={box} className={`relative ${className}`}>
      {/* Phone: a bar that opens the search window over the page. */}
      <button
        type="button"
        onClick={() => {
          setRecent(readRecent());
          setSheet(true);
        }}
        className="flex h-11 w-full items-center gap-2.5 rounded-lg border border-line bg-surface px-3.5 text-left md:hidden"
      >
        <Search className="size-5 shrink-0 text-muted" aria-hidden />
        <span className={`truncate text-[15px] ${current ? "font-semibold" : "text-muted"}`}>
          {current || t("Nə kirayə axtarırsınız?")}
        </span>
      </button>

      {/* Computer: the bar itself, with the list under it. */}
      <form
        action="/search"
        role="search"
        onSubmit={(e) => {
          // With JavaScript: the same as the plain form, but remembered.
          e.preventDefault();
          if (active >= 0 && rows[active]) rows[active].go();
          else search(text);
        }}
        className="flex h-10 items-center overflow-hidden rounded-lg border border-line bg-surface focus-within:border-primary max-md:hidden"
      >
        <Search className="ml-3.5 size-[18px] shrink-0 text-muted" aria-hidden />
        <input
          name="q"
          value={text}
          onChange={(e) => {
            setText(e.target.value);
            setActive(-1);
            setOpen(true);
          }}
          onFocus={() => {
            setRecent(readRecent());
            setOpen(true);
          }}
          onKeyDown={onKey}
          placeholder={t("Nə kirayə axtarırsınız?")}
          aria-label={t("Nə kirayə axtarırsınız?")}
          role="combobox"
          aria-expanded={open && rows.length > 0}
          aria-controls={listId}
          aria-autocomplete="list"
          autoComplete="off"
          className="h-full min-w-0 flex-1 bg-transparent px-3 text-[15px] outline-none placeholder:text-muted"
        />
        <button
          type="submit"
          className="h-full bg-primary px-5 text-[14px] font-bold text-white transition-colors hover:bg-primary-press"
        >
          {t("Axtar")}
        </button>
      </form>

      {open && rows.length > 0 && !sheet && (
        <div className="absolute inset-x-0 top-12 z-50 overflow-hidden rounded-xl border border-line bg-surface shadow-xl max-md:hidden">
          {list}
        </div>
      )}

      {sheet && (
        <div role="dialog" aria-modal="true" aria-label={t("Axtar")} className="fixed inset-0 z-[95] flex flex-col bg-bg md:hidden">
          <form
            action="/search"
            role="search"
            onSubmit={(e) => {
              e.preventDefault();
              if (active >= 0 && rows[active]) rows[active].go();
              else search(text);
            }}
            className="flex items-center gap-2 border-b border-line bg-surface px-3 py-2.5"
          >
            <button
              type="button"
              onClick={() => setSheet(false)}
              aria-label={t("Geri")}
              className="flex size-10 shrink-0 items-center justify-center rounded-lg text-ink-2 hover:bg-primary-soft"
            >
              <ArrowLeft className="size-5" />
            </button>
            <div className="flex h-11 min-w-0 flex-1 items-center rounded-lg border border-line bg-bg px-3 focus-within:border-primary">
              <input
                name="q"
                autoFocus
                value={text}
                onChange={(e) => {
                  setText(e.target.value);
                  setActive(-1);
                }}
                onKeyDown={(e) => {
                  if (e.key === "Escape") setSheet(false);
                }}
                enterKeyHint="search"
                placeholder={t("Nə kirayə axtarırsınız?")}
                aria-label={t("Nə kirayə axtarırsınız?")}
                autoComplete="off"
                className="h-full min-w-0 flex-1 bg-transparent text-[16px] outline-none placeholder:text-muted"
              />
              {text && (
                <button type="button" onClick={() => setText("")} aria-label={t("Sil")} className="text-muted">
                  <X className="size-5" />
                </button>
              )}
            </div>
          </form>
          <div className="min-h-0 flex-1 overflow-y-auto overscroll-contain">{list}</div>
        </div>
      )}
    </div>
  );
}
