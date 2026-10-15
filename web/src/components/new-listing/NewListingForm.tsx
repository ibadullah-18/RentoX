"use client";

import { Select } from "@/components/ui/Select";
import { useLocale, useT } from "@/lib/i18n/client";
import { unitLabel } from "@/lib/format";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useMemo, useRef, useState } from "react";
import { CheckCircle2, Clock, Loader2, Wallet } from "lucide-react";
import {
  createDraft,
  payAndActivate,
  submitListing,
  uploadedPhotoCount,
} from "@/app/yeni-elan/actions";
import type { Category, City, District, FieldDefinition } from "@/lib/types";
import { CategoryPicker } from "./CategoryPicker";
import { FieldInput } from "./FieldInput";
import { PhotoPicker, type Photo } from "./PhotoPicker";
import { emptyAnswer, parseNumber, toRequest, validate, type Answer } from "./fields";

type Phase =
  | { name: "editing" }
  | { name: "creating" }
  | { name: "uploading"; done: number }
  | { name: "submitting" }
  | { name: "failed"; message: string }
  | { name: "done"; id: string; status: number };

const ACTIVE = 3;
const PENDING = 2;
const PAYMENT_REQUIRED = 8;

const input =
  "h-11 w-full rounded-xl border border-line bg-surface px-4 text-[16px] outline-none focus:border-primary disabled:opacity-60";

function Section({
  number,
  title,
  children,
}: {
  number: number;
  title: string;
  children: React.ReactNode;
}) {
  return (
    <section className="space-y-4 rounded-2xl border border-line bg-surface p-5 md:p-6">
      <h2 className="flex items-center gap-3 text-lg font-extrabold">
        <span className="flex size-8 items-center justify-center rounded-full bg-primary text-sm text-white">
          {number}
        </span>
        {title}
      </h2>
      {children}
    </section>
  );
}

function Problem({ children }: { children?: React.ReactNode }) {
  if (!children) return null;
  return (
    <p role="alert" className="mt-1.5 text-sm font-semibold text-danger">
      {children}
    </p>
  );
}

