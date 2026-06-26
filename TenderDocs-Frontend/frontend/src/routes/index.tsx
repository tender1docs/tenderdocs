import { lazy, Suspense } from 'react';
import { createBrowserRouter, Navigate } from 'react-router-dom';
import { AppLayout } from '@/layouts/AppLayout';
import { Skeleton } from '@/components/ui';

const DashboardPage = lazy(() => import('@/pages/DashboardPage'));
const DocumentsPage = lazy(() => import('@/pages/DocumentsPage'));
const ProjectsPage = lazy(() => import('@/pages/ProjectsPage'));
const ProjectDetailPage = lazy(() => import('@/pages/ProjectDetailPage'));
const ProjectOrganizePage = lazy(() => import('@/pages/ProjectOrganizePage'));
const OrganizePage = lazy(() => import('@/pages/OrganizePage'));
const NotificationsPage = lazy(() => import('@/pages/NotificationsPage'));
const TeamPage = lazy(() => import('@/pages/TeamPage'));
const SettingsPage = lazy(() => import('@/pages/SettingsPage'));
const AdminLayout = lazy(() => import('@/pages/admin/AdminLayout'));
const AdminDashboardPage = lazy(() => import('@/pages/admin/AdminDashboardPage'));
const AdminUsersPage = lazy(() => import('@/pages/admin/AdminUsersPage'));
const AdminRolesPage = lazy(() => import('@/pages/admin/AdminRolesPage'));
const AdminProjectAccessPage = lazy(() => import('@/pages/admin/AdminProjectAccessPage'));
const AdminStoragePage = lazy(() => import('@/pages/admin/AdminStoragePage'));
const AdminAuditLogsPage = lazy(() => import('@/pages/admin/AdminAuditLogsPage'));
const AdminApprovalQueuePage = lazy(() => import('@/pages/admin/AdminApprovalQueuePage'));
const AdminNotificationsPage = lazy(() => import('@/pages/admin/AdminNotificationsPage'));
const AdminReportsPage = lazy(() => import('@/pages/admin/AdminReportsPage'));

function PageFallback() {
  return (
    <div className="space-y-4">
      <Skeleton className="h-9 w-64" />
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-28" />)}
      </div>
      <Skeleton className="h-72" />
    </div>
  );
}

const page = (el: React.ReactNode) => <Suspense fallback={<PageFallback />}>{el}</Suspense>;

export const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <Navigate to="/dashboard" replace /> },
      { path: 'dashboard', element: page(<DashboardPage />) },
      { path: 'documents', element: page(<DocumentsPage />) },
      { path: 'projects', element: page(<ProjectsPage />) },
      { path: 'projects/:id', element: page(<ProjectDetailPage />) },
      { path: 'projects/:id/organize', element: page(<ProjectOrganizePage />) },
      { path: 'organize', element: page(<OrganizePage />) },
      { path: 'notifications', element: page(<NotificationsPage />) },
      { path: 'team', element: page(<TeamPage />) },
      { path: 'settings', element: page(<SettingsPage />) },
      {
        path: 'admin',
        element: page(<AdminLayout />),
        children: [
          { index: true, element: page(<AdminDashboardPage />) },
          { path: 'users', element: page(<AdminUsersPage />) },
          { path: 'roles', element: page(<AdminRolesPage />) },
          { path: 'access', element: page(<AdminProjectAccessPage />) },
          { path: 'storage', element: page(<AdminStoragePage />) },
          { path: 'audit', element: page(<AdminAuditLogsPage />) },
          { path: 'approvals', element: page(<AdminApprovalQueuePage />) },
          { path: 'notifications', element: page(<AdminNotificationsPage />) },
          { path: 'reports', element: page(<AdminReportsPage />) },
        ],
      },
      { path: '*', element: <Navigate to="/dashboard" replace /> },
    ],
  },
]);
