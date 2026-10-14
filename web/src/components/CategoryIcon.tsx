"use client";

import { useState } from "react";
import { categoryGlyph } from "@/lib/category-icon";

export const isIconLink = (value?: string | null) => /^https?:\/\//i.test(value?.trim() ?? "");

/**
 * A category's icon: the picture an admin linked (a web address), else the
 * icon they chose from the list, else one guessed from the slug. A picture
 * that cannot be loaded falls back to the guessed icon.
 */
export function CategoryIcon({
  slug,
  iconUrl,
  className = "",
}: {
  slug: string;
  iconUrl?: string | null;
  className?: string;
}) {
  const [broken, setBroken] = useState(false);

  if (isIconLink(iconUrl) && !broken) {
    // eslint-disable-next-line @next/next/no-img-element
    return <img src={iconUrl!.trim()} alt="" onError={() => setBroken(true)} className={`object-contain ${className}`} />;
  }
  return categoryGlyph(slug, isIconLink(iconUrl) ? null : iconUrl, className);
}
