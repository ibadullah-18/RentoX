"use client";

import { Select } from "@/components/ui/Select";
import { useLocale, useT } from "@/lib/i18n/client";
import { unitLabel } from "@/lib/format";
import Image from "next/image";
import { useRouter } from "next/navigation";
import { useEffect, useMemo, useRef, useState } from "react";
import { Loader2, Star, X } from "lucide-react";
import {
  deleteImage,
  setCover,
  submit,
  updateDetails,
  updateFields,
} from "@/app/elanlarim/actions";
import { ownerImage } from "@/lib/format";
import {
  FieldType,
  type City,
  type District,
  type FieldDefinition,
  type ListingField,
  type OwnedDetails,
} from "@/lib/types";
import { FieldInput } from "./FieldInput";
import { PhotoPicker, MAX_PHOTOS, type Photo } from "./PhotoPicker";
import { emptyAnswer, parseNumber, toRequest, validate, type Answer } from "./fields";

const input =
  "h-11 w-full rounded-xl border border-line bg-surface px-4 text-[16px] outline-none focus:border-primary disabled:opacity-60";

/** The answer a saved value stands for. */
function answerOf(field: ListingField | undefined): Answer {
  if (!field) return emptyAnswer;
  return {
    text: field.textValue ?? (field.numericValue != null ? String(field.numericValue) : ""),
    flag: field.flagValue ?? null,
    date: field.calendarValue ? field.calendarValue.slice(0, 10) : "",
    optionIds: (field.selections ?? []).flatMap((s) => (s.optionId ? [s.optionId] : [])),
    custom: field.customValue ?? "",
  };
}

function Problem({ children }: { children?: React.ReactNode }) {
  if (!children) return null;
  return (
    <p role="alert" className="mt-1.5 text-sm font-semibold text-danger">
      {children}
    </p>
  );
}

