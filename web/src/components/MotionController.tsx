"use client";

import { useEffect } from "react";

/** How far (in degrees) a card may lean. */
const MAX = 5;

/**
 * One listener for the whole site: a card marked `data-tilt` leans a little towards the mouse and settles when it
 * leaves. Touch screens and the "reduce motion" setting do nothing here (the CSS also keeps it still then).
 */
export function MotionController() {
  useEffect(() => {
    const fine = window.matchMedia("(hover: hover) and (pointer: fine)");
    const calm = window.matchMedia("(prefers-reduced-motion: reduce)");
    let current: HTMLElement | null = null;
    let frame = 0;

    const rest = (el: HTMLElement | null) => {
      if (!el) return;
      el.removeAttribute("data-tilting");
      el.style.removeProperty("--rx");
      el.style.removeProperty("--ry");
    };

    const onMove = (e: PointerEvent) => {
      if (!fine.matches || calm.matches || e.pointerType !== "mouse") return;
      const root = document.documentElement.dataset.motion;
      if (root === "soft" || root === "off") return;
      const el = (e.target as Element | null)?.closest<HTMLElement>("[data-tilt]") ?? null;
      if (el !== current) {
        rest(current);
        current = el;
      }
      if (!el) return;
      const { clientX, clientY } = e;
      if (frame) return;
      frame = requestAnimationFrame(() => {
        frame = 0;
        if (!current) return;
        const box = current.getBoundingClientRect();
        const x = (clientX - box.left) / box.width - 0.5;
        const y = (clientY - box.top) / box.height - 0.5;
        current.setAttribute("data-tilting", "");
        current.style.setProperty("--ry", `${(x * 2 * MAX).toFixed(2)}deg`);
        current.style.setProperty("--rx", `${(-y * 2 * MAX).toFixed(2)}deg`);
      });
    };

    const onLeave = () => {
      rest(current);
      current = null;
    };

    document.addEventListener("pointermove", onMove, { passive: true });
    document.addEventListener("pointerleave", onLeave);
    window.addEventListener("blur", onLeave);
    return () => {
      document.removeEventListener("pointermove", onMove);
      document.removeEventListener("pointerleave", onLeave);
      window.removeEventListener("blur", onLeave);
      if (frame) cancelAnimationFrame(frame);
    };
  }, []);

  return null;
}
