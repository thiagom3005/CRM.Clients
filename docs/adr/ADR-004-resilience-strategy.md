# ADR-004 — Estrategia de resiliencia para integracoes externas

**Status:** Aceito
**Data:** 2026-02

## Contexto

O modulo consulta o ViaCEP (servico externo publico) para autocompletar enderecos. Servicos externos falham. Sem resiliencia, uma falha pontual retorna 500 para o usuario e pode cascatear para o restante da aplicacao.

## Decisao

Usar `Microsoft.Extensions.Http.Resilience` (Polly v8) via `AddStandardResilienceHandler`:

| Politica        | Configuracao                        |
|-----------------|-------------------------------------|
| Retry           | 3 tentativas, delay base de 1 s     |
| AttemptTimeout  | 3 s por tentativa                   |
| TotalTimeout    | 10 s para toda a operacao           |
| CircuitBreaker  | 5 falhas em 30 s → abre por 30 s    |

`ServiceUnavailableException` → HTTP 503 quando todas as tentativas se esgotam.

## Trade-offs

**A favor:**
- Falha no ViaCEP nao derruba o fluxo principal (consulta de CEP e opcional)
- Circuit breaker evita pressionar servico degradado
- Configuracao declarativa sem boilerplate

**Contra:**
- Latencia maxima de ~10 s em pior caso (3 tentativas x 3 s + jitter)
- Estado do circuit breaker e por instancia — sem compartilhamento entre pods

## Consequencias

- `GET /addresses/by-zipcode/{zipCode}` retorna 503 quando o ViaCEP esta indisponivel
- Logs com `CorrelationId` facilitam diagnostico de falhas em cascata
- Em producao com multiplos pods: considerar Redis para estado compartilhado do circuit breaker
