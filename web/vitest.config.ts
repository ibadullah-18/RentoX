import { fileURLToPath } from "node:url";
import { defineConfig } from "vitest/config";

// Tests of the plain logic (dates, phone numbers, store hours, search addresses). They run in Node, no browser.
export default defineConfig({
  resolve: { alias: { "@": fileURLToPath(new URL("./src", import.meta.url)) } },
  test: { include: ["src/**/*.test.ts"], environment: "node" },
});
