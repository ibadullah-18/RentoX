"use client";

import { useEffect, useRef, useState } from "react";
import { User } from "lucide-react";

/** The person's profile photo, or a soft placeholder when there is none. */
export function UserAvatar({ userId, size = 48, version = 0 }: { userId: string; size?: number; version?: number }) {
  const [broken, setBroken] = useState(false);
  const image = useRef<HTMLImageElement>(null);

  // The picture may fail (no photo: 404) before the page is interactive, so the error handler never hears of it:
  // look again once mounted, and show the placeholder instead of a broken picture.
  useEffect(() => {
    const el = image.current;
    if (el && el.complete && el.naturalWidth === 0) setBroken(true);
  }, []);

  return (
    <span
      className="flex shrink-0 items-center justify-center overflow-hidden rounded-full bg-primary-soft text-primary"
      style={{ width: size, height: size }}
    >
      {broken ? (
        <User style={{ width: size * 0.52, height: size * 0.52 }} aria-hidden />
      ) : (
        // eslint-disable-next-line @next/next/no-img-element
        <img
          ref={image}
          src={`/api/users/${userId}/profile-image${version ? `?v=${version}` : ""}`}
          alt=""
          width={size}
          height={size}
          className="size-full object-cover"
          onError={() => setBroken(true)}
        />
      )}
    </span>
  );
}
