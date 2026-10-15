import { LoadProblem, PageHead } from "@/components/admin/ui";
import { LocationManager, type CityRow } from "@/components/admin/LocationManager";
import { authed } from "@/lib/authed";

/** The cities and districts a listing can be placed in. */
export default async function LocationsAdminPage() {
  const result = await authed<CityRow[]>("/api/admin/locations");
  if (!result.ok) return <LoadProblem what="Şəhərlər" status={result.status} />;

  return (
    <div className="space-y-5">
      <PageHead
        title="Şəhər və rayonlar"
        hint="Elan verərkən seçilən yerlər. Silmək olmur (elanlar onlara bağlıdır): lazım olmayan yer söndürülür və yeni elanlarda görünmür. Dəyişiklik saytda dərhal görünür."
      />
      <LocationManager cities={result.data} />
    </div>
  );
}
