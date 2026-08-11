# FreelaHunter

Sistema local-first para aquisição assistida de trabalhos freelance.

## Arquitetura

Solution .NET 10 com Clean Architecture:

- **Domain** — Entidades, Value Objects, Enums e regras de domínio.
- **Contracts** — DTOs e contratos externos.
- **Application** — Casos de uso e orquestração.
- **Infrastructure** — Persistência, gateways e integrações.
- **Api** — ASP.NET Core Web API.
- **Worker** — Worker Service para processamento assíncrono.
- **Web** — Blazor Web App.

## Comandos

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

## Status

Este é o work item FHL-001 — Solution Foundation.

Integrações externas (Workana, Gmail, LLMs, scraping, browser automation, envio de propostas) ainda **não foram implementadas**.
