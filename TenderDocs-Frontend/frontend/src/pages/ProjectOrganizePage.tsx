import { useNavigate, useParams } from 'react-router-dom';
import { OrganizeWorkspace } from '@/features/organize/Workspace';

/** Organize scoped to a single project (opened from the project page). */
export default function ProjectOrganizePage() {
  const { id } = useParams();
  const navigate = useNavigate();
  if (!id) return null;
  return <OrganizeWorkspace projectId={id} onBack={() => navigate(`/projects/${id}`)} />;
}
