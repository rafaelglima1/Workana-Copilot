using FreelaHunter.Application;
using FreelaHunter.Application.Providers;
using FreelaHunter.Contracts.Opportunities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FreelaHunter.Worker;

public sealed class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<WorkerOptions> _options;
    private readonly ILogger<Worker> _logger;

    public Worker(
        IServiceScopeFactory scopeFactory,
        IOptions<WorkerOptions> options,
        ILogger<Worker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var evalInterval = TimeSpan.FromSeconds(_options.Value.IntervalSeconds);
        var scrapeInterval = TimeSpan.FromMinutes(_options.Value.ScrapeIntervalMinutes);

        var lastScrape = DateTime.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunEvaluationCycleAsync(stoppingToken);

                if (DateTime.UtcNow - lastScrape >= scrapeInterval)
                {
                    await RunScrapeCycleAsync(stoppingToken);
                    lastScrape = DateTime.UtcNow;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during worker cycle.");
            }

            try
            {
                await Task.Delay(evalInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunEvaluationCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<OpportunityBackgroundProcessor>();

        var result = await processor.ProcessPendingAsync(_options.Value.BatchSize, cancellationToken);

        if (result.Evaluated > 0 || result.Failed > 0)
        {
            _logger.LogInformation(
                "Evaluation cycle: {Evaluated} evaluated, {Proposals} proposals, {Failed} failed.",
                result.Evaluated,
                result.ProposalsGenerated,
                result.Failed);
        }
    }

    private async Task RunScrapeCycleAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting scrape cycle across enabled providers...");

        using var scope = _scopeFactory.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<JobProviderRegistry>();
        var repository = scope.ServiceProvider.GetRequiredService<IOpportunityRepository>();
        var notifier = scope.ServiceProvider.GetRequiredService<ITelegramNotifier>();
        var createHandler = scope.ServiceProvider.GetRequiredService<CreateManualOpportunityHandler>();

        var enabledIds = _options.Value.EnabledProviders;
        var providers = registry.GetEnabledProviders(enabledIds);

        var totalImported = 0;
        var totalSkipped = 0;

        foreach (var provider in providers)
        {
            try
            {
                _logger.LogInformation("Scraping provider: {Provider}", provider.DisplayName);

                var scraped = await provider.FetchOpenJobsAsync(
                    _options.Value.ScrapeMaxPages, cancellationToken);

                var imported = 0;
                var skipped = 0;

                foreach (var item in scraped)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (string.IsNullOrWhiteSpace(item.ExternalUrl))
                    {
                        skipped++;
                        continue;
                    }

                    var existing = await repository.GetByExternalReferenceAsync(
                        item.ExternalUrl, cancellationToken);

                    if (existing is not null)
                    {
                        skipped++;
                        continue;
                    }

                    var request = new CreateManualOpportunityRequest
                    {
                        Marketplace = MapMarketplace(provider.Marketplace),
                        Title = item.Title,
                        Description = item.Description,
                        Category = item.Category,
                        Subcategory = item.Subcategory,
                        RequiredSkills = item.RequiredSkills,
                        Budget = MapBudgetFromSummary(item.BudgetSummary),
                        Client = new ClientInput
                        {
                            Country = item.ClientCountry
                        },
                        Competition = item.ProposalsCount.HasValue
                            ? new CompetitionInput { ProposalCount = item.ProposalsCount.Value }
                            : null,
                        ExternalReference = item.ExternalUrl,
                        PublishedAt = item.PublishedAt
                    };

                    await createHandler.HandleAsync(request, cancellationToken);
                    imported++;
                }

                totalImported += imported;
                totalSkipped += skipped;

                _logger.LogInformation(
                    "Provider {Provider}: {Imported} new, {Skipped} skipped.",
                    provider.DisplayName, imported, skipped);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Scrape cycle failed for provider {Provider}", provider.DisplayName);
            }
        }

        _logger.LogInformation(
            "Scrape cycle complete: {Imported} total new, {Skipped} total skipped.",
            totalImported, totalSkipped);

        if (totalImported > 0 && _options.Value.NotifyOnScrape)
        {
            await notifier.SendMessageAsync(
                $"📥 <b>Scrape completo</b>\n\n" +
                $"<b>{totalImported}</b> nova(s) oportunidade(s) encontrada(s).\n" +
                $"<b>{totalSkipped}</b> ignorada(s) (duplicatas ou sem URL).",
                cancellationToken);
        }
    }

    private static ContractMarketplace MapMarketplace(Domain.Enums.Marketplace domain) => domain switch
    {
        Domain.Enums.Marketplace.Workana => ContractMarketplace.Workana,
        Domain.Enums.Marketplace.Upwork => ContractMarketplace.Upwork,
        Domain.Enums.Marketplace.Freelancer => ContractMarketplace.Freelancer,
        Domain.Enums.Marketplace.Nerdin => ContractMarketplace.Nerdin,
        Domain.Enums.Marketplace.Other => ContractMarketplace.Other,
        _ => ContractMarketplace.Unknown
    };

    private static BudgetInput? MapBudgetFromSummary(string? budgetSummary)
    {
        if (string.IsNullOrWhiteSpace(budgetSummary))
            return null;

        return new BudgetInput
        {
            Kind = ContractBudgetKind.Range,
            Basis = ContractBudgetBasis.FixedProject,
            Currency = "USD"
        };
    }
}
