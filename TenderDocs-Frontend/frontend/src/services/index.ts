/**
 * Single composition point for data access.
 *
 * The app now runs against the real .NET 8 Web API. The live services implement the same
 * I*Service contracts the mock store defined, so no component changes are required.
 * The mock implementations remain in ./store and ./seed for offline/storybook use.
 */
export {
  documentService, projectService, folderService, notificationService, teamService,
} from './live';
export type {
  IDocumentService, IProjectService, IFolderService, INotificationService, ITeamService,
} from './store';

// Re-export the API clients + helpers for screens that need direct calls (e.g. ZIP download).
export * as apiClients from './api';
export { saveBlob, api, API_BASE_URL } from '@/config/api';
