import { describe, expect, it } from "vitest";
import { isUntidy, parseSearch, toQuery } from "./search-params";

describe("search addresses", () => {
  it("reads what the filter form sent", () => {
    const p = parseSearch({ q: "  mashin ", category: "c1", minPrice: "10", page: "3", o_brand: ["a", "b", ""] });
    expect(p.q).toBe("mashin");
    expect(p.category).toBe("c1");
    expect(p.minPrice).toBe("10");
    expect(p.page).toBe(3);
    expect(p.fieldParams).toEqual([
      ["o_brand", "a"],
      ["o_brand", "b"],
    ]);
  });

  it("never lets the page fall below 1", () => {
    expect(parseSearch({ page: "0" }).page).toBe(1);
    expect(parseSearch({ page: "-4" }).page).toBe(1);
    expect(parseSearch({ page: "abc" }).page).toBe(1);
    expect(parseSearch({}).page).toBe(1);
  });

  it("calls an address untidy when it carries defaults or empty values", () => {
    expect(isUntidy({ seller: "all" })).toBe(true);
    expect(isUntidy({ sort: "date" })).toBe(true);
    expect(isUntidy({ q: "" })).toBe(true);
    expect(isUntidy({ q: "x", sort: "price" })).toBe(false);
  });

  it("builds a tidy query without defaults", () => {
    const p = parseSearch({ q: "ev", sort: "date", seller: "all", category: "c1" });
    expect(toQuery(p).toString()).toBe("q=ev&category=c1");
    expect(toQuery(p, { page: "2" }).get("page")).toBe("2");
  });
});
