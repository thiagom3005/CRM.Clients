// Centraliza chamadas HTTP para manter páginas simples.
import { env } from '../lib/env';

export class HttpError extends Error {
  readonly status: number;

  constructor(status: number, message: string) {
    super(message);
    this.name = 'HttpError';
    this.status = status;
  }
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const url = `${env.apiUrl}${path}`;

  const response = await fetch(url, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      // X-Correlation-Id facilita rastreio de chamadas nos logs da API.
      'X-Correlation-Id': crypto.randomUUID(),
      ...options.headers,
    },
  });

  if (!response.ok) {
    const text = await response.text().catch(() => response.statusText);
    throw new HttpError(response.status, text);
  }

  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

export const http = {
  get:    <T>(path: string, options?: RequestInit) => request<T>(path, options),
  post:   <T>(path: string, body: unknown)   => request<T>(path, { method: 'POST',   body: JSON.stringify(body) }),
  put:    <T>(path: string, body: unknown)   => request<T>(path, { method: 'PUT',    body: JSON.stringify(body) }),
  delete: <T>(path: string)                  => request<T>(path, { method: 'DELETE' }),
};
