import { tx } from "./i18n/locales";

export const ticketCategory: Record<number, string> = {
  1: tx("Ümumi"),
  2: tx("Hesab"),
  3: tx("Elan"),
  4: tx("Ödəniş"),
  5: tx("Mağaza"),
  6: tx("Texniki"),
  7: tx("Digər"),
};

export const ticketStatus: Record<number, string> = {
  1: tx("Açıq"),
  2: tx("İşlənir"),
  3: tx("Həll olundu"),
  4: tx("Bağlı"),
};

export const ticketTone: Record<number, string> = {
  1: "bg-primary-soft text-primary",
  2: "bg-warning-soft text-warning",
  3: "bg-success-soft text-success",
  4: "bg-line text-ink-2",
};
