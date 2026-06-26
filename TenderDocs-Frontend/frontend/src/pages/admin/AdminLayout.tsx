import { NavLink, Navigate, Outlet } from 'react-router-dom';
import {
  LayoutDashboard, Users, ShieldCheck, FolderKanban, HardDrive, ScrollText,
  CheckSquare, Bell, BarChart3, type LucideIcon,
} from 'lucide-react';
import { useAuth } from '@/auth/AuthProvider';
import { can, Permission } from '@/lib/access';
import { cn } from '@/lib/utils';

interface AdminNavItem { to: string; label: string; icon: LucideIcon; end?: boolean }

const items: AdminNavItem[] = [
  { to: '/admin', label: 'Dashboard', icon: LayoutDashboard, end: true },
  { to: '/admin/users', label: 'Users', icon: Users },
  { to: '/admin/roles', label: 'Roles', icon: ShieldCheck },
  { to: '/admin/access', label: 'Project Access', icon: FolderKanban },
  { to: '/admin/storage', label: 'Storage', icon: HardDrive },
  { to: '/admin/audit', label: 'Audit Logs', icon: ScrollText },
  { to: '/admin/approvals', label: 'Approval Queue', icon: CheckSquare },
  { to: '/admin/notifications', label: 'Notifications', icon: Bell },
  { to: '/admin/reports', label: 'Reports', icon: BarChart3 },
];

export default function AdminLayout() {
  const { permissions } = useAuth();
  if (!can(permissions, Permission.AdminAccess)) return <Navigate to="/dashboard" replace />;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-[26px] font-bold tracking-tight text-ink dark:text-slate-100">Administration</h1>
        <p className="mt-1 text-[15px] text-ink-soft dark:text-slate-300">
          Manage users, access and the workspace.
        </p>
      </div>

      <div className="flex flex-col gap-6 lg:flex-row">
        <nav className="flex gap-1 overflow-x-auto lg:w-56 lg:flex-col lg:overflow-visible">
          {items.map(({ to, label, icon: Icon, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
              className={({ isActive }) =>
                cn(
                  'flex shrink-0 items-center gap-2.5 rounded-xl px-3 py-2.5 text-[15px] font-medium transition-colors',
                  isActive
                    ? 'bg-brand-50 text-brand-700 dark:bg-brand-900/40 dark:text-brand-200'
                    : 'text-ink-soft hover:bg-slate-50 hover:text-ink dark:text-slate-300 dark:hover:bg-[#161D23]',
                )
              }
            >
              <Icon className="h-[18px] w-[18px] shrink-0" />
              <span className="flex-1 whitespace-nowrap">{label}</span>
            </NavLink>
          ))}
        </nav>

        <div className="min-w-0 flex-1">
          <Outlet />
        </div>
      </div>
    </div>
  );
}
