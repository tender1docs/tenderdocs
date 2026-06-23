import { cva, type VariantProps } from 'class-variance-authority';
import { AnimatePresence, motion } from 'framer-motion';
import { CheckCircle2, AlertTriangle, XCircle, X, Loader2, Clock } from 'lucide-react';
import {
  forwardRef, useCallback, useEffect, useState, type ButtonHTMLAttributes,
  type InputHTMLAttributes, type ReactNode, type SelectHTMLAttributes,
} from 'react';
import { cn } from '@/lib/utils';
import { ToastContext, type Toast } from '@/hooks';
import type { ExpiryStatus, ApprovalStatus } from '@/types';

/* ---------------- Button ---------------- */
const buttonVariants = cva(
  'inline-flex items-center justify-center gap-2 font-semibold rounded-xl transition-all duration-150 disabled:opacity-50 disabled:pointer-events-none select-none active:scale-[.98]',
  {
    variants: {
      variant: {
        primary: 'bg-brand-600 text-white shadow-card hover:bg-brand-700',
        secondary: 'bg-white text-ink border border-line shadow-card hover:bg-slate-50 dark:bg-[#161D23] dark:text-slate-100 dark:border-[#222A31] dark:hover:bg-[#1B232A]',
        ghost: 'text-ink-soft hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-[#1B232A]',
        soft: 'bg-brand-50 text-brand-700 hover:bg-brand-100 dark:bg-brand-900/40 dark:text-brand-200',
        danger: 'bg-danger-bg text-danger-text hover:bg-red-100 dark:bg-red-950/40',
      },
      size: { sm: 'h-9 px-3 text-sm', md: 'h-10 px-4 text-sm', lg: 'h-11 px-5 text-[15px]' },
    },
    defaultVariants: { variant: 'primary', size: 'md' },
  },
);
export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement>, VariantProps<typeof buttonVariants> {
  loading?: boolean;
}
export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant, size, loading, children, disabled, ...props }, ref) => (
    <button ref={ref} className={cn(buttonVariants({ variant, size }), className)} disabled={disabled || loading} {...props}>
      {loading && <Loader2 className="h-4 w-4 animate-spin" />}
      {children}
    </button>
  ),
);
Button.displayName = 'Button';

export function IconButton({ className, children, ...props }: ButtonHTMLAttributes<HTMLButtonElement>) {
  return (
    <button
      className={cn('inline-flex h-9 w-9 items-center justify-center rounded-lg text-ink-muted transition-colors hover:bg-slate-100 hover:text-ink dark:hover:bg-[#1B232A]', className)}
      {...props}
    >
      {children}
    </button>
  );
}

