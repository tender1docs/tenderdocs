import { Fragment } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Check, Minus } from 'lucide-react';
import { Card, Skeleton } from '@/components/ui';
import { AdminApi } from '@/services/api';
import { cn } from '@/lib/utils';

const ROLE_ORDER = ['Admin', 'Uploader', 'Approver', 'Viewer'];

export default function AdminRolesPage() {
  const { data, isLoading } = useQuery({ queryKey: ['admin', 'roles'], queryFn: () => AdminApi.roles() });

  return (
    <div className="space-y-4">
      <div>
        <h2 className="text-lg font-semibold text-ink dark:text-slate-100">Roles &amp; permissions</h2>
        <p className="text-sm text-ink-muted">
          What each role can do. This is the live matrix the API enforces — editing roles is coming next.
        </p>
      </div>

      {isLoading || !data ? (
        <Skeleton className="h-96" />
      ) : (
        <Card className="overflow-x-auto p-0">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-line dark:border-[#222A31]">
                <th className="px-4 py-3 text-left font-semibold text-ink dark:text-slate-100">Permission</th>
                {ROLE_ORDER.filter((r) => data.roles.some((x) => x.role === r)).map((r) => (
                  <th key={r} className="px-4 py-3 text-center font-semibold text-ink dark:text-slate-100">{r}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {Array.from(new Set(data.permissions.map((p) => p.category))).map((cat) => {
                const roles = ROLE_ORDER.filter((r) => data.roles.some((x) => x.role === r));
                const has = (role: string, key: string) =>
                  data.roles.find((r) => r.role === role)?.permissions.includes(key) ?? false;
                return (
                  <Fragment key={cat}>
                    <tr>
                      <td colSpan={roles.length + 1}
                        className="bg-slate-50 px-4 py-2 text-xs font-semibold uppercase tracking-wide text-ink-muted dark:bg-[#12181D]">
                        {cat}
                      </td>
                    </tr>
                    {data.permissions.filter((p) => p.category === cat).map((p) => (
                      <tr key={p.key} className="border-b border-line last:border-0 dark:border-[#222A31]">
                        <td className="px-4 py-2.5">
                          <div className="font-medium text-ink dark:text-slate-100">{p.description}</div>
                          <div className="text-xs text-ink-faint">{p.key}</div>
                        </td>
                        {roles.map((r) => (
                          <td key={r} className="px-4 py-2.5 text-center">
                            {has(r, p.key)
                              ? <Check className="inline h-4 w-4 text-valid-text" />
                              : <Minus className={cn('inline h-4 w-4 text-ink-faint/40')} />}
                          </td>
                        ))}
                      </tr>
                    ))}
                  </Fragment>
                );
              })}
            </tbody>
          </table>
        </Card>
      )}
    </div>
  );
}
