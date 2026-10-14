"use client";

import { createContext, useContext, useMemo, useState } from "react";

type Favorites = {
  signedIn: boolean;
  has: (id: string) => boolean;
  /** Changes the local state at once; the caller then tells the server. */
  set: (id: string, on: boolean) => void;
};

const Context = createContext<Favorites>({
  signedIn: false,
  has: () => false,
  set: () => {},
});

/**
 * The listings this person has favourited, known to every heart on the page.
 * Cards stay cached and shared; only this set is personal.
 */
export function FavoritesProvider({
  initialIds,
  signedIn,
  children,
}: {
  initialIds: string[];
  signedIn: boolean;
  children: React.ReactNode;
}) {
  const [ids, setIds] = useState(() => new Set(initialIds));
  // A new server answer (after signing in, for example) replaces the set.
  const [seen, setSeen] = useState(initialIds);
  if (seen !== initialIds) {
    setSeen(initialIds);
    setIds(new Set(initialIds));
  }

  const value = useMemo<Favorites>(
    () => ({
      signedIn,
      has: (id) => ids.has(id),
      set: (id, on) =>
        setIds((current) => {
          const next = new Set(current);
          if (on) next.add(id);
          else next.delete(id);
          return next;
        }),
    }),
    [ids, signedIn],
  );

  return <Context.Provider value={value}>{children}</Context.Provider>;
}

export const useFavorites = () => useContext(Context);
