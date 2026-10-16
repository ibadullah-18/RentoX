import { describe, expect, it } from "vitest";
import { formatNational, isValidPhone, normalizePhone } from "./phone";

describe("phone numbers", () => {
  it("brings every way of writing a number to +994 and nine digits", () => {
    expect(normalizePhone("050 123 45 67")).toBe("+994501234567");
    expect(normalizePhone("+994 50 123 45 67")).toBe("+994501234567");
    expect(normalizePhone("994501234567")).toBe("+994501234567");
    expect(normalizePhone("501234567")).toBe("+994501234567");
  });

  it("accepts nine digits only", () => {
    expect(isValidPhone("050 123 45 67")).toBe(true);
    expect(isValidPhone("050 123 45")).toBe(false);
    expect(isValidPhone("050 123 45 678")).toBe(false);
    expect(isValidPhone("")).toBe(false);
  });

  it("groups the digits while typing", () => {
    expect(formatNational("0501234567")).toBe("50 123 45 67");
    expect(formatNational("+99450123")).toBe("50 123");
    expect(formatNational("5012345678999")).toBe("50 123 45 67");
  });
});
