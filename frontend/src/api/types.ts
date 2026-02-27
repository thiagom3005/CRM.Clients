// Tipos espelham os contratos da API — manter sincronizado com o backend.

export type CustomerType = 'Individual' | 'Company';

export interface Address {
  zipCode: string;
  street: string;
  number: string;
  district: string;
  city: string;
  state: string;
}

export interface CustomerListItem {
  id: string;
  name: string;
  document: string;
  email: string;
  type: CustomerType;
  city?: string;
  state?: string;
  updatedAtUtc: string;
}

export interface CustomerDetails {
  id: string;
  name: string;
  document: string;
  type: CustomerType;
  birthOrFoundationDate: string;
  email: string;
  phone: string;
  address: Address;
  stateRegistration: string | null;
  isStateRegistrationExempt: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CustomerEvent {
  eventId: string;
  version: number;
  eventType: string;
  data: Record<string, unknown>;
  userId: string | null;
  correlationId: string | null;
  occurredAtUtc: string;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface CreateCustomerRequest {
  name: string;
  document: string;
  type: CustomerType;
  birthOrFoundationDate: string;
  email: string;
  phone: string;
  address: Address;
  stateRegistration?: string;
  isStateRegistrationExempt?: boolean;
}
