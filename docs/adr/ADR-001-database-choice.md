# ADR-001 — Database Choice

## Contexto
O módulo de clientes precisa de um banco relacional confiável, com boa integração ao ecossistema .NET e suporte sólido em ambientes containerizados.

## Decisão
Adotar PostgreSQL como banco principal da solução.

## Consequências
- Excelente suporte a EF Core e drivers maduros para .NET.
- Facilidade para operar localmente e em produção com Docker.
- Recursos avançados (índices, JSONB, extensões) disponíveis para evolução futura.
