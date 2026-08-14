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
- Freelancer profile (FHL-006) — perfil com skills e metas de hourly rate
- Assisted opportunity evaluation (FHL-007) — pipeline completo: Opportunity → MiMo analysis → Profile → deterministic scoring
- Deterministic skill intelligence (FHL-008) — SkillNameNormalizer, SkillAliasCatalog, SkillMatcher
- SQLite + EF Core local persistence (FHL-009) — Opportunity e FreelancerProfile via EF Core, auto-migration
- Opportunity Inbox workflow (FHL-010) — paginação, busca, filtros, ordenação, transições de status
- Evaluation snapshot persistence (FHL-011) — snapshots de avaliação persistidos, exibidos na Inbox com score/recommendation
- Deterministic proposal strategy (FHL-012) — estratégia de proposta determinística baseada em score e perfil
- Blazor Web UI (FHL-013) — Inbox, Detalhe, Criar, Perfil — via HTTP client contra a API
- Automated background processing (FHL-014) — Worker evalua oportunidades automaticamente, gera propostas para Go/Apply
- Bulk opportunity import (FHL-015) — importação em lote via JSON, com relatório de erros

API endpoints:

- `GET /api/opportunities` — inbox com paginação, busca, filtros, ordenação
- `POST /api/opportunities/manual` — criar oportunidade manual
- `POST /api/opportunities/import` — importação em lote (JSON)
- `GET /api/opportunities/{id}` — detalhe da oportunidade
- `POST /api/opportunities/{id}/shortlist` → Shortlisted
- `POST /api/opportunities/{id}/ignore` → Ignored
- `POST /api/opportunities/{id}/archive` → Archived (terminal)
- `POST /api/opportunities/{id}/evaluate` — avaliar (determinístico + LLM opcional)
- `GET /api/opportunities/{id}/evaluation` — snapshot de avaliação
- `POST /api/opportunities/{id}/proposal-strategy` — gerar estratégia de proposta
- `GET/PUT /api/freelancer-profile` — perfil do freelancer

Para utilizar LLM, configurar `MiMo__ApiKey` via env var. Provider: MiMo. Model: mimo-v2.5. Testes automatizados não necessitam de LLM.

Integrações externas:
- ✅ Workana scraper (Playwright headless browser, integrado ao Worker)
- Gmail integration (notificação de novas oportunidades) — pendente
- Docker Compose disponível (docker-compose.yml)
- LLM fallback: sistema funciona sem API key (análise neutra)
