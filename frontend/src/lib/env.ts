// Evita acesso direto a import.meta.env espalhado pelo código.
export const env = {
  apiUrl: import.meta.env.VITE_API_URL ?? 'http://localhost:8080',
} as const;
