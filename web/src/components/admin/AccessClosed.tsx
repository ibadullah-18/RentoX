import { Lock } from "lucide-react";
import { adminLogout } from "@/app/admin/giris/actions";

/** Shown instead of the panel when the owner closed this person's access. */
export function AccessClosed() {
  return (
    <div className="flex min-h-dvh items-center justify-center bg-bg px-4">
      <div className="w-full max-w-md space-y-4 rounded-2xl border border-line bg-surface p-8 text-center">
        <Lock className="mx-auto size-12 text-danger" aria-hidden />
        <h1 className="text-xl font-extrabold">Panelə giriş icazəniz bağlıdır</h1>
        <p className="text-ink-2">
          Rəhbərlik bu hesab üçün admin panelə girişi müvəqqəti bağlayıb. İcazə yenidən açılanda eyni nömrə ilə daxil ola bilərsiniz. Sual olarsa, rəhbərlə
          əlaqə saxlayın.
        </p>
        <form action={adminLogout}>
          <button type="submit" className="h-11 rounded-xl bg-primary px-8 font-bold text-white hover:bg-primary-press">
            Çıxış
          </button>
        </form>
      </div>
    </div>
  );
}