/* ---------------- Card ---------------- */
export function Card({ className, children, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return <div className={cn('surface', className)} {...props}>{children}</div>;
}

/* ---------------- Badge ---------------- */
export function Badge({ className, children }: { className?: string; children: ReactNode }) {
  return (
    <span className={cn('inline-flex items-center rounded-full bg-slate-100 px-2.5 py-1 text-xs font-medium text-ink-soft dark:bg-[#1B232A] dark:text-slate-300', className)}>
      {children}
    </span>
  );
}

const statusMap: Record<ExpiryStatus, { label: string; cls: string; Icon: typeof CheckCircle2 }> = {
  valid:    { label: 'Valid',    cls: 'bg-valid-bg text-valid-text ring-1 ring-inset ring-valid-ring',   Icon: CheckCircle2 },
  expiring: { label: 'Expiring', cls: 'bg-warn-bg text-warn-text ring-1 ring-inset ring-warn-ring',     Icon: AlertTriangle },
  expired:  { label: 'Expired',  cls: 'bg-danger-bg text-danger-text ring-1 ring-inset ring-danger-ring', Icon: XCircle },
  none:     { label: 'No Expiry', cls: 'bg-slate-100 text-ink-muted ring-1 ring-inset ring-slate-200 dark:bg-[#1B232A] dark:ring-[#222A31]', Icon: CheckCircle2 },
};
export function StatusBadge({ status }: { status: ExpiryStatus }) {
  const { label, cls, Icon } = statusMap[status];
  return (
    <span className={cn('inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium', cls)}>
      <Icon className="h-3.5 w-3.5" /> {label}
    </span>
  );
}

const approvalMap: Record<ApprovalStatus, { label: string; cls: string; Icon: typeof CheckCircle2 }> = {
  pending:  { label: 'Pending',  cls: 'bg-warn-bg text-warn-text ring-1 ring-inset ring-warn-ring',       Icon: Clock },
  approved: { label: 'Approved', cls: 'bg-valid-bg text-valid-text ring-1 ring-inset ring-valid-ring',     Icon: CheckCircle2 },
  rejected: { label: 'Rejected', cls: 'bg-danger-bg text-danger-text ring-1 ring-inset ring-danger-ring',  Icon: XCircle },
};
export function ApprovalBadge({ status, className }: { status: ApprovalStatus; className?: string }) {
  const { label, cls, Icon } = approvalMap[status];
  return (
    <span className={cn('inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium', cls, className)}>
      <Icon className="h-3.5 w-3.5" /> {label}
    </span>
  );
}

/* ---------------- Input / Select ---------------- */
export const Input = forwardRef<HTMLInputElement, InputHTMLAttributes<HTMLInputElement>>(
  ({ className, ...props }, ref) => (
    <input
      ref={ref}
      className={cn('h-10 w-full rounded-xl border border-line bg-white px-3.5 text-sm text-ink placeholder:text-ink-faint transition-colors focus:border-brand-400 focus:ring-2 focus:ring-brand-100 dark:bg-[#12181D] dark:border-[#222A31] dark:text-slate-100 dark:focus:ring-brand-900/40', className)}
      {...props}
    />
  ),
);
Input.displayName = 'Input';

export const Select = forwardRef<HTMLSelectElement, SelectHTMLAttributes<HTMLSelectElement>>(
  ({ className, children, ...props }, ref) => (
    <div className="relative">
      <select
        ref={ref}
        className={cn('h-10 w-full appearance-none rounded-xl border border-line bg-white px-3.5 pr-9 text-sm text-ink transition-colors focus:border-brand-400 focus:ring-2 focus:ring-brand-100 dark:bg-[#12181D] dark:border-[#222A31] dark:text-slate-100', className)}
        {...props}
      >
        {children}
      </select>
      <svg className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-faint" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="m6 9 6 6 6-6" /></svg>
    </div>
  ),
);
Select.displayName = 'Select';

/* ---------------- Segmented control ---------------- */
export function Segmented<T extends string>({ options, value, onChange }: {
  options: { label: string; value: T }[]; value: T; onChange: (v: T) => void;
}) {
  return (
    <div className="flex flex-wrap gap-1.5">
      {options.map((o) => (
        <button
          key={o.value}
          onClick={() => onChange(o.value)}
          className={cn(
            'rounded-lg px-3 py-1.5 text-sm font-medium transition-colors',
            value === o.value ? 'bg-brand-600 text-white' : 'bg-slate-100 text-ink-soft hover:bg-slate-200 dark:bg-[#1B232A] dark:text-slate-300',
          )}
        >
          {o.label}
        </button>
      ))}
    </div>
  );
}

/* ---------------- Modal ---------------- */
export function Modal({ open, onClose, title, children, width = 'max-w-md' }: {
  open: boolean; onClose: () => void; title?: string; children: ReactNode; width?: string;
}) {
  useEffect(() => {
    if (!open) return;
    const onEsc = (e: KeyboardEvent) => e.key === 'Escape' && onClose();
    window.addEventListener('keydown', onEsc);
    return () => window.removeEventListener('keydown', onEsc);
  }, [open, onClose]);
  return (
    <AnimatePresence>
      {open && (
        <motion.div className="fixed inset-0 z-50 flex items-center justify-center p-4"
          initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}>
          <div className="absolute inset-0 bg-slate-900/40 backdrop-blur-sm" onClick={onClose} />
          <motion.div
            className={cn('relative w-full rounded-2xl bg-white p-6 shadow-lift dark:bg-[#12181D] dark:border dark:border-[#222A31]', width)}
            initial={{ opacity: 0, y: 16, scale: 0.98 }} animate={{ opacity: 1, y: 0, scale: 1 }} exit={{ opacity: 0, y: 8, scale: 0.98 }}
            transition={{ type: 'spring', stiffness: 380, damping: 30 }}>
            {title && (
              <div className="mb-4 flex items-center justify-between">
                <h3 className="text-lg font-semibold text-ink dark:text-slate-100">{title}</h3>
                <IconButton onClick={onClose}><X className="h-5 w-5" /></IconButton>
              </div>
            )}
            {children}
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}

/* ---------------- Skeleton ---------------- */
export function Skeleton({ className }: { className?: string }) {
  return <div className={cn('animate-pulse rounded-md bg-slate-200/70 dark:bg-[#1B232A]', className)} />;
}

/* ---------------- Empty state ---------------- */
export function EmptyState({ icon, title, hint, action }: {
  icon: ReactNode; title: string; hint?: string; action?: ReactNode;
}) {
  return (
    <div className="flex flex-col items-center justify-center rounded-2xl border border-dashed border-line py-16 text-center dark:border-[#222A31]">
      <div className="mb-4 flex h-14 w-14 items-center justify-center rounded-2xl bg-brand-50 text-brand-600 dark:bg-brand-900/40 dark:text-brand-200">{icon}</div>
      <p className="text-base font-semibold text-ink dark:text-slate-100">{title}</p>
      {hint && <p className="mt-1 max-w-sm text-sm text-ink-muted">{hint}</p>}
      {action && <div className="mt-5">{action}</div>}
    </div>
  );
}

/* ---------------- Avatar ---------------- */
export function Avatar({ initials, className }: { initials: string; className?: string }) {
  return (
    <div className={cn('flex h-9 w-9 items-center justify-center rounded-full bg-slate-100 text-sm font-semibold text-ink-soft dark:bg-[#1B232A] dark:text-slate-200', className)}>
      {initials}
    </div>
  );
}

/* ---------------- Toast provider ---------------- */
export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);
  const dismiss = useCallback((id: number) => setToasts((t) => t.filter((x) => x.id !== id)), []);
  const push = useCallback((t: Omit<Toast, 'id'>) => {
    const id = Date.now() + Math.random();
    setToasts((cur) => [...cur, { ...t, id }]);
    setTimeout(() => dismiss(id), 3200);
  }, [dismiss]);
  return (
    <ToastContext.Provider value={{ toasts, push, dismiss }}>
      {children}
      <div className="pointer-events-none fixed bottom-5 right-5 z-[60] flex w-80 flex-col gap-2">
        <AnimatePresence>
          {toasts.map((t) => (
            <motion.div key={t.id}
              initial={{ opacity: 0, x: 40, scale: 0.95 }} animate={{ opacity: 1, x: 0, scale: 1 }} exit={{ opacity: 0, x: 40 }}
              className={cn('pointer-events-auto flex items-start gap-2.5 rounded-xl border bg-white px-4 py-3 shadow-lift dark:bg-[#12181D]',
                t.tone === 'success' ? 'border-valid-ring' : t.tone === 'danger' ? 'border-danger-ring' : 'border-line dark:border-[#222A31]')}>
              {t.tone === 'success' && <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0 text-valid-text" />}
              {t.tone === 'danger' && <XCircle className="mt-0.5 h-4 w-4 shrink-0 text-danger-text" />}
              <span className="text-sm font-medium text-ink dark:text-slate-100">{t.title}</span>
            </motion.div>
          ))}
        </AnimatePresence>
      </div>
    </ToastContext.Provider>
  );
}
