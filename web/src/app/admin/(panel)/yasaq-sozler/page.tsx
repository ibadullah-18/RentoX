import { BannedWordsManager, type BannedWordRow } from "@/components/admin/BannedWordsManager";
import { LoadProblem, PageHead } from "@/components/admin/ui";
import { authed } from "@/lib/authed";

/** Words that mark a listing for the moderators. */
export default async function BannedWordsPage() {
  const result = await authed<BannedWordRow[]>("/api/admin/banned-words");
  if (!result.ok) return <LoadProblem what="Qadağan sözlər" status={result.status} />;

  return (
    <div className="space-y-5">
      <PageHead
        title="Qadağan sözlər"
        hint="Elanın başlığında və ya təsvirində bu sözlər tapılsa, elan moderasiya siyahısında və səhifəsində xəbərdarlıqla göstərilir. Elan avtomatik silinmir: qərarı moderator verir."
      />
      <BannedWordsManager words={result.data} />
    </div>
  );
}
