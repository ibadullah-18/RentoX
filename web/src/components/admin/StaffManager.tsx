"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { Loader2, LogOut, ShieldCheck, UserMinus, UserPlus } from "lucide-react";
import { endSessions, grantAdmin, revokeAdmin, setSections } from "@/app/admin/(panel)/staff-actions";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { Pill } from "@/components/admin/ui";
import { PANEL_SECTIONS, type StaffMember } from "@/lib/admin-catalog";
import { formatNational } from "@/lib/phone";

const input =
  "h-10 w-full rounded-lg border border-line bg-surface px-3 text-[15px] outline-none focus:border-primary disabled:opacity-60";

/** The people with admin access, a way to add one by phone, and to take access away. */
export function StaffManager({ members, me }: { members: StaffMember[]; me: string }) {
  const router = useRouter();
  const [phone, setPhone] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [asking, setAsking] = useState<StaffMember | null>(null);
  const [signingOut, setSigningOut] = useState<StaffMember | null>(null);
  const [note, setNote] = useState<string | null>(null);

  async function add() {
    if (busy) return;
    setBusy(true);
    setError(null);
    const res = await grantAdmin(phone);
    setBusy(false);
    if (res.ok) {
      setPhone("");
      router.refresh();
    } else setError(res.unauthorized ? "İcazə yoxdur." : res.message);
  }

  async function saveSections(member: StaffMember, sections: string[]) {
    setBusy(true);
    setError(null);
    const res = await setSections(member.userId, sections);
    setBusy(false);
    if (res.ok) router.refresh();
    else setError(res.unauthorized ? "İcazə yoxdur." : res.message);
  }

  async function signOut(member: StaffMember) {
    setBusy(true);
    setError(null);
    setNote(null);
    const res = await endSessions(member.userId);
    setBusy(false);
    setSigningOut(null);
    if (res.ok) {
      setNote(`${member.fullName}: ${res.ended ?? 0} sessiya bağlandı. Yenidən daxil olmalıdır.`);
      router.refresh();
    } else setError(res.unauthorized ? "İcazə yoxdur." : res.message);
  }

  async function remove(member: StaffMember) {
    setBusy(true);
    setError(null);
    const res = await revokeAdmin(member.userId);
    setBusy(false);
    setAsking(null);
    if (res.ok) router.refresh();
    else setError(res.unauthorized ? "İcazə yoxdur." : res.message);
  }

  return (
    <div className="space-y-5">
      <form
        onSubmit={(e) => {
          e.preventDefault();
          void add();
        }}
        className="space-y-3 rounded-xl border border-line bg-surface p-4"
      >
        <p className="text-sm font-bold">Admin icazəsi ver</p>
        <p className="text-xs text-ink-2">
          Şəxs saytda qeydiyyatdan keçmiş olmalıdır. Yeni icazə şəxsin növbəti girişində (və ya 15 dəqiqəyə qədər) qüvvəyə minir.
        </p>
        <div className="flex gap-2">
          <span className="flex h-10 items-center rounded-lg border border-line bg-surface px-3 text-sm font-semibold text-ink-2">+994</span>
          <input
            value={phone}
            onChange={(e) => setPhone(formatNational(e.target.value))}
            inputMode="numeric"
            placeholder="50 123 45 67"
            disabled={busy}
            className={input}
          />
          <button
            type="submit"
            disabled={busy}
            className="flex h-10 shrink-0 items-center gap-2 rounded-lg bg-primary px-4 text-sm font-bold text-white hover:bg-primary-press disabled:opacity-60"
          >
            {busy ? <Loader2 className="size-4 animate-spin" aria-hidden /> : <UserPlus className="size-4" aria-hidden />}
            Əlavə et
          </button>
        </div>
        {error && (
          <p role="alert" className="text-sm font-semibold text-danger">
            {error}
          </p>
        )}
      </form>

      <div className="overflow-hidden rounded-xl border border-line bg-surface">
        <h2 className="border-b border-line px-4 py-3 text-[15px] font-extrabold">İcazəsi olanlar ({members.length})</h2>
        <ul className="divide-y divide-line">
          {members.map((m) => (
            <li key={m.userId} className="flex flex-wrap items-center gap-3 px-4 py-3">
              <ShieldCheck className="size-5 shrink-0 text-primary" aria-hidden />
              <span className="min-w-0 flex-1">
                <span className="block truncate font-semibold">
                  {m.fullName}
                  {m.userId === me && <span className="ml-2 text-xs font-bold text-ink-2">(siz)</span>}
                </span>
                <span className="block text-sm text-ink-2">{m.phone ?? "—"}</span>
                {m.activeSessions !== undefined && (
                  <span className="block text-xs text-ink-2">
                    {m.activeSessions} aktiv sessiya
                    {m.lastActiveAtUtc ? ` · son aktivlik ${new Date(m.lastActiveAtUtc).toLocaleString("az")}` : ""}
                  </span>
                )}
              </span>
              <Pill tone={m.role === "SuperAdmin" ? "primary" : "success"}>{m.role}</Pill>
              {m.role === "Admin" && (
                <SectionsEditor
                  key={`${m.userId}-${m.sections.join(",")}`}
                  member={m}
                  busy={busy}
                  onSave={(sections) => void saveSections(m, sections)}
                />
              )}
              {m.userId !== me && (m.activeSessions ?? 0) > 0 && (
                <button
                  type="button"
                  disabled={busy}
                  onClick={() => setSigningOut(m)}
                  className="flex h-9 items-center gap-1.5 rounded-lg border border-line px-3 text-sm font-semibold hover:border-primary hover:text-primary disabled:opacity-60"
                >
                  <LogOut className="size-4" aria-hidden />
                  Sessiyaları bağla
                </button>
              )}
              {m.role === "Admin" && m.userId !== me && (
                <button
                  type="button"
                  disabled={busy}
                  onClick={() => setAsking(m)}
                  className="flex h-9 items-center gap-1.5 rounded-lg border border-line px-3 text-sm font-semibold text-danger hover:border-danger disabled:opacity-60"
                >
                  <UserMinus className="size-4" aria-hidden />
                  Geri al
                </button>
              )}
            </li>
          ))}
        </ul>
      </div>

      {note && (
        <p role="status" className="rounded-xl bg-success/10 px-4 py-3 text-sm font-semibold text-success">
          {note}
        </p>
      )}

      <ConfirmDialog
        open={signingOut !== null}
        title="Sessiyalar bağlansın?"
        message={`${signingOut?.fullName ?? ""} bütün cihazlardan çıxarılacaq və yenidən daxil olmalı olacaq. Artıq verilmiş giriş bileti ən çoxu 15 dəqiqə qüvvədə qala bilər.`}
        confirmLabel="Bağla"
        destructive
        busy={busy}
        onClose={() => setSigningOut(null)}
        onConfirm={() => signingOut && void signOut(signingOut)}
      />

      <ConfirmDialog
        open={asking !== null}
        title="İcazə geri alınsın?"
        message={`${asking?.fullName ?? ""} artıq admin panelə girə bilməyəcək və bütün cihazlardan çıxarılacaq.`}
        confirmLabel="Geri al"
        destructive
        busy={busy}
        onClose={() => setAsking(null)}
        onConfirm={() => asking && void remove(asking)}
      />
    </div>
  );
}

