# ADR-001 — Event Sourcing

**Status:** Aceito
**Data:** 2026-02

## Contexto

O modulo de clientes precisa de rastreabilidade completa de mudancas para auditoria fiscal e compliance. Solucoes CRUD tradicionais perdem o historico ao sobrescrever registros.

## Decisao

Adotar Event Sourcing: o estado do aggregate e derivado de uma sequencia imutavel de eventos persistidos na tabela `events`. O read model (`customer_read_model`) e uma projecao sincronizada via `CustomerProjectionWriter`.

## Trade-offs

**A favor:**
- Auditoria completa sem tabelas de log separadas
- Replay possivel para reconstruir read models ou depurar producao
- Concorrencia otimista natural via `UNIQUE(aggregate_id, version)`

**Contra:**
- Read model precisa ser mantido sincronizado com o event store
- Rehydrate tem custo O(n) de eventos — aceitavel no volume atual
- Queries ad-hoc no event store sao impraticaveis (use o read model)

## Consequencias

- Toda mutacao de dominio obrigatoriamente gera evento e passa pelo `AppendAsync`
- Nunca consultar o aggregate via event store em endpoints de leitura
- Evolucao de schema de eventos requer versionamento (upcasters)
