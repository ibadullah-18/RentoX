import { notFound } from "next/navigation";
import { BrandingForm } from "@/components/admin/BrandingForm";
import { LoadProblem, PageHead } from "@/components/admin/ui";
import { authed } from "@/lib/authed";
import { canSeeBranding } from "@/lib/session";
import type { Branding } from "@/lib/types";

/** The name, the logos and the icon of the brand: the owner changes them here, whenever they want. */
export default async function BrandingPage() {
  if (!(await canSeeBranding())) notFound();

  const result = await authed<Branding>("/api/admin/branding");
  if (!result.ok) return <LoadProblem what="Brend ayarları" status={result.status} />;

  return (
    <div className="space-y-5">
      <PageHead
        title="Brend: loqo, ikon, ad"
        hint="Saytın başlığındakı və alt hissəsindəki loqonu, brauzer sekməsindəki ikonu, saytın adını istədiyin vaxt dəyişə bilərsən. Dəyişiklik saytda və mobil tətbiqdə bir dəqiqə ərzində görünür. Şəkil seçilməyibsə, saytla gələn standart şəkil göstərilir."
      />
      <BrandingForm branding={result.data} />
    </div>
  );
}
