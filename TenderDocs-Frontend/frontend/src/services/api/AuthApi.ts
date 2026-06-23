import { api } from '@/config/api';
import type { AuthResultDto, UserDto } from './dtos';

export const AuthApi = {
  login: (email: string, password: string) =>
    api.post<AuthResultDto>('/auth/login', { email, password }),

  register: (email: string, password: string, fullName: string, organizationName?: string) =>
    api.post<AuthResultDto>('/auth/register', { email, password, fullName, organizationName }),

  refresh: (refreshToken: string) =>
    api.post<AuthResultDto>('/auth/refresh', { refreshToken }),

  google: (idToken: string) =>
    api.post<AuthResultDto>('/auth/google', { idToken }),

  me: () => api.get<UserDto>('/auth/me'),

  updateProfile: (fullName: string) => api.put<UserDto>('/auth/me', { fullName }),
};