export function NewListingForm({
  categories,
  cities,
  activationFee,
  maxPhotos,
}: {
  categories: Category[];
  cities: City[];
  /** What activating one more listing costs once the free ones are used. */
  activationFee: number;
  /** The most photos one listing may have. */
  maxPhotos: number;
}) {
  const t = useT();
  const locale = useLocale();
  const router = useRouter();

  const [path, setPath] = useState<string[]>([]);
  // Fields, answers and districts belong to one category / city: they are
  // stored with its id, and ignored once another one is chosen.
  const [loadedFields, setLoadedFields] = useState<{ id: string; list: FieldDefinition[] } | null>(null);
  const [failedFields, setFailedFields] = useState<string | null>(null);
  const [answerSet, setAnswerSet] = useState<{ id: string; values: Record<string, Answer> } | null>(null);

  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [priceText, setPriceText] = useState("");
  const [unitChoice, setUnit] = useState<number>(2);

  const [cityId, setCityId] = useState("");
  const [loadedDistricts, setLoadedDistricts] = useState<{ id: string; list: District[] } | null>(null);
  const [districtChoice, setDistrictChoice] = useState<{ cityId: string; id: string } | null>(null);

  const [photos, setPhotos] = useState<Photo[]>([]);
  const [showErrors, setShowErrors] = useState(false);
  const [phase, setPhase] = useState<Phase>({ name: "editing" });

  // What an interrupted attempt already did, so a retry never repeats it.
  const draftId = useRef<string | null>(null);
  const uploaded = useRef(0);
  const [hasDraft, setHasDraft] = useState(false);

  // ---- the chosen category ------------------------------------------------
  const chain = useMemo(() => {
    const out: Category[] = [];
    let level = categories;
    for (const id of path) {
      const found = level.find((c) => c.id === id);
      if (!found) break;
      out.push(found);
      level = found.children;
    }
    return out;
  }, [categories, path]);
  const category = chain.at(-1) ?? null;
  const leaf = category && category.children.length === 0 ? category : null;

  const periods = useMemo(
    () => (leaf?.rentalPeriods?.length ? leaf.rentalPeriods : [1, 2, 3, 4, 5, 6]),
    [leaf],
  );

  // Not every category allows every period (daily, monthly...).
  const unit = periods.includes(unitChoice) ? unitChoice : periods.includes(2) ? 2 : periods[0];

  // The fields this category asks for.
  const leafId = leaf?.id ?? null;
  useEffect(() => {
    if (!leafId) return;
    let cancelled = false;
    fetch(`/api/categories/${leafId}/fields?language=${locale}`)
      .then((r) => (r.ok ? (r.json() as Promise<FieldDefinition[]>) : Promise.reject()))
      .then((list) => {
        if (!cancelled) {
          setLoadedFields({
            id: leafId,
            list: [...list].sort((a, b) => a.displayOrder - b.displayOrder),
          });
        }
      })
      .catch(() => {
        if (!cancelled) setFailedFields(leafId);
      });
    return () => {
      cancelled = true;
    };
  }, [leafId, locale]);
  const fields = loadedFields?.id === leafId ? loadedFields.list : null;
  const fieldsFailed = failedFields !== null && failedFields === leafId;
  const answers = answerSet?.id === leafId ? answerSet.values : {};

  // The districts of the chosen city.
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

  // ---- checking ---------------------------------------------------------------
  const price = parseNumber(priceText);
  const priceError =
    priceText.trim() === ""
      ? unit === 6
        ? null
        : t("Qiyməti yazın.")
      : price === null || price < 0
        ? t("Düzgün qiymət yazın.")
        : null;

  const fieldErrors = Object.fromEntries(
    (fields ?? []).flatMap((def) => {
      const error = validate(def, answers[def.id] ?? emptyAnswer);
      return error ? [[def.id, error] as const] : [];
    }),
  );

  const errors = {
    category: leaf ? null : t("Kateqoriyanı axıra qədər seçin."),
    title: title.trim() ? null : t("Başlığı yazın."),
    description: description.trim() ? null : t("Təsviri yazın."),
    price: priceError,
    city: cityId ? null : t("Şəhəri seçin."),
    district: city?.hasDistricts && !districtId ? t("Rayonu seçin.") : null,
    fields: Object.keys(fieldErrors).length > 0 || fields === null ? "fields" : null,
    photos: photos.length > 0 ? null : t("Ən azı bir şəkil əlavə edin."),
  };
  const valid = Object.values(errors).every((e) => e === null);
  const busy = ["creating", "uploading", "submitting"].includes(phase.name);

  // ---- sending ---------------------------------------------------------------
  async function send() {
    if (busy) return;
    if (!valid || !leaf) {
      setShowErrors(true);
      document.querySelector("[data-first-error]")?.scrollIntoView({ block: "center", behavior: "smooth" });
      return;
    }

    try {
      if (!draftId.current) {
        setPhase({ name: "creating" });
        const draft = await createDraft({
          categoryId: leaf.id,
          title,
          description,
          price: price ?? 0,
          rentalPeriodUnit: unit,
          cityId,
          districtId: districtId || undefined,
          fields: (fields ?? []).flatMap((def) => {
            const body = toRequest(def, answers[def.id] ?? emptyAnswer);
            return body ? [body] : [];
          }),
        });
        if (!draft.ok) return failed(draft.unauthorized, draft.message);
        draftId.current = draft.id;
        setHasDraft(true);
      }
      const id = draftId.current;

      // A photo whose answer got lost may already be there: ask the server.
      if (uploaded.current > 0) {
        const held = await uploadedPhotoCount(id);
        if (held !== null) uploaded.current = Math.min(held, photos.length);
      }

      for (let i = uploaded.current; i < photos.length; i++) {
        setPhase({ name: "uploading", done: i });
        const form = new FormData();
        form.set("file", photos[i].file, photos[i].file.name);
        const res = await fetch(`/bff/listings/${id}/images`, { method: "POST", body: form });
        if (res.status === 401) return failed(true, "");
        if (!res.ok) {
          return failed(
            false,
            res.status === 429
              ? t("Çox tez-tez şəkil yüklənib. Bir az gözləyib yenidən yoxlayın.")
              : t("{n}-ci şəkil yüklənmədi. Yenidən cəhd edin.", { n: i + 1 }),
          );
        }
        uploaded.current = i + 1;
      }

      setPhase({ name: "submitting" });
      const sent = await submitListing(id);
      if (!sent.ok) return failed(sent.unauthorized, sent.message);
      setPhase({ name: "done", id, status: sent.status });
      router.refresh();
    } catch {
      failed(false, t("Serverə qoşulmaq olmadı. İnternetinizi yoxlayıb yenidən cəhd edin."));
    }
  }

  function failed(unauthorized: boolean, message: string) {
    if (unauthorized) {
      router.push("/giris?next=/yeni-elan");
      return;
    }
    setPhase({ name: "failed", message });
  }

  // ---- the result ---------------------------------------------------------
  if (phase.name === "done") {
    return <Result id={phase.id} status={phase.status} activationFee={activationFee} />;
  }

  const first = (key: keyof typeof errors) =>
    showErrors && errors[key] ? { "data-first-error": "" } : {};

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        void send();
      }}
      className="space-y-5"
      noValidate
    >
      <Section number={1} title={t("Kateqoriya")}>
        <div {...first("category")}>
          <CategoryPicker tree={categories} path={path} disabled={busy} onChange={setPath} />
          {showErrors && <Problem>{errors.category}</Problem>}
        </div>
      </Section>

      {leaf && (
        <>
          <Section number={2} title={t("Məlumat")}>
            <label className="block" {...first("title")}>
              <span className="mb-2 block text-sm font-bold">{t("Başlıq")}</span>
              <input
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                disabled={busy}
                maxLength={150}
                placeholder={t("Məs.: Toyota Camry 2022, gündəlik")}
                className={input}
              />
              {showErrors && <Problem>{errors.title}</Problem>}
            </label>

            <label className="block" {...first("description")}>
              <span className="mb-2 block text-sm font-bold">{t("Təsvir")}</span>
              <textarea
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                disabled={busy}
                maxLength={10000}
                rows={6}
                placeholder={t("Şərtlər, depozit, təhvil vaxtı və s.")}
                className={`${input} h-auto py-3`}
              />
              {showErrors && <Problem>{errors.description}</Problem>}
            </label>

            <div className="grid gap-4 sm:grid-cols-2" {...first("price")}>
              <label className="block">
                <span className="mb-2 block text-sm font-bold">{t("Qiymət (AZN)")}</span>
                <input
                  value={priceText}
                  onChange={(e) => setPriceText(e.target.value)}
                  disabled={busy || unit === 6}
                  inputMode="decimal"
                  placeholder={unit === 6 ? t("Razılaşma ilə") : "0"}
                  className={input}
                />
                {showErrors && <Problem>{errors.price}</Problem>}
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
                        unit === p
                          ? "border-primary bg-primary text-white"
                          : "border-line bg-surface hover:border-primary"
                      }`}
                    >
                      {p === 6 ? t("Razılaşma") : `/ ${t(unitLabel[p])}`}
                    </button>
                  ))}
                </div>
              </div>
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
              <div className="block" {...first("city")}>
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
                {showErrors && <Problem>{errors.city}</Problem>}
              </div>
              {city?.hasDistricts && (
                <div className="block" {...first("district")}>
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
                  {showErrors && <Problem>{errors.district}</Problem>}
                </div>
              )}
            </div>
          </Section>

          {(fields === null && !fieldsFailed) || (fields && fields.length > 0) || fieldsFailed ? (
            <Section number={3} title={t("Əlavə xüsusiyyətlər")}>
              {fieldsFailed && (
                <Problem>{t("Xüsusiyyətlər yüklənmədi. Kateqoriyanı yenidən seçin.")}</Problem>
              )}
              {fields === null && !fieldsFailed && (
                <p className="flex items-center gap-2 text-ink-2">
                  <Loader2 className="size-4 animate-spin" aria-hidden /> {t("Yüklənir…")}</p>
              )}
              <div className="grid gap-5 sm:grid-cols-2">
                {(fields ?? []).map((def) => (
                  <div
                    key={def.id}
                    className={def.type === 6 || def.type === 5 ? "sm:col-span-2" : ""}
                    {...(showErrors && fieldErrors[def.id] ? { "data-first-error": "" } : {})}
                  >
                    <FieldInput
                      def={def}
                      answer={answers[def.id] ?? emptyAnswer}
                      error={showErrors ? (fieldErrors[def.id] ?? null) : null}
                      disabled={busy}
                      onChange={(next) =>
                        setAnswerSet({
                          id: leaf.id,
                          values: { ...answers, [def.id]: next },
                        })
                      }
                    />
                  </div>
                ))}
              </div>
            </Section>
          ) : null}

          <Section number={4} title={t("Şəkillər")}>
            <div {...first("photos")}>
              <PhotoPicker photos={photos} disabled={busy} onChange={setPhotos} max={maxPhotos} />
              {showErrors && <Problem>{errors.photos}</Problem>}
            </div>
          </Section>

          <div className="space-y-3">
            {phase.name === "failed" && (
              <p role="alert" className="rounded-xl bg-danger/10 px-4 py-3 text-sm font-semibold text-danger">
                {phase.message} {t("Yazdıqlarınız saxlanıb, düyməyə yenidən basın.")}</p>
            )}
            <button
              type="submit"
              disabled={busy}
              className="flex h-14 w-full items-center justify-center gap-2 rounded-xl bg-primary text-base font-bold text-white hover:bg-primary-press disabled:opacity-70"
            >
              {busy && <Loader2 className="size-5 animate-spin" aria-hidden />}
              {phase.name === "creating" && t("Elan yaradılır…")}
              {phase.name === "uploading" &&
                t("Şəkillər yüklənir ({done}/{total})…", { done: phase.done + 1, total: photos.length })}
              {phase.name === "submitting" && t("Göndərilir…")}
              {!busy && (hasDraft ? t("Yenidən cəhd et") : t("Elanı göndər"))}
            </button>
          </div>
        </>
      )}
    </form>
  );
}

/** What happened to the listing after it was sent. */
function Result({ id, status, activationFee }: { id: string; status: number; activationFee: number }) {
  const t = useT();
  const router = useRouter();
  const [paying, setPaying] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [live, setLive] = useState(status === ACTIVE);

  async function pay() {
    setPaying(true);
    setMessage(null);
    const res = await payAndActivate(id);
    setPaying(false);
    if (res.ok) {
      setLive(true);
      router.refresh();
    } else if (res.unauthorized) {
      router.push("/giris?next=/yeni-elan");
    } else {
      setMessage(
        t("Ödəniş alınmadı. Balansınız çatmırsa, onu mobil tətbiqdən artırın və yenidən cəhd edin."),
      );
    }
  }

  const card = "mx-auto flex max-w-md flex-col items-center gap-3 rounded-2xl border border-line bg-surface p-8 text-center";
  const primary =
    "mt-2 flex h-11 w-full items-center justify-center gap-2 rounded-xl bg-primary px-6 font-bold text-white hover:bg-primary-press disabled:opacity-70";

  if (live || status === PENDING) {
    const isLive = live;
    return (
      <div className={card}>
        {isLive ? (
          <CheckCircle2 className="size-14 text-success" aria-hidden />
        ) : (
          <Clock className="size-14 text-warning" aria-hidden />
        )}
        <h1 className="text-2xl font-extrabold">
          {isLive ? t("Elanınız dərc olundu") : t("Elan yoxlamaya göndərildi")}
        </h1>
        <p className="text-ink-2">
          {isLive
            ? t("İndi hamı onu görə bilər.")
            : t("Moderator baxdıqdan sonra dərc olunacaq. Nəticəni bildirişlə xəbər verəcəyik.")}
        </p>
        {isLive && (
          <Link href={`/listing/${id}`} className={primary}>
            {t("Elana bax")}</Link>
        )}
        <Link href="/" className="text-sm font-bold text-primary hover:underline">
          {t("Ana səhifə")}</Link>
      </div>
    );
  }

  if (status === PAYMENT_REQUIRED) {
    return (
      <div className={card}>
        <Wallet className="size-14 text-primary" aria-hidden />
        <h1 className="text-2xl font-extrabold">{t("Aktivləşdirmə haqqı")}</h1>
        <p className="text-ink-2">
          {t("Pulsuz elan limitiniz dolub. Bu elanı dərc etmək üçün {fee} AZN pul kisənizdən çıxılacaq.", { fee: activationFee })}</p>
        {message && (
          <p role="alert" className="text-sm font-semibold text-danger">
            {message}
          </p>
        )}
        <button type="button" onClick={pay} disabled={paying} className={primary}>
          {paying && <Loader2 className="size-5 animate-spin" aria-hidden />}
          {t("{fee} AZN ödə və aktiv et", { fee: activationFee })}</button>
        <Link href="/" className="text-sm font-bold text-primary hover:underline">
          {t("Sonra ödəyəcəyəm")}</Link>
      </div>
    );
  }

  return (
    <div className={card}>
      <CheckCircle2 className="size-14 text-primary" aria-hidden />
      <h1 className="text-2xl font-extrabold">{t("Elan göndərildi")}</h1>
      <p className="text-ink-2">{t("Elanın vəziyyətini mobil tətbiqdə görə bilərsiniz.")}</p>
      <Link href="/" className={primary}>
        {t("Ana səhifə")}</Link>
    </div>
  );
}
