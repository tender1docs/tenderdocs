import { NavLink } from "react-router-dom";
import {
  LayoutDashboard,
  FileText,
  FolderKanban,
  Settings,
  ShieldCheck,
  type LucideIcon,
} from "lucide-react";
import { Logo } from "./Logo";
import { cn } from "@/lib/utils";
import { useAuth } from "@/auth/AuthProvider";
import { can, Permission } from "@/lib/access";

interface NavItem {
  to: string;
  label: string;
  icon: LucideIcon;
  permission?: string; // when set, the item shows only if the user holds this permission
}

const nav: NavItem[] = [
  { to: "/dashboard", label: "Overview", icon: LayoutDashboard },
  { to: "/documents", label: "Documents", icon: FileText },
  { to: "/projects", label: "Projects", icon: FolderKanban },
  { to: "/settings", label: "Settings", icon: Settings },
  { to: "/admin", label: "Administration", icon: ShieldCheck, permission: Permission.AdminAccess },
];

export function Sidebar({ onNavigate }: { onNavigate?: () => void }) {
  const { permissions } = useAuth();
  const visibleNav = nav.filter((item) => !item.permission || can(permissions, item.permission));

  return (
    <aside className="flex h-full w-64 flex-col border-r border-line bg-white dark:border-[#1A2127] dark:bg-[#0E1317]">
      <div className="px-5 pt-5 pb-4">
        <NavLink
          to="/dashboard"
          onClick={onNavigate}
          aria-label="Go to Overview"
          className="inline-block rounded-xl transition-opacity hover:opacity-80 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500"
        >
          <Logo />
        </NavLink>
      </div>

      <nav className="flex-1 space-y-1 px-3">
        {visibleNav.map(({ to, label, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            onClick={onNavigate}
            className={({ isActive }) =>
              cn(
                "group flex items-center gap-3 rounded-xl px-3 py-2.5 text-[15px] font-medium transition-colors",
                isActive
                  ? "bg-brand-50 text-brand-700 dark:bg-brand-900/40 dark:text-brand-200"
                  : "text-ink-soft hover:bg-slate-50 hover:text-ink dark:text-slate-300 dark:hover:bg-[#161D23]",
              )
            }
          >
            {({ isActive }) => (
              <>
                <Icon
                  className={cn(
                    "h-5 w-5 shrink-0",
                    isActive
                      ? "text-brand-600 dark:text-brand-300"
                      : "text-ink-muted",
                  )}
                />
                <span className="flex-1">{label}</span>
              </>
            )}
          </NavLink>
        ))}
      </nav>
    </aside>
  );
}
