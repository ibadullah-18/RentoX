"use client";

import { useState } from "react";
import { User } from "lucide-react";

/** The person's profile photo, or a soft placeholder when there is none. */
export function UserAvatar({ userId, size = 48, version = 0 }: { userId: string; size?: number; version?: number }) {
  const [broken, setBroken] = useState(false);

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
