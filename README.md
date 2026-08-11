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
- MiMo structured opportunity analysis (FHL-005) — análise semântica via LLM, sem score via LLM

Para utilizar análise real, configurar `MiMo__ApiKey` via environment variable ou mecanismo seguro de secrets.
Provider default: MiMo. Model default: mimo-v2.5. Nenhum LLM é necessário para testes automatizados.

Integrações externas (Workana, Gmail, scraping) ainda **não foram implementadas**.
