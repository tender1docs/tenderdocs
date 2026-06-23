import { NavLink } from "react-router-dom";
import {
  LayoutDashboard,
  FileText,
  FolderKanban,
  Bell,
  Users,
  Settings,
  Slash,
} from "lucide-react";
import { Logo } from "./Logo";
import { cn } from "@/lib/utils";
import { useNotifications } from "@/hooks";
import { useAuth } from "@/auth/AuthProvider";
import { pagesFor } from "@/lib/access";

const nav = [
  { to: "/dashboard", label: "Dashboard", icon: LayoutDashboard },
  { to: "/documents", label: "Documents", icon: FileText },
  { to: "/projects", label: "Projects", icon: FolderKanban },
  // { to: "/notifications", label: "Notifications", icon: Bell },
  // { to: '/team', label: 'Team', icon: Users },
  { to: "/settings", label: "Settings", icon: Settings },
];

export function Sidebar({ onNavigate }: { onNavigate?: () => void }) {
  const { data: notifications } = useNotifications();
  const unread = notifications?.filter((n) => !n.read).length ?? 0;
  const { role } = useAuth();
  const allowed = role ? pagesFor(role) : [];
  const visibleNav = nav.filter((item) => allowed.includes(item.to));

  return (
    <aside className="flex h-full w-64 flex-col border-r border-line bg-white dark:border-[#1A2127] dark:bg-[#0E1317]">
      <div className="px-5 pt-5 pb-4">
        <Logo />
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
                {label === "Notifications" && unread > 0 && (
                  <span className="flex h-5 min-w-5 items-center justify-center rounded-full bg-brand-600 px-1.5 text-[11px] font-semibold text-white">
                    {unread}
                  </span>
                )}
              </>
            )}
          </NavLink>
        ))}
      </nav>

      <div className="p-3">
        {/* <div className="rounded-2xl border border-line bg-slate-50/70 p-4 dark:border-[#1A2127] dark:bg-[#12181D]"> */}
        {/* <div className="flex items-center gap-2 text-ink-soft dark:text-slate-200">
            {/* <Slash className="h-4 w-4 -rotate-12 text-ink-muted" /> */}
        {/* <span className="text-sm font-semibold">Demo Mode</span> */}
        {/* </div> */}
        {/* <p className="mt-1.5 text-xs leading-relaxed text-ink-muted">
            Files stored locally. Connect Google Drive in Settings.
          </p> */}
        {/* </div> */}
      </div>
    </aside>
  );
}
