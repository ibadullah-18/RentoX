import { getT } from "@/lib/i18n/server";
import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { StoreForm } from "@/components/store/StoreForm";
import { StoreInfoForm } from "@/components/store/StoreInfoForm";
import { StoreContactForm } from "@/components/store/StoreContactForm";
import { StoreImages } from "@/components/store/StoreImages";
import { StoreVipPanel } from "@/components/store/StoreVipPanel";
import { StoreStatsCard } from "@/components/store/StoreStatsCard";
import { SubmitStore } from "@/components/store/SubmitStore";
import { getMyStore, getMyStoreStats, getMyStoreVip } from "@/lib/personal";
import { getSession } from "@/lib/session";
import { emptyStoreForm } from "@/lib/store-form";
import { msUntil } from "@/lib/store-contact";
import { tx } from "@/lib/i18n/locales";
import { StoreStatus } from "@/lib/types";

export async function generateMetadata(): Promise<Metadata> {
  return { title: (await getT())("Mağazam"), robots: { index: false } };
}

const label: Record<number, string> = {
  [StoreStatus.draft]: tx("Qaralama"),
  [StoreStatus.pendingReview]: tx("Yoxlamada"),
  [StoreStatus.active]: tx("Aktiv"),
  [StoreStatus.rejected]: tx("Rədd edilib"),
  [StoreStatus.suspended]: tx("Dayandırılıb"),
};

const tone: Record<number, string> = {
  [StoreStatus.active]: "bg-success-soft text-success",
  [StoreStatus.pendingReview]: "bg-warning-soft text-warning",
  [StoreStatus.rejected]: "bg-danger/10 text-danger",
  [StoreStatus.suspended]: "bg-line text-ink-2",
  [StoreStatus.draft]: "bg-primary-soft text-primary",
};

export default async function MyStorePage() {
  const t = await getT();
  if (!(await getSession())) redirect("/giris?next=/magaza");

  const result = await getMyStore();
  if (!result.ok && result.status === 401) redirect("/giris?next=/magaza");

  // No store yet: the form to create one.
  if (!result.ok) {
    return (
      <div className="mx-auto max-w-2xl space-y-6">
        <div>
          <h1 className="text-2xl font-extrabold">{t("Mağaza yarat")}</h1>
          <p className="mt-1 text-ink-2">
            {t("Mağaza elanlarınızı bir yerdə göstərir, izləyicilər toplayır. Əvvəlcə məlumatı doldurun, sonra loqo əlavə edib yoxlamaya göndərin.")}</p>
        </div>
        <StoreForm mode="create" initial={emptyStoreForm} />
      </div>
    );
  }

  const store = result.data;
  const [stats, vip] =
    store.status === StoreStatus.active ? await Promise.all([getMyStoreStats(), getMyStoreVip()]) : [null, null];
  const contactOpen = store.status !== StoreStatus.suspended && store.status !== StoreStatus.deleted;
  const editable = store.status === StoreStatus.draft || store.status === StoreStatus.rejected;

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="min-w-0 space-y-1">
          <h1 className="truncate text-2xl font-extrabold">{store.name}</h1>
          <span
            className={`inline-flex rounded-md px-2.5 py-1 text-xs font-bold ${tone[store.status] ?? "bg-line text-ink-2"}`}
          >
            {label[store.status] ? t(label[store.status]) : "—"}
          </span>
        </div>
        {store.status === StoreStatus.active && (
          <Link
            href={`/store/${store.slug}`}
            className="flex h-11 items-center rounded-xl border border-line px-5 font-bold hover:border-primary"
          >
            {t("Saytda bax")}</Link>
        )}
      </div>

      {store.status === StoreStatus.rejected && (
        <div role="alert" className="rounded-xl bg-danger/10 px-4 py-3 text-sm">
          <p className="font-bold text-danger">{t("Mağaza rədd edilib")}</p>
          {store.rejectionReason && <p className="mt-1">{store.rejectionReason}</p>}
          <p className="mt-1 text-ink-2">{t("Düzəldib yenidən göndərə bilərsiniz.")}</p>
        </div>
      )}
      {store.status === StoreStatus.pendingReview && (
        <p className="rounded-xl bg-primary-soft px-4 py-3 text-sm font-semibold text-primary">
          {t("Moderator baxır. Nəticəni bildirişlə xəbər verəcəyik.")}</p>
      )}
      {store.status === StoreStatus.suspended && (
        <p className="rounded-xl bg-line px-4 py-3 text-sm font-semibold text-ink-2">
          {t("Mağaza dayandırılıb. Dəstəyə yazın.")}</p>
      )}

      {vip?.ok && <StoreVipPanel vip={vip.data} leftMs={vip.data.vipUntilUtc ? msUntil(vip.data.vipUntilUtc) : 0} />}

      {stats?.ok && <StoreStatsCard stats={stats.data} />}

      <StoreImages
        storeId={store.id}
        hasLogo={Boolean(store.logoImageUrl)}
        hasCover={Boolean(store.coverImageUrl)}
        editable={editable}
      />

      {editable ? (
        <>
          <StoreForm
            key={`${store.phoneNumber}|${store.email}|${store.address}|${store.instagramUrl}|${store.tiktokUrl}|${store.facebookUrl}|${store.websiteUrl}`}
            mode="edit"
            contactSeparate
            initial={{
              name: store.name,
              description: store.description,
              phone: store.phoneNumber,
              email: store.email ?? "",
              address: store.address ?? "",
              instagram: store.instagramUrl ?? "",
              tiktok: store.tiktokUrl ?? "",
              facebook: store.facebookUrl ?? "",
              website: store.websiteUrl ?? "",
            }}
          />
          <SubmitStore hasLogo={Boolean(store.logoImageUrl)} />
        </>
      ) : (
        <dl className="overflow-hidden rounded-xl border border-line bg-surface">
          {[
            ["Telefon", store.phoneNumber],
            [t("E-poçt"), store.email],
            [t("Ünvan"), store.address],
          ].map(([k, v]) =>
            v ? (
              <div key={k} className="flex justify-between gap-4 border-b border-line px-4 py-3 last:border-b-0">
                <dt className="text-ink-2">{k}</dt>
                <dd className="text-right font-semibold">{v}</dd>
              </div>
            ) : null,
          )}
        </dl>
      )}

      {store.status === StoreStatus.active && <StoreInfoForm key={`${store.name}|${store.description}`} name={store.name} description={store.description} />}

      {contactOpen && (
        <StoreContactForm
          key={JSON.stringify([store.phoneNumber, store.email, store.address, store.instagramUrl, store.tiktokUrl, store.facebookUrl, store.youtubeUrl, store.websiteUrl, store.whatsAppNumber, store.whatsAppMessage, store.workingHours])}
          initial={{
            phone: store.phoneNumber,
            email: store.email ?? "",
            address: store.address ?? "",
            whatsApp: store.whatsAppNumber ?? "",
            whatsAppMessage: store.whatsAppMessage ?? "",
            instagram: store.instagramUrl ?? "",
            tiktok: store.tiktokUrl ?? "",
            youtube: store.youtubeUrl ?? "",
            facebook: store.facebookUrl ?? "",
            website: store.websiteUrl ?? "",
            hours: store.workingHours ?? [],
          }}
        />
      )}
    </div>
  );
}
