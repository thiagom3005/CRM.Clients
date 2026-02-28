// Write side: toda criação passa por command para manter consistência do domínio.
import { http } from './httpClient';
import type { CustomerType } from './types';

// Espelha o record flat que o endpoint POST /customers espera.
export interface CreateCustomerCommand {
  type: CustomerType;
  name: string;
  document: string;
  birthOrFoundationDate: string; // YYYY-MM-DD (DateOnly no backend)
  email: string;
  phone: string;
  zipCode: string;
  street: string;
  number: string;
  district: string;
  city: string;
  state: string;
  stateRegistration?: string;
  isStateRegistrationExempt: boolean;
}

export interface ZipCodeAddress {
  zipCode: string;
  street: string;
  district: string;
  city: string;
  state: string;
}

// Retorna o Guid do cliente criado (corpo da resposta 201).
export function createCustomer(payload: CreateCustomerCommand): Promise<string> {
  return http.post<string>('/customers', payload);
}

// Integração externa melhora UX, mas não bloqueia o cadastro se falhar.
export function lookupZipCode(digits: string): Promise<ZipCodeAddress> {
  return http.get<ZipCodeAddress>(`/addresses/by-zipcode/${digits}`);
}
