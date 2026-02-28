// Read model atende tela direto; aqui evitamos "adivinhar" estado do aggregate.
import { http } from './httpClient';
import type { CustomerDetails, CustomerEvent, PagedResult } from './types';

export function getCustomerById(id: string, signal?: AbortSignal): Promise<CustomerDetails> {
  return http.get<CustomerDetails>(`/customers/${id}`, { signal });
}

export function getCustomerEvents(
  id: string,
  page: number,
  pageSize: number,
  signal?: AbortSignal,
): Promise<PagedResult<CustomerEvent>> {
  const qs = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  return http.get<PagedResult<CustomerEvent>>(`/customers/${id}/events?${qs}`, { signal });
}
