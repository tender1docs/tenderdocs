import { createContext, useCallback, useContext, useRef, useState, type ReactNode } from 'react';
import { Modal, Button } from '@/components/ui';

export type ConfirmOptions = {
  title: string;
  message?: ReactNode;
  confirmText?: string;
  cancelText?: string;
  tone?: 'default' | 'danger';
};

type ConfirmFn = (opts: ConfirmOptions) => Promise<boolean>;

const ConfirmContext = createContext<ConfirmFn>(async () => false);

/** `const confirm = useConfirm(); if (await confirm({ title })) { … }` */
export const useConfirm = () => useContext(ConfirmContext);

/**
 * App-wide Yes/No confirmation. Wrap the app once; any component can then call
 * `await confirm({...})` which resolves true (Yes) or false (No / dismiss).
 */
export function ConfirmProvider({ children }: { children: ReactNode }) {
  const [opts, setOpts] = useState<ConfirmOptions | null>(null);
  const resolver = useRef<((v: boolean) => void) | null>(null);

  const confirm = useCallback<ConfirmFn>((o) => {
    setOpts(o);
    return new Promise<boolean>((resolve) => { resolver.current = resolve; });
  }, []);

  const settle = useCallback((value: boolean) => {
    resolver.current?.(value);
    resolver.current = null;
    setOpts(null);
  }, []);

  return (
    <ConfirmContext.Provider value={confirm}>
      {children}
      <Modal open={!!opts} onClose={() => settle(false)} title={opts?.title} width="max-w-sm">
        {opts?.message && (
          <p className="text-sm text-ink-soft dark:text-slate-300">{opts.message}</p>
        )}
        <div className="mt-6 flex justify-end gap-2">
          <Button variant="secondary" onClick={() => settle(false)}>{opts?.cancelText ?? 'No'}</Button>
          <Button variant={opts?.tone === 'danger' ? 'danger' : 'primary'} onClick={() => settle(true)}>
            {opts?.confirmText ?? 'Yes'}
          </Button>
        </div>
      </Modal>
    </ConfirmContext.Provider>
  );
}
