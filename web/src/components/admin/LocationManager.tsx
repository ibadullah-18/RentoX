"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { Loader2, Pencil, Plus } from "lucide-react";
import { createCity, createDistrict, updateCity, updateDistrict, type LocationInput, type LocationSaved } from "@/app/admin/(panel)/location-actions";
import { adminHref } from "@/lib/admin-path";
import { Pill } from "@/components/admin/ui";

export type DistrictRow = { id: string; nameAz: string; nameRu: string; nameEn: string; displayOrder: number; isActive: boolean; listingCount: number };
export type CityRow = DistrictRow & { districts: DistrictRow[] };

const field = "h-9 w-full rounded-lg border border-line bg-surface px-2.5 text-sm outline-none focus:border-primary disabled:opacity-60";

type FormValue = { nameAz: string; nameRu: string; nameEn: string; displayOrder: string; isActive: boolean };

/** Three names and an order; when editing, also whether the place is offered. */
function PlaceForm({
  initial,
  editing,
  submit,
  onDone,
  onCancel,
  label,
}: {
  initial: FormValue;
  editing: boolean;
  submit: (input: LocationInput) => Promise<LocationSaved>;
  onDone: () => void;
  onCancel: () => void;
  label: string;
}) {
  const router = useRouter();
  const [v, setV] = useState(initial);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const set = (patch: Partial<FormValue>) => setV((x) => ({ ...x, ...patch }));

  async function save() {
    setBusy(true);
    setError(null);
    const res = await submit({
      nameAz: v.nameAz,
      nameRu: v.nameRu,
      nameEn: v.nameEn,
      displayOrder: Number(v.displayOrder),
      isActive: editing ? v.isActive : undefined,
    });
    setBusy(false);
    if (res.ok) {
      onDone();
      router.refresh();
    } else if (res.unauthorized) router.push(adminHref("/giris"));
    else setError(res.message);
  }

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        void save();
      }}
      className="space-y-3 rounded-lg bg-bg p-3"
    >
      <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-4">
        <input aria-label="Azərbaycanca ad" placeholder="Ad (AZ)" value={v.nameAz} onChange={(e) => set({ nameAz: e.target.value })} disabled={busy} className={field} />
        <input aria-label="Rusca ad" placeholder="Название (RU)" value={v.nameRu} onChange={(e) => set({ nameRu: e.target.value })} disabled={busy} className={field} />
        <input aria-label="İngiliscə ad" placeholder="Name (EN)" value={v.nameEn} onChange={(e) => set({ nameEn: e.target.value })} disabled={busy} className={field} />
        <input aria-label="Sıra" placeholder="Sıra" inputMode="numeric" value={v.displayOrder} onChange={(e) => set({ displayOrder: e.target.value })} disabled={busy} className={field} />
      </div>
      {editing && (
        <label className="flex items-center gap-2 text-sm font-semibold">
          <input type="checkbox" checked={v.isActive} onChange={(e) => set({ isActive: e.target.checked })} disabled={busy} className="size-4 accent-[var(--color-primary)]" />
          Göstərilir (söndürülsə, yeni elanlarda seçilə bilməz)
        </label>
      )}
      <div className="flex flex-wrap items-center gap-2">
        <button type="submit" disabled={busy} className="flex h-9 items-center gap-2 rounded-lg bg-primary px-4 text-sm font-bold text-white hover:bg-primary-press disabled:opacity-60">
          {busy && <Loader2 className="size-4 animate-spin" aria-hidden />}
          {label}
        </button>
        <button type="button" onClick={onCancel} disabled={busy} className="h-9 rounded-lg px-3 text-sm font-semibold text-ink-2 hover:bg-primary-soft">
          Ləğv et
        </button>
        {error && (
          <p role="alert" className="text-sm font-semibold text-danger">
            {error}
          </p>
        )}
      </div>
    </form>
  );
}

const fromRow = (r: DistrictRow): FormValue => ({
  nameAz: r.nameAz,
  nameRu: r.nameRu,
  nameEn: r.nameEn,
  displayOrder: String(r.displayOrder),
  isActive: r.isActive,
});

const blank = (order: number): FormValue => ({ nameAz: "", nameRu: "", nameEn: "", displayOrder: String(order), isActive: true });

