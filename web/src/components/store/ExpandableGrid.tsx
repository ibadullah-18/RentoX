"use client";

import { Children, useState } from "react";

/**
 * A grid of cards that shows the first few and a "show all / show less" button for the rest. The cards are made
 * on the server and passed in; this only decides how many are visible.
 */
export function ExpandableGrid({
  first,
  showAll,
  showLess,
  children,
}: {
  first: number;
  showAll: string;
  showLess: string;
  children: React.ReactNode;
}) {
  const [open, setOpen] = useState(false);
  const items = Children.toArray(children);

  return (
    <>
      <ul className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:gap-4 lg:grid-cols-4 xl:grid-cols-5">{open ? items : items.slice(0, first)}</ul>
      {items.length > first && (
        <button
          type="button"
          onClick={() => setOpen((v) => !v)}
          className="mx-auto mt-4 flex h-11 items-center rounded-xl border border-line px-6 font-bold hover:border-primary hover:text-primary"
        >
          {open ? showLess : showAll}
        </button>
      )}
    </>
  );
}
