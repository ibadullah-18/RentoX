"use client";

import { usePathname } from "next/navigation";
import { useEffect } from "react";

/** The messenger-like pages: a conversation, a support request. They fill the screen: no footer, no padding. */
export function isChatRoute(pathname: string): boolean {
  return /^\/mesajlar(\/|$)/.test(pathname) || /^\/destek\/(?!yeni(\/|$))[^/]+/.test(pathname);
}

/** A conversation or a request is open (not just a list): on a phone the bottom bar steps aside. */
export function isChatOpen(pathname: string): boolean {
  return /^\/mesajlar\/[^/]+/.test(pathname) || /^\/destek\/(?!yeni(\/|$))[^/]+/.test(pathname);
}

/** The footer is for reading pages, not for a chat window. */
export function FooterGate({ children }: { children: React.ReactNode }) {
  return isChatRoute(usePathname()) ? null : children;
}

/**
 * The frame around the page. A chat page gets the whole height under the header (the header's real height is
 * measured into `--rx-header`, so a chat window can be exactly as tall as what is left).
 */
export function MainFrame({ children }: { children: React.ReactNode }) {
  const chat = isChatRoute(usePathname());

  useEffect(() => {
    const header = document.querySelector("body > header, body header");
    if (!header) return;
    const set = () => document.documentElement.style.setProperty("--rx-header", `${header.getBoundingClientRect().height}px`);
    set();
    const observer = new ResizeObserver(set);
    observer.observe(header);
    return () => observer.disconnect();
  }, []);

  return (
    <main
      className={
        chat
          ? "mx-auto w-full max-w-[1240px] flex-1 px-0 py-0 md:px-6 md:py-5"
          : "mx-auto w-full max-w-[1240px] flex-1 px-4 py-5 md:px-6 md:py-8"
      }
    >
      {children}
    </main>
  );
}
