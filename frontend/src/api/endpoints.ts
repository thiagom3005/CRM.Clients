import { http } from './httpClient';
import type {
  CustomerDetails,
  CustomerEvent,
  CustomerListItem,
  CreateCustomerRequest,
  PagedResult,
  Address,
} from './types';

export type SortOption = 'updatedAtDesc' | 'nameAsc';

export interface SearchParams {
  search?: string;
  page?: number;
  pageSize?: number;
  sort?: SortOption;
}

export const customersApi = {
  list: (params: SearchParams = {}) => {
    const qs = new URLSearchParams();
    if (params.search)   qs.set('search',   params.search);
    if (params.page)     qs.set('page',     String(params.page));
    if (params.pageSize) qs.set('pageSize', String(params.pageSize));
    if (params.sort)     qs.set('sort',     params.sort);
    const query = qs.size ? `?${qs}` : '';
    return http.get<PagedResult<CustomerListItem>>(`/customers${query}`);
  },

  get: (id: string) =>
    http.get<CustomerDetails>(`/customers/${id}`),

  create: (body: CreateCustomerRequest) =>
    http.post<{ id: string }>('/customers', body),

  events: (id: string, page = 1, pageSize = 20) =>
    http.get<PagedResult<CustomerEvent>>(
      `/customers/${id}/events?page=${page}&pageSize=${pageSize}`,
    ),

  updateEmail: (id: string, email: string) =>
    http.put<void>(`/customers/${id}/email`, { email }),

  updateAddress: (id: string, address: Address) =>
    http.put<void>(`/customers/${id}/address`, address),
};
