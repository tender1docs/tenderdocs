import { useMemo } from "react";
import { Link } from "react-router-dom";
import { motion } from "framer-motion";
import {
  FileText,
  CheckCircle2,
  AlertTriangle,
  XCircle,
  ArrowRight,
  Slash,
  FolderKanban,
  ChevronRight,
  FileType2,
} from "lucide-react";
import { Card, StatusBadge, Button, Skeleton } from "@/components/ui";
import {
  DocumentsByTypeChart,
  TypeDistributionChart,
} from "@/features/dashboard/Charts";
import { useDocuments, useProjects, useMe } from "@/hooks";
import { fmtDayMonth, pluralize } from "@/lib/utils";

const metricMeta = [
  {
    key: "total",
    label: "Total Documents",
    Icon: FileText,
    tint: "text-brand-600 bg-brand-50 dark:bg-brand-900/40",
  },
  {
    key: "valid",
    label: "Valid",
    Icon: CheckCircle2,
    tint: "text-valid-text bg-valid-bg",
  },
  {
    key: "expiring",
    label: "Expiring Soon",
    Icon: AlertTriangle,
    tint: "text-warn-text bg-warn-bg",
  },
  {
    key: "expired",
    label: "Expired",
    Icon: XCircle,
    tint: "text-danger-text bg-danger-bg",
  },
] as const;

