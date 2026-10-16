import { describe, expect, it } from "vitest";
import { bakuParts, fmtDate, fmtDateTime, fmtMonth, fmtNumeric, fmtTime } from "./dates";

describe("dates (Baku time, written by hand)", () => {
  // 14 October 2026, 21:30 UTC is already 15 October, 01:30 in Baku (UTC+4).
  const late = "2026-10-14T21:30:00Z";

  it("uses the Baku clock, not the machine's", () => {
    expect(bakuParts(late)).toMatchObject({ year: 2026, month: 10, day: 15, hour: 1, minute: 30 });
  });

  it("writes the Azerbaijani month names in full and short (never 'M10')", () => {
    expect(fmtDate(late, "az")).toBe("15 oktyabr 2026");
    expect(fmtDate(late, "az", { short: true, year: false })).toBe("15 okt");
    expect(fmtDate("2026-01-05T08:00:00Z", "az")).toBe("5 yanvar 2026");
    expect(fmtDate("2026-12-05T08:00:00Z", "az")).toBe("5 dekabr 2026");
  });

  it("writes English and Russian months too", () => {
    expect(fmtDate(late, "en")).toBe("15 October 2026");
    expect(fmtDate(late, "ru")).toBe("15 октября 2026");
  });

  it("writes times and combined forms", () => {
    expect(fmtTime(late)).toBe("01:30");
    expect(fmtDateTime(late, "az")).toBe("15 oktyabr, 01:30");
    expect(fmtNumeric(late)).toBe("15.10.2026 01:30");
    expect(fmtNumeric(late, false)).toBe("15.10.2026");
  });

  it("names a month from 'YYYY-MM'", () => {
    expect(fmtMonth("2026-10")).toBe("oktyabr 2026");
  });

  it("does not break on nothing or on garbage", () => {
    expect(fmtNumeric(null)).toBe("—");
    expect(fmtNumeric(undefined)).toBe("—");
    expect(fmtDate("not a date")).toBe("—");
    expect(fmtTime("")).toBe("—");
  });
});