/** Which sections of the panel one Admin may use (nothing ticked or all ticked = everything). */
function SectionsEditor({
  member,
  busy,
  onSave,
}: {
  member: StaffMember;
  busy: boolean;
  onSave: (sections: string[]) => void;
}) {
  const [open, setOpen] = useState(false);
  const [picked, setPicked] = useState<string[]>(member.sections);
  const all = member.sections.length === 0;

  return (
    <div className="w-full basis-full">
      <button type="button" onClick={() => setOpen((v) => !v)} className="text-sm font-semibold text-primary hover:underline">
        Bölmələr: {all ? "hamısı" : PANEL_SECTIONS.filter((s) => member.sections.includes(s.id)).map((s) => s.label.split(" (")[0]).join(", ")}
      </button>
      {open && (
        <div className="mt-2 space-y-2 rounded-lg border border-line p-3">
          <div className="grid gap-1.5 sm:grid-cols-2">
            {PANEL_SECTIONS.map((s) => (
              <label key={s.id} className="flex cursor-pointer items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={picked.includes(s.id)}
                  disabled={busy}
                  onChange={(e) => setPicked((p) => (e.target.checked ? [...p, s.id] : p.filter((x) => x !== s.id)))}
                  className="size-4 accent-[var(--primary)]"
                />
                {s.label}
              </label>
            ))}
          </div>
          <p className="text-xs text-ink-2">
            Heç nə seçilməsə və ya hamısı seçilsə, hər bölmə açıqdır. Dəyişiklik şəxsin sessiyasını bağlayır, yenidən daxil olmalıdır.
          </p>
          <button
            type="button"
            disabled={busy}
            onClick={() => onSave(picked)}
            className="h-9 rounded-lg bg-primary px-4 text-sm font-bold text-white hover:bg-primary-press disabled:opacity-60"
          >
            Saxla
          </button>
        </div>
      )}
    </div>
  );
}