export default function DashboardPage() {
  const { data: documents, isLoading } = useDocuments();
  const { data: projects } = useProjects();
  const { data: me } = useMe();

  const metrics = useMemo(() => {
    const docs = documents ?? [];
    return {
      total: docs.length,
      valid: docs.filter((d) => d.status === "valid").length,
      expiring: docs.filter((d) => d.status === "expiring").length,
      expired: docs.filter((d) => d.status === "expired").length,
    };
  }, [documents]);

  const byType = useMemo(() => {
    const map = new Map<string, number>();
    (documents ?? []).forEach((d) =>
      map.set(d.type, (map.get(d.type) ?? 0) + 1),
    );
    return [...map.entries()]
      .map(([type, count]) => ({ type, count }))
      .sort((a, b) => b.count - a.count);
  }, [documents]);

  const expiring = (documents ?? []).filter(
    (d) => d.status === "expiring" || d.status === "expired",
  );
  const recent = [...(documents ?? [])]
    .sort((a, b) => +new Date(b.uploadedAt) - +new Date(a.uploadedAt))
    .slice(0, 4);

  return (
    <div className="space-y-6">
      {/* Hero */}
      <motion.div
        initial={{ opacity: 0, y: 8 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.4 }}
        className="flex flex-col gap-4 rounded-2xl border border-line bg-gradient-to-br from-hero-from to-hero-to p-7 dark:border-[#1A2127] dark:from-[#10211E] dark:to-[#0E1A18] sm:flex-row sm:items-center sm:justify-between"
      >
        <div>
          <h1 className="text-[28px] font-bold tracking-tight text-ink dark:text-slate-100">
            Hello {me?.name ?? "there"} 👋
          </h1>
          <p className="mt-1 text-[15px] text-ink-soft dark:text-slate-300">
            Here's a snapshot of your tender document workspace.
          </p>
        </div>
        <div className="flex items-center gap-4">
          {/* <span className="flex items-center gap-1.5 text-sm font-medium text-ink-soft dark:text-slate-300"><Slash className="h-4 w-4 -rotate-12 text-ink-muted" /> Demo Mode</span> */}
          <Link to="/settings">
            <Button variant="secondary" size="sm">
              Settings
            </Button>
          </Link>
        </div>
      </motion.div>

      {/* Metrics */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        {metricMeta.map((m, i) => (
          <motion.div
            key={m.key}
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.05 * i }}
          >
            <Card className="p-5">
              <div className="flex items-start justify-between">
                <span className="text-sm font-medium text-ink-soft dark:text-slate-300">
                  {m.label}
                </span>
                <span
                  className={`flex h-9 w-9 items-center justify-center rounded-full ${m.tint}`}
                >
                  <m.Icon className="h-5 w-5" />
                </span>
              </div>
              {isLoading ? (
                <Skeleton className="mt-4 h-9 w-12" />
              ) : (
                <p className="mt-3 text-4xl font-bold tracking-tight text-ink dark:text-slate-100">
                  {metrics[m.key]}
                </p>
              )}
            </Card>
          </motion.div>
        ))}
      </div>

      {/* Charts */}
      <div className="grid grid-cols-1 gap-5 lg:grid-cols-3">
        <Card className="p-6 lg:col-span-2">
          <div className="mb-2 flex items-center justify-between">
            <h2 className="text-lg font-semibold text-ink dark:text-slate-100">
              Documents by Type
            </h2>
            <Link
              to="/documents"
              className="inline-flex items-center gap-1 text-sm font-medium text-brand-600 hover:text-brand-700"
            >
              View all <ArrowRight className="h-4 w-4" />
            </Link>
          </div>
          {byType.length ? (
            <DocumentsByTypeChart data={byType.slice(0, 7)} />
          ) : (
            <div className="flex h-[260px] items-center justify-center text-sm text-ink-muted">
              No documents yet
            </div>
          )}
        </Card>
        <Card className="p-6">
          <h2 className="mb-2 text-lg font-semibold text-ink dark:text-slate-100">
            Type Distribution
          </h2>
          {byType.length ? (
            <TypeDistributionChart data={byType} />
          ) : (
            <div className="flex h-[260px] items-center justify-center text-sm text-ink-muted">
              No data
            </div>
          )}
        </Card>
      </div>

      {/* Expiring + Projects */}
      <div className="grid grid-cols-1 gap-5 lg:grid-cols-2">
        <Card className="p-6">
          <div className="mb-4 flex items-center justify-between">
            <h2 className="flex items-center gap-2 text-lg font-semibold text-ink dark:text-slate-100">
              <AlertTriangle className="h-5 w-5 text-warn-text" /> Expiring /
              Expired
            </h2>
            <Link
              to="/documents"
              className="inline-flex items-center gap-1 text-sm font-medium text-brand-600 hover:text-brand-700"
            >
              View all <ArrowRight className="h-4 w-4" />
            </Link>
          </div>
          {expiring.length === 0 ? (
            <p className="py-12 text-center text-sm text-ink-muted">
              No documents expiring soon. 🎉
            </p>
          ) : (
            <ul className="space-y-2">
              {expiring.map((d) => (
                <li
                  key={d.id}
                  className="flex items-center justify-between rounded-xl border border-line px-3.5 py-3 dark:border-[#1F262C]"
                >
                  <span className="flex items-center gap-3 min-w-0">
                    <FileType2 className="h-5 w-5 shrink-0 text-pdf" />
                    <span className="min-w-0">
                      <span className="block truncate text-sm font-medium text-ink dark:text-slate-100">
                        {d.name}
                      </span>
                      <span className="block text-xs text-ink-muted">
                        {d.type}
                      </span>
                    </span>
                  </span>
                  <StatusBadge status={d.status} />
                </li>
              ))}
            </ul>
          )}
        </Card>

        <Card className="p-6">
          <div className="mb-4 flex items-center justify-between">
            <h2 className="flex items-center gap-2 text-lg font-semibold text-ink dark:text-slate-100">
              <FolderKanban className="h-5 w-5 text-brand-600" /> Projects
            </h2>
            <Link
              to="/projects"
              className="inline-flex items-center gap-1 text-sm font-medium text-brand-600 hover:text-brand-700"
            >
              All projects <ArrowRight className="h-4 w-4" />
            </Link>
          </div>
          <ul className="space-y-2">
            {(projects ?? []).slice(0, 3).map((p) => (
              <li key={p.id}>
                <Link
                  to={`/projects/${p.id}`}
                  className="flex items-center gap-3 rounded-xl border border-line px-3.5 py-3 transition-colors hover:border-brand-200 hover:bg-brand-50/40 dark:border-[#1F262C]"
                >
                  <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-slate-100 text-ink-soft dark:bg-[#1B232A]">
                    <FolderKanban className="h-5 w-5" />
                  </span>
                  <span className="min-w-0 flex-1">
                    <span className="block truncate text-sm font-semibold text-ink dark:text-slate-100">
                      {p.name}
                    </span>
                    <span className="block text-xs text-ink-muted">
                      {pluralize(p.documentIds.length, "document")}
                    </span>
                  </span>
                  <ChevronRight className="h-4 w-4 text-ink-faint" />
                </Link>
              </li>
            ))}
          </ul>
        </Card>
      </div>

      {/* Recent uploads */}
      <Card className="p-6">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-ink dark:text-slate-100">
            Recent Uploads
          </h2>
          <Link
            to="/documents"
            className="inline-flex items-center gap-1 text-sm font-medium text-brand-600 hover:text-brand-700"
          >
            View all <ArrowRight className="h-4 w-4" />
          </Link>
        </div>
        <ul className="divide-y divide-line dark:divide-[#1F262C]">
          {recent.map((d) => (
            <li key={d.id} className="flex items-center justify-between py-3">
              <span className="flex items-center gap-3 min-w-0">
                <FileType2 className="h-5 w-5 shrink-0 text-pdf" />
                <span className="min-w-0">
                  <span className="block truncate text-sm font-medium text-ink dark:text-slate-100">
                    {d.name}
                  </span>
                  <span className="block text-xs text-ink-muted">
                    {d.type} • {d.authority} • {d.financialYear}
                  </span>
                </span>
              </span>
              <StatusBadge status={d.status} />
            </li>
          ))}
        </ul>
      </Card>
    </div>
  );
}