/** All cities with their districts; add, rename, re-order and switch places on or off. */
export function LocationManager({ cities }: { cities: CityRow[] }) {
  // Which form is open: "city:new", "city:<id>", "district:new:<cityId>" or "district:<id>".
  const [open, setOpen] = useState<string | null>(null);
  const close = () => setOpen(null);
  const nextOrder = (rows: { displayOrder: number }[]) => (rows.length ? Math.max(...rows.map((r) => r.displayOrder)) + 1 : 1);

  return (
    <div className="space-y-4">
      {open === "city:new" ? (
        <PlaceForm initial={blank(nextOrder(cities))} editing={false} submit={createCity} onDone={close} onCancel={close} label="Şəhəri əlavə et" />
      ) : (
        <button type="button" onClick={() => setOpen("city:new")} className="flex h-10 items-center gap-2 rounded-lg bg-primary px-4 text-sm font-bold text-white hover:bg-primary-press">
          <Plus className="size-4" aria-hidden />
          Yeni şəhər
        </button>
      )}

      {cities.map((c) => (
        <section key={c.id} className="rounded-xl border border-line bg-surface">
          <div className="flex flex-wrap items-center gap-2 border-b border-line px-4 py-3">
            <h2 className={`text-[15px] font-extrabold ${c.isActive ? "" : "text-ink-2 line-through"}`}>{c.nameAz}</h2>
            <span className="text-xs text-ink-2">
              {c.nameRu} · {c.nameEn} · sıra {c.displayOrder} · {c.listingCount} elan
            </span>
            {!c.isActive && <Pill tone="muted">Söndürülüb</Pill>}
            <button type="button" onClick={() => setOpen(open === `city:${c.id}` ? null : `city:${c.id}`)} className="ml-auto flex h-8 items-center gap-1 rounded-lg px-2 text-sm font-bold text-primary hover:bg-primary-soft">
              <Pencil className="size-4" aria-hidden />
              Dəyiş
            </button>
          </div>

          {open === `city:${c.id}` && (
            <div className="border-b border-line p-3">
              <PlaceForm initial={fromRow(c)} editing submit={(i) => updateCity(c.id, i)} onDone={close} onCancel={close} label="Yadda saxla" />
            </div>
          )}

          <ul className="divide-y divide-line">
            {c.districts.length === 0 && <li className="px-4 py-3 text-sm text-ink-2">Rayon yoxdur.</li>}
            {c.districts.map((d) => (
              <li key={d.id} className="px-4 py-2.5">
                <div className="flex flex-wrap items-center gap-2">
                  <span className={`font-semibold ${d.isActive ? "" : "text-ink-2 line-through"}`}>{d.nameAz}</span>
                  <span className="text-xs text-ink-2">
                    {d.nameRu} · {d.nameEn} · sıra {d.displayOrder} · {d.listingCount} elan
                  </span>
                  {!d.isActive && <Pill tone="muted">Söndürülüb</Pill>}
                  <button type="button" onClick={() => setOpen(open === `district:${d.id}` ? null : `district:${d.id}`)} className="ml-auto flex h-8 items-center gap-1 rounded-lg px-2 text-sm font-bold text-primary hover:bg-primary-soft">
                    <Pencil className="size-4" aria-hidden />
                    Dəyiş
                  </button>
                </div>
                {open === `district:${d.id}` && (
                  <div className="mt-2">
                    <PlaceForm initial={fromRow(d)} editing submit={(i) => updateDistrict(d.id, i)} onDone={close} onCancel={close} label="Yadda saxla" />
                  </div>
                )}
              </li>
            ))}
          </ul>

          <div className="border-t border-line p-3">
            {open === `district:new:${c.id}` ? (
              <PlaceForm initial={blank(nextOrder(c.districts))} editing={false} submit={(i) => createDistrict(c.id, i)} onDone={close} onCancel={close} label="Rayonu əlavə et" />
            ) : (
              <button type="button" onClick={() => setOpen(`district:new:${c.id}`)} className="flex h-8 items-center gap-1 text-sm font-bold text-primary hover:underline">
                <Plus className="size-4" aria-hidden />
                Rayon əlavə et
              </button>
            )}
          </div>
        </section>
      ))}
    </div>
  );
}
