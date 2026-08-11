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

- Manual opportunity intake (FHL-003)
- Deterministic opportunity scoring (FHL-004) — score calculado on-demand, sem LLM

Nenhum LLM está conectado. Integrações externas (Workana, Gmail, scraping) ainda **não foram implementadas**.