/** Editing a draft, a rejected or a re-opened listing; saving sends it for review again. */
export function EditListingForm({
  listing,
  fieldDefs,
  cities,
  periods,
}: {
  listing: OwnedDetails;
  fieldDefs: FieldDefinition[];
  cities: City[];
  periods: number[];
}) {
  const t = useT();
  const locale = useLocale();
  const router = useRouter();

  const [title, setTitle] = useState(listing.title);
  const [description, setDescription] = useState(listing.description);
  const [priceText, setPriceText] = useState(String(listing.price));
  const [unitChoice, setUnit] = useState<number>(listing.rentalPeriodUnit);
  const [cityId, setCityId] = useState(listing.cityId ?? "");
  const [districtChoice, setDistrictChoice] = useState<{ cityId: string; id: string } | null>(
    listing.cityId && listing.districtId ? { cityId: listing.cityId, id: listing.districtId } : null,
  );
  const [loadedDistricts, setLoadedDistricts] = useState<{ id: string; list: District[] } | null>(null);

  const [answers, setAnswers] = useState<Record<string, Answer>>(() =>
    Object.fromEntries(
      fieldDefs.map((def) => [def.id, answerOf(listing.fields.find((f) => f.fieldId === def.id))]),
    ),
  );

  // Photos: the saved ones (some marked for removal) and new ones.
  const [removed, setRemoved] = useState<string[]>([]);
  const [cover, setCoverId] = useState<string | null>(null);
  const [added, setAdded] = useState<Photo[]>([]);

  const [showErrors, setShowErrors] = useState(false);
  const [stage, setStage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const uploaded = useRef(0);

  const unit = periods.includes(unitChoice) ? unitChoice : periods[0];
  const city = cities.find((c) => c.id === cityId) ?? null;
  const cityWithDistricts = city?.hasDistricts ? city.id : null;

  useEffect(() => {
    if (!cityWithDistricts) return;
    let cancelled = false;
    fetch(`/api/locations/cities/${cityWithDistricts}/districts?language=${locale}`)
      .then((r) => (r.ok ? (r.json() as Promise<District[]>) : Promise.reject()))
      .then((list) => {
        if (!cancelled) setLoadedDistricts({ id: cityWithDistricts, list });
      })
      .catch(() => {});
    return () => {
      cancelled = true;
    };
  }, [cityWithDistricts, locale]);
  const districts = loadedDistricts?.id === cityWithDistricts ? loadedDistricts.list : [];
  const districtId = districtChoice?.cityId === cityId ? districtChoice.id : "";

  const saved = useMemo(
    () => {
      const kept = [...listing.images]
        .sort((a, b) => a.displayOrder - b.displayOrder)
        .filter((i) => !removed.includes(i.id));
      // The chosen cover goes first.
      return cover ? [...kept.filter((i) => i.id === cover), ...kept.filter((i) => i.id !== cover)] : kept;
    },
    [listing.images, removed, cover],
  );

  // ---- checking -----------------------------------------------------------
  const price = parseNumber(priceText);
  const fieldErrors = Object.fromEntries(
    fieldDefs.flatMap((def) => {
      const e = validate(def, answers[def.id] ?? emptyAnswer);
      return e ? [[def.id, e] as const] : [];
    }),
  );
  const problems = {
    title: title.trim() ? null : t("Başlığı yazın."),
    description: description.trim() ? null : t("Təsviri yazın."),
    price:
      priceText.trim() === ""
        ? unit === 6
          ? null
          : t("Qiyməti yazın.")
        : price === null || price < 0
          ? t("Düzgün qiymət yazın.")
          : null,
    city: cityId ? null : t("Şəhəri seçin."),
    district: city?.hasDistricts && !districtId ? t("Rayonu seçin.") : null,
    photos: saved.length + added.length > 0 ? null : t("Ən azı bir şəkil olmalıdır."),
  };
  const valid = Object.values(problems).every((p) => p === null) && Object.keys(fieldErrors).length === 0;
  const busy = stage !== null;

  async function save() {
    if (busy) return;
    if (!valid) {
      setShowErrors(true);
      return;
    }
    setError(null);

    const fail = (unauthorized: boolean, message: string) => {
      setStage(null);
      if (unauthorized) router.push(`/giris?next=/elanlarim/${listing.id}/redakte`);
      else setError(message);
    };

    try {
      setStage(t("Saxlanılır…"));
      const details = await updateDetails(listing.id, {
        title,
        description,
        price: price ?? 0,
        rentalPeriodUnit: unit,
        cityId,
        districtId: districtId || undefined,
      });
      if (!details.ok) return fail(details.unauthorized, details.message);

      const body = fieldDefs.flatMap((def) => {
        const r = toRequest(def, answers[def.id] ?? emptyAnswer);
        return r ? [r] : [];
      });
      const fields = await updateFields(listing.id, body);
      if (!fields.ok) return fail(fields.unauthorized, fields.message);

      for (const id of removed) {
        setStage(t("Şəkillər yenilənir…"));
        const gone = await deleteImage(listing.id, id);
        if (!gone.ok) return fail(gone.unauthorized, gone.message);
      }
      setRemoved([]);

      for (let i = uploaded.current; i < added.length; i++) {
        setStage(t("Şəkillər yüklənir ({done}/{total})…", { done: i + 1, total: added.length }));
        const form = new FormData();
        form.set("file", added[i].file, added[i].file.name);
        const res = await fetch(`/bff/listings/${listing.id}/images`, { method: "POST", body: form });
        if (res.status === 401) return fail(true, "");
        if (!res.ok) return fail(false, t("{n}-ci şəkil yüklənmədi. Yenidən cəhd edin.", { n: i + 1 }));
        uploaded.current = i + 1;
      }

      if (cover && !removed.includes(cover)) {
        const done = await setCover(listing.id, cover);
        if (!done.ok) return fail(done.unauthorized, done.message);
      }

      setStage(t("Göndərilir…"));
      const sent = await submit(listing.id);
      if (!sent.ok) return fail(sent.unauthorized, sent.message);

      router.replace(`/elanlarim/${listing.id}`);
      router.refresh();
    } catch {
      fail(false, t("Serverə qoşulmaq olmadı. İnternetinizi yoxlayıb yenidən cəhd edin."));
    }
  }

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        void save();
      }}
      noValidate
      className="space-y-5"
    >
      <section className="space-y-4 rounded-2xl border border-line bg-surface p-5 md:p-6">
        <h2 className="text-lg font-extrabold">{t("Məlumat")}</h2>

        <label className="block">
          <span className="mb-2 block text-sm font-bold">{t("Başlıq")}</span>
          <input value={title} onChange={(e) => setTitle(e.target.value)} disabled={busy} maxLength={150} className={input} />
          {showErrors && <Problem>{problems.title}</Problem>}
        </label>

        <label className="block">
          <span className="mb-2 block text-sm font-bold">{t("Təsvir")}</span>
          <textarea
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            disabled={busy}
            maxLength={10000}
            rows={6}
            className={`${input} h-auto py-3`}
          />
          {showErrors && <Problem>{problems.description}</Problem>}
        </label>

        <div className="grid gap-4 sm:grid-cols-2">
          <label className="block">
            <span className="mb-2 block text-sm font-bold">{t("Qiymət (AZN)")}</span>
            <input
              value={priceText}
              onChange={(e) => setPriceText(e.target.value)}
              disabled={busy || unit === 6}
              inputMode="decimal"
              className={input}
            />
            {showErrors && <Problem>{problems.price}</Problem>}
          </label>
          <div>
            <span className="mb-2 block text-sm font-bold">{t("Müddət")}</span>
            <div className="flex flex-wrap gap-2">
              {periods.map((p) => (
                <button
                  key={p}
                  type="button"
                  disabled={busy}
                  aria-pressed={unit === p}
                  onClick={() => setUnit(p)}
                  className={`rounded-lg border px-4 py-2.5 text-sm font-semibold ${
                    unit === p ? "border-primary bg-primary text-white" : "border-line bg-surface hover:border-primary"
                  }`}
                >
                  {p === 6 ? t("Razılaşma") : `/ ${t(unitLabel[p])}`}
                </button>
              ))}
            </div>
          </div>
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          <div className="block">
            <span className="mb-2 block text-sm font-bold">{t("Şəhər")}</span>
            <Select
              value={cityId}
              onChange={setCityId}
              disabled={busy}
              placeholder={t("Seçin")}
              ariaLabel={t("Şəhər")}
              className="h-11"
              options={cities.map((c) => ({ value: c.id, label: c.name }))}
            />
            {showErrors && <Problem>{problems.city}</Problem>}
          </div>
          {city?.hasDistricts && (
            <div className="block">
              <span className="mb-2 block text-sm font-bold">{t("Rayon")}</span>
              <Select
                value={districtId}
                onChange={(id) => setDistrictChoice({ cityId, id })}
                disabled={busy}
                placeholder={t("Seçin")}
                ariaLabel={t("Rayon")}
                className="h-11"
                options={districts.map((d) => ({ value: d.id, label: d.name }))}
              />
              {showErrors && <Problem>{problems.district}</Problem>}
            </div>
          )}
        </div>
      </section>

      {fieldDefs.length > 0 && (
        <section className="space-y-4 rounded-2xl border border-line bg-surface p-5 md:p-6">
          <h2 className="text-lg font-extrabold">{t("Əlavə xüsusiyyətlər")}</h2>
          <div className="grid gap-5 sm:grid-cols-2">
            {fieldDefs.map((def) => (
              <div
                key={def.id}
                className={def.type === FieldType.multiSelect || def.type === FieldType.singleSelect ? "sm:col-span-2" : ""}
              >
                <FieldInput
                  def={def}
                  answer={answers[def.id] ?? emptyAnswer}
                  error={showErrors ? (fieldErrors[def.id] ?? null) : null}
                  disabled={busy}
                  onChange={(next) => setAnswers((a) => ({ ...a, [def.id]: next }))}
                />
              </div>
            ))}
          </div>
        </section>
      )}

      <section className="space-y-4 rounded-2xl border border-line bg-surface p-5 md:p-6">
        <h2 className="text-lg font-extrabold">{t("Şəkillər")}</h2>

        {saved.length > 0 && (
          <ul className="grid grid-cols-3 gap-2 sm:grid-cols-4 md:grid-cols-5">
            {saved.map((img, i) => (
              <li key={img.id} className="relative aspect-square overflow-hidden rounded-lg bg-primary-soft">
                <Image src={ownerImage(img.url)!} alt={t("Şəkil {n}", { n: i + 1 })} fill unoptimized sizes="160px" className="object-cover" />
                {i === 0 ? (
                  <span className="absolute bottom-1 left-1 flex items-center gap-1 rounded-md bg-ink/70 px-1.5 py-0.5 text-[11px] font-bold text-white">
                    <Star className="size-3 fill-warning text-warning" aria-hidden />
                    {t("Əsas")}</span>
                ) : (
                  <button
                    type="button"
                    disabled={busy}
                    onClick={() => setCoverId(img.id)}
                    className="absolute bottom-1 left-1 rounded-md bg-ink/70 px-1.5 py-0.5 text-[11px] font-bold text-white hover:bg-ink"
                  >
                    {t("Əsas et")}</button>
                )}
                <button
                  type="button"
                  disabled={busy}
                  onClick={() => setRemoved((r) => [...r, img.id])}
                  aria-label={t("Şəkli sil")}
                  className="absolute right-1 top-1 flex size-7 items-center justify-center rounded-full bg-ink/70 text-white hover:bg-ink"
                >
                  <X className="size-4" />
                </button>
              </li>
            ))}
          </ul>
        )}

        <PhotoPicker photos={added} disabled={busy} onChange={setAdded} max={MAX_PHOTOS - saved.length} />
        {showErrors && <Problem>{problems.photos}</Problem>}
      </section>

      {error && (
        <p role="alert" className="rounded-xl bg-danger/10 px-4 py-3 text-sm font-semibold text-danger">
          {error} {t("Dəyişikliklər saxlanıb, düyməyə yenidən basın.")}</p>
      )}
      <button
        type="submit"
        disabled={busy}
        className="flex h-14 w-full items-center justify-center gap-2 rounded-xl bg-primary text-base font-bold text-white hover:bg-primary-press disabled:opacity-70"
      >
        {busy && <Loader2 className="size-5 animate-spin" aria-hidden />}
        {stage ?? t("Yadda saxla və yoxlamaya göndər")}
      </button>
    </form>
  );
}
