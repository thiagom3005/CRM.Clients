# CRM.Clients

Baseline arquitetural do desafio técnico **"Módulo de Clientes — CRM Corporativo"**.

## Visão geral
Este repositório contém a fundação técnica da API de clientes usando arquitetura em camadas (Domain, Application, Infrastructure e Api), pronta para evoluir com regras de negócio.

## Stack
- .NET 9 / C# latest
- ASP.NET Core Web API
- MediatR (preparado)
- FluentValidation (preparado)
- EF Core + PostgreSQL (infra preparada)
- Serilog
- Docker / Docker Compose

## Estrutura
```text
/src
 ├── CRM.Clients.Domain
 ├── CRM.Clients.Application
 ├── CRM.Clients.Infrastructure
 └── CRM.Clients.Api
/docs/adr
/docker
/frontend
```

## Como rodar com Docker
```bash
cd docker
docker compose up --build
```

## Como rodar localmente
```bash
dotnet restore CRM.Clients.sln
dotnet build CRM.Clients.sln
dotnet run --project src/CRM.Clients.Api/CRM.Clients.Api.csproj
```

## URLs úteis
- Swagger: `http://localhost:8080/swagger`
- Health: `http://localhost:8080/health`
