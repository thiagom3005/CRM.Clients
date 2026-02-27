# CRM.Clients — Frontend

React 19 + TypeScript + Vite. Consome a API REST em `http://localhost:8080`.

## Requisitos

- Node 20+

## Instalação

```bash
npm install
```

## Desenvolvimento

```bash
# Sobe a API primeiro (veja /docker ou /scripts na raiz do monorepo)
npm run dev
```

Abre em `http://localhost:3000`.

## Variáveis de ambiente

Copie `.env.example` para `.env.local` e ajuste se necessário:

```bash
cp .env.example .env.local
```

| Variável | Padrão |
|---|---|
| `VITE_API_URL` | `http://localhost:8080` |

## Scripts

| Comando | O que faz |
|---|---|
| `npm run dev` | Servidor de dev com HMR na porta 3000 |
| `npm run build` | Build de produção para `dist/` |
| `npm run preview` | Preview do build local |
| `npm run lint` | ESLint em todos os arquivos TS/TSX |

## Estrutura

```
src/
  api/          # httpClient, endpoints e tipos da API
  app/          # App.tsx e router
  components/   # Layout, PageContainer, Loading, ErrorMessage
  lib/          # env.ts e utilitários compartilhados
  pages/        # Uma página por rota
  styles/       # global.css (reset + tokens)
  main.tsx      # Entry point
```
