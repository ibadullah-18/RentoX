import { afterEach, describe, expect, it, vi } from "vitest";
import { checkUrl, hhmm, openNow, toMinutes, todayNumber, whatsappLink } from "./store-contact";

const t = (text: string) => text;

describe("store hours", () => {
  afterEach(() => vi.useRealTimers());

  it("turns minutes into a clock and back", () => {
    expect(hhmm(545)).toBe("09:05");
    expect(hhmm(0)).toBe("00:00");
    expect(toMinutes("09:30")).toBe(570);
    expect(toMinutes("9:05")).toBe(545);
    expect(toMinutes("25:00")).toBeNull();
    expect(toMinutes("10:75")).toBeNull();
    expect(toMinutes("abc")).toBeNull();
  });

  it("is open inside the hours of the day in Baku time, closed outside them", () => {
    // Wednesday 14 October 2026, 10:00 in Baku = 06:00 UTC.
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-10-14T06:00:00Z"));
    const hours = [{ day: 3, openMinute: 540, closeMinute: 1080 }];
    expect(todayNumber()).toBe(3);
    expect(openNow(hours)).toBe(true);

    vi.setSystemTime(new Date("2026-10-14T04:30:00Z")); // 08:30, not yet open
    expect(openNow(hours)).toBe(false);
    vi.setSystemTime(new Date("2026-10-14T14:00:00Z")); // 18:00: the closing minute itself is closed
    expect(openNow(hours)).toBe(false);
    vi.setSystemTime(new Date("2026-10-15T06:00:00Z")); // Thursday: not listed
    expect(openNow(hours)).toBe(false);
  });

  it("says nothing when no hours are given", () => {
    expect(openNow([])).toBeNull();
    expect(openNow(null)).toBeNull();
  });
});

describe("store links", () => {
  it("builds the WhatsApp link with the first message", () => {
    expect(whatsappLink("+994 50 111 22 33")).toBe("https://wa.me/994501112233");
    expect(whatsappLink("994501112233", "Salam! Ünvan?")).toBe(`https://wa.me/994501112233?text=${encodeURIComponent("Salam! Ünvan?")}`);
  });

  it("accepts only http(s) links of a sane length", () => {
    expect(checkUrl("", t)).toBeNull();
    expect(checkUrl("https://instagram.com/shop", t)).toBeNull();
    expect(checkUrl("ftp://x.az", t)).not.toBeNull();
    expect(checkUrl("javascript:alert(1)", t)).not.toBeNull();
    expect(checkUrl("not a link", t)).not.toBeNull();
    expect(checkUrl(`https://x.az/${"a".repeat(300)}`, t)).not.toBeNull();
  });
});
