# ADR-003 — PostgreSQL como banco principal

**Status:** Aceito
**Data:** 2026-02

## Contexto

O projeto precisa de:
- Garantia ACID para o commit atomico de eventos + projecao
- Constraint `UNIQUE(aggregate_id, version)` para concorrencia otimista no event store
- Suporte a `ILike` para busca case-insensitive sem collation extra
- Ecossistema maduro no .NET (Npgsql + EF Core)

## Decisao

PostgreSQL 16 como unico banco de dados. Sem cache de segunda camada na fase atual.

## Trade-offs

**A favor:**
- ACID nativo: evento e projecao no mesmo `SaveChanges` = zero risco de inconsistencia
- `UNIQUE(aggregate_id, version)` detecta race condition automaticamente como `23505`
- `ILike` nativo para busca case-insensitive sem UDF
- Docker-ready, suporte amplo em cloud (RDS, Cloud SQL, Supabase)

**Contra:**
- Nao e OLAP: queries analiticas complexas no event store precisarao de exportacao
- Em alto volume, o UNIQUE constraint pode ser gargalo de escrita

## Consequencias

- Connection string configuravel via `ConnectionStrings:Default` em `appsettings.json`
- Migrations gerenciadas pelo EF Core (`dotnet ef migrations`)
- Em producao usar connection pool e SSL obrigatorio
