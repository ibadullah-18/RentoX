"use client";

import { useT } from "@/lib/i18n/client";
import { useTransition } from "react";
import { usePathname, useRouter } from "next/navigation";
import { Heart } from "lucide-react";
import { setFavorite } from "@/app/secilmisler/actions";
import { useFavorites } from "./FavoritesProvider";

/** The heart on a card. Signed out, it leads to the login page first. */
export function FavoriteButton({
  listingId,
  className = "",
  plain = false,
}: {
  listingId: string;
  className?: string;
  /** A small round button with a plain heart (on top of a photo), not the card's. */
  plain?: boolean;
}) {
  const favorites = useFavorites();
  const router = useRouter();
  const pathname = usePathname();
  const t = useT();
  const [pending, start] = useTransition();
  const on = favorites.has(listingId);

  function toggle() {
    if (!favorites.signedIn) {
      router.push(`/giris?next=${encodeURIComponent(pathname)}`);
      return;
    }
    const next = !on;
    favorites.set(listingId, next); // shown at once
    start(async () => {
      const result = await setFavorite(listingId, next);
      if (!result.ok) {
        favorites.set(listingId, !next); // did not happen: put it back
        if (result.unauthorized) router.push(`/giris?next=${encodeURIComponent(pathname)}`);
      }
    });
  }

  return (
    <button
      type="button"
      onClick={toggle}
      disabled={pending}
      aria-pressed={on}
      aria-label={on ? t("Seçilmişlərdən çıxar") : t("Seçilmişlərə əlavə et")}
      className={`${plain ? "" : "flex size-10 items-center justify-center rounded-full text-white drop-shadow"} transition-transform active:scale-90 ${className}`}
    >
      <Heart
        className={`${plain ? "size-[18px]" : "size-6"} ${on ? "fill-accent text-accent" : plain ? "" : "fill-black/25"}`}
        aria-hidden
      />
    </button>
  );
}
