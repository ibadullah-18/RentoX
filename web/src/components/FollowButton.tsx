"use client";

import { useLocale, useT } from "@/lib/i18n/client";
import { formatNumber } from "@/lib/format";
import { useState, useTransition } from "react";
import { usePathname, useRouter } from "next/navigation";
import { UserCheck, UserPlus } from "lucide-react";
import { setFollow } from "@/app/store/actions";
import { useFavorites } from "./FavoritesProvider";

/** Follow / unfollow a store. Signed out it leads to login first. */
export function FollowButton({
  storeId,
  slug,
  following: initial,
  followers: initialFollowers,
}: {
  storeId: string;
  slug: string;
  following: boolean;
  followers: number;
}) {
  const t = useT();
  const locale = useLocale();
  const { signedIn } = useFavorites();
  const router = useRouter();
  const pathname = usePathname();
  const [following, setFollowing] = useState(initial);
  const [followers, setFollowers] = useState(initialFollowers);
  const [pending, start] = useTransition();

  function toggle() {
    if (!signedIn) {
      router.push(`/giris?next=${encodeURIComponent(pathname)}`);
      return;
    }
    const next = !following;
    setFollowing(next);
    setFollowers((n) => Math.max(0, n + (next ? 1 : -1)));
    start(async () => {
      const result = await setFollow(storeId, slug, next);
      if (!result.ok) {
        setFollowing(!next);
        setFollowers((n) => Math.max(0, n + (next ? -1 : 1)));
        if (result.unauthorized) router.push(`/giris?next=${encodeURIComponent(pathname)}`);
      }
    });
  }

  return (
    <div className="flex items-center gap-3">
      <button
        type="button"
        onClick={toggle}
        disabled={pending}
        aria-pressed={following}
        className={`flex h-11 items-center gap-2 rounded-xl px-5 font-bold transition-colors ${
          following
            ? "border border-line bg-surface hover:border-danger hover:text-danger"
            : "bg-primary text-white hover:bg-primary-press"
        }`}
      >
        {following ? <UserCheck className="size-5" aria-hidden /> : <UserPlus className="size-5" aria-hidden />}
        {following ? t("İzləyirsiniz") : t("İzlə")}
      </button>
      <span className="text-sm font-semibold text-ink-2">{t("{n} izləyici", { n: formatNumber(followers, locale) })}</span>
    </div>
  );
}
