import { api } from '@/config/api';
import type { NotificationDto } from './dtos';

export const NotificationsApi = {
  list: (unreadOnly = false) =>
    api.get<NotificationDto[]>(`/notifications${unreadOnly ? '?unreadOnly=true' : ''}`),

  markRead: (id: string) => api.post<void>(`/notifications/${id}/read`),
  markAllRead: () => api.post<void>('/notifications/read-all'),
};
