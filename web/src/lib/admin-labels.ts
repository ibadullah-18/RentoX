// Labels the admin screens share (also used by client components).
export const reportReason: Record<number, string> = {
  1: "Spam",
  2: "Fırıldaq",
  3: "Qadağan olunmuş məzmun",
  4: "Yanıldıcı",
  5: "Yanlış kateqoriya",
  6: "Digər",
};

export const reportStatus: Record<number, string> = {
  1: "Gözləyir",
  2: "Baxılır",
  3: "Həll olundu",
  4: "Rədd edildi",
};

export const priorityLabel: Record<number, string> = { 1: "Aşağı", 2: "Normal", 3: "Yüksək", 4: "Təcili" };

export const chatReason: Record<number, string> = {
  1: "Spam",
  2: "Fırıldaq",
  3: "Təhqir / təqib",
  4: "Qadağan olunmuş məzmun",
  5: "Digər",
};

export const walletType: Record<number, string> = {
  1: "Balans artırılması",
  2: "Elan aktivləşdirmə haqqı",
  3: "İrəli çəkmə / VIP",
  4: "Geri qaytarma",
  5: "Bonus",
  6: "Admin düzəlişi",
};

export const walletDirection: Record<number, string> = { 1: "Daxilolma", 2: "Çıxılma" };

export const ticketStatusLabel: Record<number, string> = { 1: "Açıq", 2: "İşlənir", 3: "Həll olundu", 4: "Bağlı" };
export const listingStatusLabel: Record<number, string> = {
  1: "Qaralama",
  2: "Yoxlamada",
  3: "Aktiv",
  4: "Rədd edilib",
  5: "Müddəti bitib",
  6: "Dayandırılıb",
  7: "Silinib",
  8: "Ödəniş gözləyir",
};
export const storeStatusLabel: Record<number, string> = {
  1: "Qaralama",
  2: "Yoxlamada",
  3: "Aktiv",
  4: "Rədd edilib",
  5: "Dayandırılıb",
  6: "Silinib",
};

/** What each audit action is called, and how to read its before/after number. */
const sectionNames = ["Elanlar", "Mağazalar", "Şikayətlər", "Dəstək", "Kataloq", "İstifadəçilər", "Maliyyə", "Kadr"];

/** A set of panel sections written as bits → "Dəstək, Şikayətlər" (all of them: "Hamısı"). */
export function sectionsFromMask(mask: number): string {
  // Every ordinary section (without the staff one) is what "no section chosen" means.
  if (mask === (1 << (sectionNames.length - 1)) - 1) return "Hamısı";
  if (mask === (1 << sectionNames.length) - 1) return "Hamısı və Kadr";
  return sectionNames.filter((_, i) => (mask & (1 << i)) !== 0).join(", ") || "—";
}

export const auditAction: Record<
  number,
  { label: string; value?: Record<number, string>; format?: (n: number) => string; target: string }
> = {
  1: { label: "Elan statusu dəyişdi", value: listingStatusLabel, target: "Elan" },
  2: { label: "Mağaza statusu dəyişdi", value: storeStatusLabel, target: "Mağaza" },
  3: { label: "Dəstək statusu dəyişdi", value: ticketStatusLabel, target: "Müraciət" },
  4: { label: "Dəstək prioriteti dəyişdi", value: priorityLabel, target: "Müraciət" },
  5: { label: "Dəstək cavabı əlavə edildi", target: "Müraciət" },
  7: { label: "İstifadəçi statusu dəyişdi", value: { 1: "Aktiv", 2: "Dayandırılıb", 3: "Bloklanıb", 4: "Silinib" }, target: "İstifadəçi" },
  8: { label: "Admin bölmələri dəyişdi", format: sectionsFromMask, target: "İstifadəçi" },
  11: { label: "Pul kisəsi əllə dəyişdirildi", format: (n) => `${n > 0 ? "+" : "−"}${(Math.abs(n) / 100).toFixed(2)} AZN`, target: "İstifadəçi" },
  10: { label: "İşçinin sessiyaları bağlandı", format: (n) => `${n} sessiya`, target: "İstifadəçi" },
  9: { label: "Bildiriş göndərildi", format: (n) => `${n} nəfər`, target: "Mesaj" },
  6: { label: "Admin icazəsi dəyişdi", value: { 0: "Yoxdur", 1: "Admin" }, target: "İstifadəçi" },
  12: {
    label: "İşçi ilə bağlı dəyişiklik",
    value: { 1: "Profil və ya maaş", 2: "Panelə giriş icazəsi", 3: "Maaş sətri əlavə edildi", 4: "Maaş sətri ləğv edildi", 5: "İcazə üzrə qərar", 6: "Müraciət üzrə qərar" },
    target: "İşçi",
  },
};
