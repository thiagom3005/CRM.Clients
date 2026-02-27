# ADR-002 — CQRS

**Status:** Aceito
**Data:** 2026-02

## Contexto

Com Event Sourcing, o write model (aggregate rehydratado de eventos) e otimizado para invariantes, nao para consultas. Expor o mesmo modelo para leitura cria acoplamento e degrada performance.

## Decisao

Separar explicitamente commands (escrita) e queries (leitura):

- **Commands** — handlers carregam aggregate via event store, aplicam mutacao, fazem append de eventos
- **Queries** — handlers consultam diretamente o `customer_read_model` denormalizado via EF Core com `AsNoTracking()` e projecao direta para DTO

MediatR e usado como dispatcher, mas o padrao CQRS vale independente dele.

## Trade-offs

**A favor:**
- Read model otimizado por caso de uso (campos, indices, formato)
- Write side desacoplado do formato de exibicao
- Escalabilidade independente (leitura pode ser cacheada; escrita nao)

**Contra:**
- Consistencia eventual entre evento e projecao (mitigado: projecao e sincrona na mesma transacao)
- Duas "versoes" da entidade para manter (aggregate + read model)

## Consequencias

- Nunca rehydratar aggregate em query handlers
- Nunca escrever eventos em query handlers
- `ICustomerReadRepository` e `IEventStore` sao interfaces distintas e nao substituiveis
