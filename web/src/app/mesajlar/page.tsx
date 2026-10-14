import { getT } from "@/lib/i18n/server";
import { MessageCircle } from "lucide-react";

/** The empty right-hand side on a computer; on a phone the list fills the screen. */
export default async function MessagesHome() {
  const t = await getT();
  return (
    <div className="flex flex-1 flex-col items-center justify-center gap-2 p-8 text-center">
      <MessageCircle className="size-12 text-primary" aria-hidden />
      <h2 className="text-lg font-extrabold">{t("Söhbət seçin")}</h2>
      <p className="max-w-xs text-ink-2">{t("Soldan söhbət seçin və ya elan sahibinə yazın.")}</p>
    </div>
  );
}
