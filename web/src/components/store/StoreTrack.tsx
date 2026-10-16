"use client";

import { useEffect } from "react";

export type StoreEventKind = "view" | "call" | "whatsapp" | "message";

/** Tells the site that something happened on the store page. Fire and forget: it never gets in the way. */
export function reportStoreEvent(slug: string, kind: StoreEventKind) {
  try {
    void fetch("/bff/store-event", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ slug, kind }),
      keepalive: true,
    }).catch(() => {});
  } catch {
    // Nothing to do.
  }
}

/** Counts one view when the page is opened (once per visit to that store in this tab). */
export function StoreView({ slug }: { slug: string }) {
  useEffect(() => {
    const key = `rx_store_seen_${slug}`;
    try {
      if (sessionStorage.getItem(key)) return;
      sessionStorage.setItem(key, "1");
    } catch {
      // Without storage it simply counts each time.
    }
    reportStoreEvent(slug, "view");
  }, [slug]);

  return null;
}
