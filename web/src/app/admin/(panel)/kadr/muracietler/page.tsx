import { MessageSquareText } from "lucide-react";
import { Avatar } from "@/components/admin/Avatar";
import { Empty, LoadProblem, PageHead, Pill, dateTime } from "@/components/admin/ui";
import { RequestHandler } from "@/components/admin/HrForms";
import { requestStatusLabel, requestStatusTone, type StaffRequest } from "@/lib/admin-hr";
import { authed } from "@/lib/authed";
import { getRoles } from "@/lib/session";

function RequestList({ items, owner, mine }: { items: StaffRequest[]; owner: boolean; mine: boolean }) {
  if (items.length === 0) return <Empty icon={MessageSquareText} text="Müraciət yoxdur" />;
  return (
    <ul className="space-y-3">
      {items.map((r) => {
        const open = r.status === 1 || r.status === 2;
        return (
          <li key={r.id} className="flex flex-wrap items-start gap-4 rounded-xl border border-line bg-surface p-4">
            <Avatar name={r.fromName} />
            <div className="min-w-0 flex-1 space-y-1">
              <p className="flex flex-wrap items-center gap-2 font-extrabold">
                {r.subject}
                <Pill tone={requestStatusTone[r.status]}>{requestStatusLabel[r.status]}</Pill>
                {r.toOwner && r.status === 1 && <Pill tone="primary">Birbaşa rəhbərə</Pill>}
              </p>
              <p className="text-sm text-ink-2">
                {r.fromName} · {dateTime(r.createdAtUtc)}
              </p>
              <p className="whitespace-pre-wrap text-sm">{r.body}</p>
              {r.answer && (
                <p className="rounded-lg bg-bg px-3 py-2 text-sm">
                  <b>{r.handledByName ?? "Cavab"}:</b> {r.answer}
                </p>
              )}
            </div>
            {open && (mine || owner) && <RequestHandler id={r.id} canForward={!owner && r.status === 1} />}
          </li>
        );
      })}
    </ul>
  );
}

/** What the staff write to the management. The owner sees what went to him and what the staff side handles. */
export default async function KadrRequestsPage() {
  const owner = (await getRoles()).includes("SuperAdmin");

  const [forOwner, forStaff] = await Promise.all([
    owner ? authed<StaffRequest[]>("/api/admin/hr/requests", { query: { scope: "owner" } }) : Promise.resolve(null),
    authed<StaffRequest[]>("/api/admin/hr/requests"),
  ]);
  if (!forStaff.ok) return <LoadProblem what="Müraciətlər" status={forStaff.status} />;

  return (
    <div className="space-y-6">
      <PageHead
        title="İşçi müraciətləri"
        hint="İşçi ya işçilərə baxan şəxsə yazır (o cavab verir və ya rəhbərə yönləndirir), ya da birbaşa rəhbərə."
      />

      {owner && forOwner?.ok && (
        <section className="space-y-3">
          <h2 className="text-lg font-extrabold">Sizə gələnlər ({forOwner.data.filter((r) => r.status <= 2).length} gözləyir)</h2>
          <RequestList items={forOwner.data} owner mine />
        </section>
      )}

      <section className="space-y-3">
        <h2 className="text-lg font-extrabold">{owner ? "Kadr tərəfində olanlar" : "Gələn müraciətlər"}</h2>
        <RequestList items={forStaff.data} owner={owner} mine />
      </section>
    </div>
  );
}
