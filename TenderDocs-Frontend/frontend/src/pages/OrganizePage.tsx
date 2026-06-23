import { useNavigate } from 'react-router-dom';
import { FolderKanban } from 'lucide-react';
import { useProjects } from '@/hooks';
import { Skeleton, EmptyState } from '@/components/ui';
import { ProjectPicker } from '@/features/organize/ProjectPicker';

/**
 * Organize entry point. Organize is now project-scoped: picking a project opens its
 * dedicated Organize workspace at /projects/:id/organize.
 */
export default function OrganizePage() {
  const navigate = useNavigate();
  const { data: projects, isLoading } = useProjects();

  if (isLoading) {
    return (
      <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
        {Array.from({ length: 6 }).map((_, i) => <Skeleton key={i} className="h-44" />)}
      </div>
    );
  }

  if (!projects?.length) {
    return (
      <EmptyState
        icon={<FolderKanban className="h-6 w-6" />}
        title="No projects yet"
        hint="Create a project first, then come back to map its documents."
      />
    );
  }

  return <ProjectPicker projects={projects} onSelect={(p) => navigate(`/projects/${p.id}/organize`)} />;
}
