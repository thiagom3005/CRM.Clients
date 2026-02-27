# CRM.Clients — Modulo de Clientes

API de gerenciamento de clientes corporativos construida com .NET 9, DDD, CQRS e Event Sourcing.

## Visao Geral

Modulo responsavel pelo ciclo de vida completo de clientes (PF e PJ), com:

- Validacoes de dominio: CPF/CNPJ, idade minima 18 anos, IE/isencao para PJ
- Historico auditavel de toda mutacao via Event Sourcing
- Read model denormalizado para consultas performaticas
- Integracao resiliente com ViaCEP para autocompletar enderecos
- Rastreabilidade por `X-Correlation-Id` e `X-User` em todos os logs estruturados

## Arquitetura

```
┌─────────────────┐   ┌─────────────────────┐   ┌──────────────────────┐
│    API Layer    │──▶│    Application      │──▶│       Domain         │
│  Endpoints,     │   │  Handlers,          │   │  Aggregates,         │
│  Middleware     │   │  Validators,        │   │  Value Objects,      │
└─────────────────┘   │  MediatR pipeline   │   │  Domain Events       │
                      └─────────────────────┘   └──────────────────────┘
                               │
                               ▼
                      ┌─────────────────────┐
                      │   Infrastructure    │
                      │  EF Core, Postgres, │
                      │  ViaCEP Client      │
                      └─────────────────────┘
```

| Camada         | Responsabilidade                                              |
|----------------|---------------------------------------------------------------|
| Domain         | Invariantes de negocio, value objects, eventos de dominio     |
| Application    | Orquestracao: unicidade -> aggregate -> eventos -> projecao   |
| Infrastructure | Persistencia (event store + read model), integracoes externas |
| API            | Roteamento, serializacao, middleware de cross-cutting         |

## Fluxo Command -> Event -> Projection -> Query

```
POST /customers
  │
  ├─ ValidationBehavior (FluentValidation)
  ├─ Verifica unicidade de CPF e e-mail no read model
  ├─ Customer.CreateIndividual() — invariantes de dominio
  ├─ PgEventStore.AppendAsync() — insere em events
  ├─ CustomerProjectionWriter — atualiza customer_read_model
  └─ SaveChangesAsync() — commit atomico (eventos + projecao)

GET /customers/{id}
  └─ EfCustomerReadRepository — SELECT direto no read model (AsNoTracking)
```

Evento e projecao estao **na mesma transacao**. Nunca ha leitura inconsistente.

## Endpoints

| Metodo | Rota                                | Descricao                             |
|--------|-------------------------------------|---------------------------------------|
| POST   | /customers                          | Cria cliente (PF ou PJ)               |
| GET    | /customers/{id}                     | Detalhe completo do cliente           |
| GET    | /customers?search=&page=&pageSize=  | Busca paginada por nome, e-mail, CPF  |
| GET    | /customers/{id}/events              | Historico paginado de eventos         |
| PUT    | /customers/{id}/email               | Altera e-mail                         |
| PUT    | /customers/{id}/address             | Atualiza endereco                     |
| GET    | /addresses/by-zipcode/{zipCode}     | Consulta CEP via ViaCEP               |
| GET    | /health/live                        | Liveness probe (processo vivo?)       |
| GET    | /health/ready                       | Readiness probe (banco acessivel?)    |

## Observabilidade

Todos os logs sao estruturados (Serilog) e incluem:

- `CorrelationId` — propagado via `X-Correlation-Id` ou gerado na entrada
- `UserId` — extraído de `X-User`; JWT quando houver autenticação real

```
[2026-02-27 13:08:05 -03:00 INF] a1b2c3d4 joao.silva Cliente criado: 9f3a... (Individual)
```

Formato: `[timestamp] {CorrelationId} {UserId} {mensagem}`

## Auditoria

`GET /customers/{id}/events` retorna historico completo de eventos:

```json
{
  "items": [
    {
      "eventId": "...",
      "version": 1,
      "eventType": "CustomerCreated",
      "data": { "..." },
      "userId": "joao.silva",
      "correlationId": "a1b2c3d4",
      "occurredAtUtc": "2026-02-27T16:08:05Z"
    }
  ],
  "total": 2,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

## Como Executar

### Docker (recomendado)

```bash
cd docker
docker compose up --build
```

API em `http://localhost:8080` | Swagger em `http://localhost:8080/swagger`

### Desenvolvimento local

```bash
# Windows
.\scripts\dev.ps1

# Linux / macOS
./scripts/dev.sh
```

Ou manualmente:

```bash
# Postgres apenas
docker compose -f docker/docker-compose.yml up postgres -d

# API (migrations automaticas em Development)
dotnet run --project src/CRM.Clients.Api
```

### Variaveis de ambiente

| Variavel                        | Padrao                                                                              |
|---------------------------------|-------------------------------------------------------------------------------------|
| `ConnectionStrings__Default`    | `Host=localhost;Port=5432;Database=crm_clients;Username=crm_user;Password=crm_pass` |
| `ASPNETCORE_ENVIRONMENT`        | `Development`                                                                       |

## Testes

```bash
dotnet test
```

| Suite                  | Tipo       | O que cobre                                              |
|------------------------|------------|----------------------------------------------------------|
| Domain.Tests           | Unitario   | Aggregate, value objects, invariantes de dominio          |
| IntegrationTests       | Integracao | Endpoints HTTP + Postgres real via Testcontainers         |
| ViaCepClientTests      | Unitario   | HttpClient com fake handler, sem chamada real ao ViaCEP  |

Testes de integracao sobem containers PostgreSQL isolados via **Testcontainers** — sem mocks de banco.

## Trade-offs do Desafio

| Decisao                               | Justificativa                                                        |
|---------------------------------------|----------------------------------------------------------------------|
| Event Sourcing sincrono               | Projecao na mesma transacao: simplicidade > consistencia eventual    |
| Read model denormalizado              | Evita joins custosos em lista/detalhe                                |
| Sem cache de segunda camada           | Volume atual nao justifica; Postgres com indices e suficiente        |
| ViaCEP como dependencia opcional      | Consulta de CEP nao bloqueia criacao de cliente                      |
| Sem autenticacao real                 | X-User no header; JWT + OIDC quando houver autenticação real         |
| Polly por instancia (sem Redis)       | Estado do circuit breaker nao e compartilhado entre pods             |
| Concorrencia otimista via UNIQUE index| Simples e confiavel; pessimistic locking seria over-engineering aqui |

## Estrutura

```
src/
  CRM.Clients.Api/           # Endpoints, middlewares, Program.cs
  CRM.Clients.Application/   # Commands, queries, handlers, validators
  CRM.Clients.Domain/        # Aggregate, value objects, domain events
  CRM.Clients.Infrastructure/# EF Core, Postgres, ViaCEP client

docs/adr/                    # Architecture Decision Records
docker/                      # docker-compose.yml
scripts/                     # dev.ps1 / dev.sh
tests/
  CRM.Clients.Domain.Tests/
  CRM.Clients.IntegrationTests/
```

## ADRs

- [ADR-001](docs/adr/ADR-001-event-sourcing.md) — Event Sourcing
- [ADR-002](docs/adr/ADR-002-cqrs.md) — CQRS
- [ADR-003](docs/adr/ADR-003-postgresql-choice.md) — PostgreSQL
- [ADR-004](docs/adr/ADR-004-resilience-strategy.md) — Resiliencia (Polly + ViaCEP)
