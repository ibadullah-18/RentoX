"use client";

import { usePathname } from "next/navigation";
import { useState } from "react";
import { isChatRoute } from "@/components/ChromeGate";
import { ADMIN_SEGMENT } from "@/lib/admin-path";

/**
 * A page that was opened by going back (the arrow, the swipe or the browser's back) keeps its place and does not
 * replay its entrance: only a quick fade. Every other opening is the full entrance (see `.rx-page` in the CSS).
 */
let wentBack = false;
if (typeof window !== "undefined") {
  window.addEventListener("popstate", () => {
    wentBack = true;
  });
}

function takeBack(): boolean {
  const was = wentBack;
  wentBack = false;
  return was;
}

export default function Template({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  // Read once, when this opening starts (a template is made again for every page that opens).
  const [nav] = useState(() => (takeBack() ? "back" : "push"));
  // A chat window fills the screen and has its own layout, and the admin panel is a work tool: neither is animated.
  if (isChatRoute(pathname) || pathname === `/${ADMIN_SEGMENT}` || pathname.startsWith(`/${ADMIN_SEGMENT}/`) || pathname.startsWith("/admin")) {
    return <>{children}</>;
  }
  return (
    <div className="rx-page" data-nav={nav}>
      {children}
    </div>
  );
}
