"use client";

import { ChevronRight } from "lucide-react";
import type { Category } from "@/lib/types";

/**
 * Pick a category level by level until one with nothing inside it: the chosen
 * chain stays above, the next choices below.
 */
export function CategoryPicker({
  tree,
  path,
  disabled,
  onChange,
}: {
  tree: Category[];
  /** Ids from the top category down. */
  path: string[];
  disabled: boolean;
  onChange: (path: string[]) => void;
}) {
  const chain: Category[] = [];
  let level = tree;
  for (const id of path) {
    const found = level.find((c) => c.id === id);
    if (!found) break;
    chain.push(found);
    level = found.children;
  }
  const next = chain.at(-1)?.children ?? tree;

  return (
    <div className="space-y-3">
      {chain.length > 0 && (
        <ol className="flex flex-wrap items-center gap-1 text-sm">
          {chain.map((c, i) => (
            <li key={c.id} className="flex items-center gap-1">
              {i > 0 && <ChevronRight className="size-4 text-ink-2" aria-hidden />}
              <button
                type="button"
                disabled={disabled}
                onClick={() => onChange(path.slice(0, i))}
                className="rounded-lg bg-primary-soft px-3 py-1.5 font-bold text-primary hover:opacity-80"
              >
                {c.name}
              </button>
            </li>
          ))}
        </ol>
      )}

      {next.length > 0 ? (
        <ul className="grid grid-cols-2 gap-2 sm:grid-cols-3">
          {next.map((c) => (
            <li key={c.id}>
              <button
                type="button"
                disabled={disabled}
                onClick={() => onChange([...path.slice(0, chain.length), c.id])}
                className="flex h-14 w-full items-center justify-between gap-2 rounded-xl border border-line bg-surface px-4 text-left font-semibold hover:border-primary"
              >
                <span className="truncate">{c.name}</span>
                {c.children.length > 0 && (
                  <ChevronRight className="size-5 shrink-0 text-ink-2" aria-hidden />
                )}
              </button>
            </li>
          ))}
        </ul>
      ) : null}
    </div>
  );
}
