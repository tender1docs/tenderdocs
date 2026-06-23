import { api } from '@/config/api';
import type { GlobalSearchResultDto } from './dtos';

export const SearchApi = {
  search: (term: string) =>
    api.get<GlobalSearchResultDto>(`/search?q=${encodeURIComponent(term)}`),
};
