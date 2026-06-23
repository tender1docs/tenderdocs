export function Logo() {
  return (
    <div className="flex items-center gap-3">
      <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-brand-500 to-brand-700 text-sm font-bold text-white shadow-card">
        TD
      </div>
      <div className="leading-tight">
        <p className="text-[15px] font-bold tracking-tight text-ink dark:text-slate-100">TenderDocs</p>
        <p className="text-xs text-ink-muted">Document Manager</p>
      </div>
    </div>
  );
}
