import Link from "next/link";
import { notFound } from "next/navigation";
import { ScrollText } from "lucide-react";
import { Chips, Empty, PageHead, dateTime, shortId } from "@/components/admin/ui";
import { PrintButton } from "@/components/admin/PrintButton";
import { Pagination } from "@/components/Pagination";
import { adminHref } from "@/lib/admin-path";
import { auditAction } from "@/lib/admin-labels";
import type { AuditPage } from "@/lib/admin-types";
import { authed } from "@/lib/authed";
import { getRoles } from "@/lib/session";

/** Who did what: only for the SuperAdmin, and only to read. */
export default async function AuditPageView({ searchParams }: PageProps<"/admin/audit">) {
  if (!(await getRoles()).includes("SuperAdmin")) notFound();

  const sp = await searchParams;
  const one = (v: string | string[] | undefined) => (Array.isArray(v) ? v[0] : v);
  const page = Math.max(1, Number.parseInt(one(sp.page) ?? "1", 10) || 1);
  const action = one(sp.action);
  const actor = one(sp.actor);
  const target = one(sp.target);

  const result = await authed<AuditPage>("/api/admin/audit-logs", {
    query: { page, pageSize: 25, action, actorUserId: actor, targetId: target },
  });
  const data = result.ok ? result.data : null;

  const link = (over: Record<string, string | undefined>) => {
    const q = new URLSearchParams();
    const next = { action, actor, target, ...over };
    for (const [k, v] of Object.entries(next)) if (v) q.set(k, v);
    const text = q.toString();
    return adminHref(text ? `/audit?${text}` : "/audit");
  };

  const chips = [
    { label: "Hamısı", href: link({ action: undefined, page: undefined }), value: undefined },
    ...Object.entries(auditAction).map(([v, a]) => ({ label: a.label, href: link({ action: v, page: undefined }), value: v })),
  ];

  const name = (n: number | null | undefined, a?: (typeof auditAction)[number]) =>
    n == null ? "—" : a?.format ? a.format(n) : (a?.value?.[n] ?? String(n));

  return (
    <div className="space-y-5">
      <PageHead
        title="Əməliyyat tarixçəsi"
        hint="Statusların dəyişməsi və dəstək cavabları. Bu, bütün kliklərin yox, yalnız qeydə alınan əməliyyatların siyahısıdır."
        actions={<PrintButton />}
      />
      <Chips items={chips} current={action} />
      {(actor || target) && (
        <p className="no-print text-sm text-ink-2">
          Filtr: {actor && <>icra edən {shortId(actor)} </>}
          {target && <>obyekt {shortId(target)} </>}
          <Link href={link({ actor: undefined, target: undefined })} className="font-bold text-primary hover:underline">
            təmizlə
          </Link>
        </p>
      )}

      {!data || data.items.length === 0 ? (
        <Empty icon={ScrollText} text="Qeyd yoxdur" />
      ) : (
        <>
          <div className="overflow-x-auto rounded-xl border border-line bg-surface">
            <table className="w-full min-w-[720px] text-left text-sm">
              <thead className="border-b border-line text-ink-2">
                <tr>
                  <th className="px-4 py-3 font-semibold">Vaxt</th>
                  <th className="px-4 py-3 font-semibold">İcra edən</th>
                  <th className="px-4 py-3 font-semibold">Əməliyyat</th>
                  <th className="px-4 py-3 font-semibold">Obyekt</th>
                  <th className="px-4 py-3 font-semibold">Əvvəl → sonra</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((i) => {
                  const a = auditAction[i.action];
                  return (
                    <tr key={i.id} className="border-b border-line last:border-b-0">
                      <td className="whitespace-nowrap px-4 py-3">{dateTime(i.occurredAtUtc)}</td>
                      <td className="px-4 py-3">
                        <Link href={link({ actor: i.actorUserId, page: undefined })} className="font-mono text-xs hover:text-primary">
                          {i.actorName ?? shortId(i.actorUserId)}
                        </Link>
                        {i.actorName && <span className="block font-mono text-[11px] text-ink-2">{shortId(i.actorUserId)}</span>}
                      </td>
                      <td className="px-4 py-3 font-semibold">{a?.label ?? `Əməliyyat ${i.action}`}</td>
                      <td className="px-4 py-3">
                        <Link href={link({ target: i.targetId, page: undefined })} className="hover:text-primary">
                          {i.targetName ? (
                            <>
                              <span className="font-semibold">{i.targetName}</span>
                              <span className="block text-[11px] text-ink-2">
                                {a?.target ?? "Obyekt"} <span className="font-mono">{shortId(i.targetId)}</span>
                              </span>
                            </>
                          ) : (
                            <>
                              {a?.target ?? "Obyekt"} <span className="font-mono text-xs">{shortId(i.targetId)}</span>
                            </>
                          )}
                        </Link>
                      </td>
                      <td className="px-4 py-3">
                        {i.previousValue == null && i.currentValue == null
                          ? "—"
                          : `${name(i.previousValue, a)} → ${name(i.currentValue, a)}`}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
          <p className="text-xs text-ink-2">
            Cəmi {data.totalCount} qeyd · səhifə {data.page}/{data.totalPages}
          </p>
          <Pagination page={data.page} totalPages={data.totalPages} hrefFor={(p) => link({ page: p > 1 ? String(p) : undefined })} />
        </>
      )}
    </div>
  );
}
