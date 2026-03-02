# CRM.Clients — Módulo de Clientes

[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
[![React 19](https://img.shields.io/badge/React-19-61DAFB?style=flat&logo=react&logoColor=white)](https://react.dev)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.9-3178C6?style=flat&logo=typescript&logoColor=white)](https://www.typescriptlang.org)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?style=flat&logo=postgresql&logoColor=white)](https://postgresql.org)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=flat&logo=docker&logoColor=white)](https://docs.docker.com/compose/)
[![CQRS](https://img.shields.io/badge/Pattern-CQRS-orange?style=flat)](docs/adr/ADR-002-cqrs.md)
[![Event Sourcing](https://img.shields.io/badge/Pattern-Event%20Sourcing-green?style=flat)](docs/adr/ADR-001-event-sourcing.md)

---

## 🎯 Objetivo

O módulo gerencia o ciclo de vida de clientes corporativos — Pessoa Física e Jurídica —
com validações de domínio ricas: CPF/CNPJ, idade mínima de 18 anos para PF e
regras de IE/isenção para PJ.

O foco arquitetural é demonstrar DDD + CQRS + Event Sourcing de forma prática e auditável:
cada criação ou alteração gera um evento persistido no banco, e o frontend
consome tanto o read model otimizado quanto o histórico bruto de eventos via API.

---

## 🔎 Como Avaliar o Projeto

Percurso sugerido para ver tudo funcionando em menos de 5 minutos:

**1. Subir API e banco**
```bash
cd docker && docker compose up --build
```
Aguardar a mensagem `CRM.Clients iniciado` nos logs da API.

**2. Subir o frontend** (terminal separado)
```bash
cd frontend && npm install && npm run dev
```

**3. Criar um cliente**
→ Formulário: http://localhost:3000 → *Novo cliente*
→ Ou via Swagger: http://localhost:8080/swagger → `POST /customers`

**4. Ver o cliente na lista** → http://localhost:3000/customers

**5. Abrir o detalhe** clicando no nome do cliente na tabela.

**6. Abrir a auditoria de eventos** → clicar em "Auditoria de eventos" no detalhe.
Expandir o payload do evento `CustomerCreated` para ver todos os dados gravados.

**7. Conferir os health checks**
```
http://localhost:8080/health/live   → Healthy (processo vivo)
http://localhost:8080/health/ready  → Healthy (banco acessível)
```

---

## 🌐 URLs Locais

| Serviço         | URL                                    |
|-----------------|----------------------------------------|
| Frontend        | http://localhost:3000                  |
| API             | http://localhost:8080                  |
| Swagger         | http://localhost:8080/swagger          |
| Health — live   | http://localhost:8080/health/live      |
| Health — ready  | http://localhost:8080/health/ready     |
| Postgres        | localhost:5432 / db: crm_clients       |

> `/health/live` confirma que o processo está de pé, sem checar dependências.
> `/health/ready` verifica o Postgres — usado por orquestradores antes de rotear tráfego.

---

## 🧠 Arquitetura

### Fluxo: Command → Aggregate → Event Store → Projection → Query → UI

```
POST /customers
     │
     ├─[FluentValidation] ──────────── formato inválido   → 400
     ├─[Unicidade]  ────────────────── CPF ou e-mail dup. → 409
     │
     ├─[Aggregate] ─────────────────── invariante violada → 400
     │   Customer.CreateIndividual()
     │   Customer.CreateCompany()
     │              │ DomainEvent: CustomerCreated
     │              ▼
     └─[Transação única] ── SaveChangesAsync()
          ├─ EventStore  →  tabela events            (imutável, append-only)
          └─ Projection  →  customer_read_model      (denormalizado, pesquisável)

GET /customers/{id}          →  lê customer_read_model  (sem joins)
GET /customers/{id}/events   →  lê events               (histórico completo)
                                          │
                                          ▼
                                   Frontend React
                              lista · detalhe · auditoria
```

**Evento e projeção estão na mesma transação** — nunca há leitura inconsistente.

### Camadas

| Camada          | Responsabilidade                                                 |
|-----------------|------------------------------------------------------------------|
| Domain          | Invariantes de negócio, value objects, domain events             |
| Application     | Orquestração: unicidade → aggregate → eventos → projeção         |
| Infrastructure  | EF Core, event store, read model, ViaCEP client                  |
| API             | Roteamento, serialização, middlewares de cross-cutting           |
| Frontend        | React 19 + Vite · lista · detalhe · auditoria · cadastro         |

---

## 📡 Endpoints

| Método | Rota                              | Descrição                               |
|--------|-----------------------------------|-----------------------------------------|
| POST   | /customers                        | Cria cliente (PF ou PJ)                 |
| GET    | /customers/{id}                   | Detalhe completo                        |
| GET    | /customers                        | Busca paginada por nome, e-mail, CPF    |
| GET    | /customers/{id}/events            | Histórico paginado de eventos           |
| PUT    | /customers/{id}/email             | Altera e-mail                           |
| PUT    | /customers/{id}/address           | Atualiza endereço                       |
| GET    | /addresses/by-zipcode/{zipCode}   | Consulta CEP via ViaCEP                 |
| GET    | /health/live                      | Liveness probe                          |
| GET    | /health/ready                     | Readiness probe                         |

---

## 🔍 Observabilidade

Todos os requests produzem logs estruturados (Serilog) com campos contextuais:

```
[2026-02-27 13:08:05 -03:00 INF] a1b2c3d4 joao.silva Cliente criado: 9f3a... (Individual)
                                   └──────┘ └────────┘
                                  CorrelId    UserId
```

- **CorrelationId** — propagado via `X-Correlation-Id` ou gerado na entrada.
  Aparece em cada linha de log, permitindo rastrear o fluxo completo de um request.
- **UserId** — extraído de `X-User`; em produção viria do JWT após integração com IdP.
- **Auditoria via Event Store** — `GET /customers/{id}/events` retorna cada mutação
  com payload completo, usuário responsável e correlation ID. Nada se perde.

```json
{
  "items": [{
    "eventId": "...",
    "version": 1,
    "eventType": "CustomerCreated",
    "data": { "name": "João Silva", "document": "52998224725", "..." },
    "userId": "joao.silva",
    "correlationId": "a1b2c3d4-...",
    "occurredAtUtc": "2026-02-27T16:08:05Z"
  }],
  "total": 1, "page": 1, "pageSize": 20, "totalPages": 1
}
```

---

## ⚖️ Trade-offs Conscientes

| Decisão                                | Justificativa                                                       |
|----------------------------------------|---------------------------------------------------------------------|
| Projeção síncrona (mesma transação)    | Simplicidade primeiro; consistência eventual quando surgir volume   |
| Event Store no mesmo banco             | Zero infraestrutura extra; separar quando houver necessidade real   |
| Auth simulado via `X-User`             | JWT + OIDC quando houver IdP; o slot já existe nos logs e eventos   |
| Read model denormalizado               | Elimina joins em lista e detalhe; custo na escrita vale a pena      |
| Sem cache de segunda camada            | Postgres com índices é suficiente para o volume atual               |
| Circuit breaker por instância          | Estado não compartilhado entre pods; Redis quando escalar           |
| Concorrência via UNIQUE index          | Simples e confiável; pessimistic locking seria over-engineering     |
| ViaCEP não bloqueia criação            | Falha silenciosamente — UX não é prejudicada se o serviço cair      |

---

## 🐳 Como Executar

### Docker (recomendado)

```bash
cd docker
docker compose up --build
```

O Postgres sobe com healthcheck; a API aguarda o banco estar pronto antes de rodar as migrations.

### Frontend (terminal separado)

```bash
cd frontend
npm install
npm run dev
```

`VITE_API_URL` aponta para `http://localhost:8080` por padrão — nenhuma configuração necessária.

### Desenvolvimento local (sem Docker)

```bash
# Só o Postgres
docker compose -f docker/docker-compose.yml up postgres -d

# API (migrations automáticas em Development)
dotnet run --project src/CRM.Clients.Api
```

Scripts de conveniência: `.\scripts\dev.ps1` (Windows) · `./scripts/dev.sh` (Linux/macOS)

### Variáveis de ambiente

| Variável                        | Padrão                                                                              |
|---------------------------------|-------------------------------------------------------------------------------------|
| `ConnectionStrings__Default`    | `Host=localhost;Port=5432;Database=crm_clients;Username=crm_user;Password=crm_pass` |
| `ASPNETCORE_ENVIRONMENT`        | `Development`                                                                       |
| `VITE_API_URL`                  | `http://localhost:8080`                                                             |

---

## 🧪 Testes

```bash
dotnet test
```

| Suite                  | Tipo        | O que cobre                                              |
|------------------------|-------------|----------------------------------------------------------|
| Domain.Tests           | Unitário    | Aggregate, value objects, invariantes de domínio         |
| IntegrationTests       | Integração  | Endpoints HTTP + Postgres real via Testcontainers        |
| ViaCepClientTests      | Unitário    | HttpClient com fake handler, sem rede real               |

Testes de integração sobem containers PostgreSQL isolados via **Testcontainers** —
sem mocks de banco, sem shared state entre classes de teste.

---

## 📁 Estrutura

```
src/
  CRM.Clients.Api/            → Endpoints, middlewares, Program.cs
  CRM.Clients.Application/    → Commands, queries, handlers, validators
  CRM.Clients.Domain/         → Aggregate, value objects, domain events
  CRM.Clients.Infrastructure/ → EF Core, Postgres, ViaCEP client

frontend/
  src/api/                    → Camada HTTP (queries de leitura + commands de escrita)
  src/pages/                  → Lista · Detalhe · Auditoria · Cadastro
  src/components/             → Layout, PageContainer, ErrorMessage, Loading

docker/                       → docker-compose.yml
docs/adr/                     → Architecture Decision Records
scripts/                      → dev.ps1 / dev.sh
tests/
  CRM.Clients.Domain.Tests/
  CRM.Clients.IntegrationTests/
```

---

## 📐 ADRs

- [ADR-001](docs/adr/ADR-001-event-sourcing.md) — Event Sourcing
- [ADR-002](docs/adr/ADR-002-cqrs.md) — CQRS
- [ADR-003](docs/adr/ADR-003-postgresql-choice.md) — PostgreSQL
- [ADR-004](docs/adr/ADR-004-resilience-strategy.md) — Resiliência (Polly + ViaCEP)
