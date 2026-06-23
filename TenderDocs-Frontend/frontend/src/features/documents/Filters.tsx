import { SlidersHorizontal } from 'lucide-react';
import { Card, Select, Segmented, Input } from '@/components/ui';
import type { ExpiryStatus } from '@/types';

export interface DocFilters {
  type: string; authority: string; fy: string; project: string; uploader: string;
  expiry: 'all' | ExpiryStatus; tag: string;
}
export const emptyFilters: DocFilters = { type: 'All', authority: 'All', fy: 'All', project: 'All', uploader: 'All', expiry: 'all', tag: '' };

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="mb-1.5 block text-sm font-medium text-ink-soft dark:text-slate-300">{label}</label>
      {children}
    </div>
  );
}

export function FiltersPanel({ value, onChange, options }: {
  value: DocFilters; onChange: (v: DocFilters) => void;
  options: { types: string[]; authorities: string[]; fys: string[]; projects: string[]; uploaders: string[] };
}) {
  const set = (patch: Partial<DocFilters>) => onChange({ ...value, ...patch });
  const sel = (vals: string[]) => ['All', ...vals];

  return (
    <Card className="p-5">
      <div className="mb-4 flex items-center gap-2">
        <SlidersHorizontal className="h-5 w-5 text-ink-soft" />
        <h3 className="text-base font-semibold text-ink dark:text-slate-100">Filters</h3>
      </div>
      <div className="space-y-4">
        <Field label="Document Type"><Select value={value.type} onChange={(e) => set({ type: e.target.value })}>{sel(options.types).map((t) => <option key={t}>{t}</option>)}</Select></Field>
        <Field label="Issuing Authority"><Select value={value.authority} onChange={(e) => set({ authority: e.target.value })}>{sel(options.authorities).map((t) => <option key={t}>{t}</option>)}</Select></Field>
        <Field label="Financial Year"><Select value={value.fy} onChange={(e) => set({ fy: e.target.value })}>{sel(options.fys).map((t) => <option key={t}>{t}</option>)}</Select></Field>
        <Field label="Project"><Select value={value.project} onChange={(e) => set({ project: e.target.value })}>{sel(options.projects).map((t) => <option key={t}>{t}</option>)}</Select></Field>
        <Field label="Uploader"><Select value={value.uploader} onChange={(e) => set({ uploader: e.target.value })}>{sel(options.uploaders).map((t) => <option key={t}>{t}</option>)}</Select></Field>
        <Field label="Expiry">
          <Segmented<'all' | ExpiryStatus>
            value={value.expiry}
            onChange={(v) => set({ expiry: v })}
            options={[
              { label: 'All', value: 'all' }, { label: 'Valid', value: 'valid' }, { label: 'Expiring', value: 'expiring' },
              { label: 'Expired', value: 'expired' }, { label: 'No Expiry', value: 'none' },
            ]}
          />
        </Field>
        <Field label="Tag"><Input placeholder="e.g., audit, urgent" value={value.tag} onChange={(e) => set({ tag: e.target.value })} /></Field>
        <button onClick={() => onChange(emptyFilters)} className="w-full pt-1 text-center text-sm font-medium text-ink-soft hover:text-ink dark:text-slate-300">Clear all filters</button>
      </div>
    </Card>
  );
}
