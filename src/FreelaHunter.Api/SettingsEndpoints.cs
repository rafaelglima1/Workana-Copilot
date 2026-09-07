using FreelaHunter.Application;
using FreelaHunter.Application.Providers;
using FreelaHunter.Infrastructure;
using FreelaHunter.Infrastructure.Providers;
using FreelaHunter.Infrastructure.Providers.Nerdin;
using FreelaHunter.Infrastructure.Providers.Workana;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FreelaHunter.Api;

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/settings", (
            IConfiguration configuration) =>
        {
            var llm = new LlmSettingsResponse
            {
                Provider = configuration["MiMo:BaseUrl"] ?? "",
                Model = configuration["MiMo:Model"] ?? "mimo-v2.5",
                HasApiKey = !string.IsNullOrWhiteSpace(configuration["MiMo:ApiKey"])
            };

            var telegram = new TelegramSettingsResponse
            {
                BotToken = configuration["Telegram:BotToken"] ?? "",
                ChatId = configuration["Telegram:ChatId"] ?? "",
                Enabled = configuration.GetValue<bool>("Telegram:Enabled")
            };

            var scraper = new ScraperSettingsResponse
            {
                WorkanaBaseUrl = configuration["Providers:Workana:BaseUrl"] ?? "https://www.workana.com/jobs",
                WorkanaLanguage = configuration["Providers:Workana:Language"] ?? "pt",
                WorkanaSkills = configuration["Providers:Workana:Skills"] ?? "",
                WorkanaSort = configuration["Providers:Workana:Sort"] ?? "date",
                WorkanaLoginEnabled = configuration.GetValue<bool>("Providers:Workana:LoginEnabled"),
                WorkanaEmail = configuration["Providers:Workana:Email"] ?? "",
                NerdinBaseUrl = configuration["Providers:Nerdin:BaseUrl"] ?? "https://www.nerdin.com.br/vagas",
                NerdinTechnology = configuration["Providers:Nerdin:Technology"] ?? "",
                EnabledProviders = configuration.GetSection("Worker:EnabledProviders").Get<string[]>() ?? ["workana"],
                ScrapeIntervalMinutes = configuration.GetValue<int>("Worker:ScrapeIntervalMinutes"),
                MaxPages = configuration.GetValue<int>("Worker:ScrapeMaxPages"),
                CamoufoxEnabled = configuration.GetValue<bool>("Camoufox:Enabled"),
                CamoufoxServerUrl = configuration["Camoufox:ServerUrl"] ?? "http://localhost:54321"
            };

            return Results.Ok(new SettingsResponse
            {
                Llm = llm,
                Telegram = telegram,
                Scraper = scraper
            });
        })
        .WithName("GetSettings")
        .Produces<SettingsResponse>();

        app.MapPut("/api/settings", async (
            [FromBody] UpdateSettingsRequest request,
            IConfiguration configuration) =>
        {
            var settingsPath = FindSettingsFile(configuration);

            if (!File.Exists(settingsPath))
                return Results.StatusCode(500);

            var json = await File.ReadAllTextAsync(settingsPath);
            var settings = System.Text.Json.Nodes.JsonNode.Parse(json) as System.Text.Json.Nodes.JsonObject
                ?? new System.Text.Json.Nodes.JsonObject();

            if (request.Llm is not null)
            {
                SetNestedValue(settings, "MiMo", "BaseUrl", request.Llm.Provider);
                SetNestedValue(settings, "MiMo", "Model", request.Llm.Model);
                if (!string.IsNullOrWhiteSpace(request.Llm.ApiKey))
                    SetNestedValue(settings, "MiMo", "ApiKey", request.Llm.ApiKey);
            }

            if (request.Telegram is not null)
            {
                SetNestedValue(settings, "Telegram", "BotToken", request.Telegram.BotToken);
                SetNestedSetting(settings, "Telegram", "ChatId", request.Telegram.ChatId);
                if (request.Telegram.Enabled.HasValue)
                    SetNestedValue(settings, "Telegram", "Enabled", request.Telegram.Enabled.Value.ToString().ToLower());
            }

            if (request.Scraper is not null)
            {
                // Workana provider
                SetNestedValue(settings, "Providers", "Workana:BaseUrl", request.Scraper.WorkanaBaseUrl);
                SetNestedValue(settings, "Providers", "Workana:Language", request.Scraper.WorkanaLanguage);
                SetNestedValue(settings, "Providers", "Workana:Skills", request.Scraper.WorkanaSkills);
                if (!string.IsNullOrWhiteSpace(request.Scraper.WorkanaSort))
                    SetNestedValue(settings, "Providers", "Workana:Sort", request.Scraper.WorkanaSort);
                if (request.Scraper.WorkanaLoginEnabled.HasValue)
                    SetNestedValue(settings, "Providers", "Workana:LoginEnabled", request.Scraper.WorkanaLoginEnabled.Value.ToString().ToLower());
                SetNestedValue(settings, "Providers", "Workana:Email", request.Scraper.WorkanaEmail);
                if (!string.IsNullOrWhiteSpace(request.Scraper.WorkanaPassword))
                    SetNestedValue(settings, "Providers", "Workana:Password", request.Scraper.WorkanaPassword);

                // Nerdin provider
                SetNestedValue(settings, "Providers", "Nerdin:BaseUrl", request.Scraper.NerdinBaseUrl);
                SetNestedValue(settings, "Providers", "Nerdin:Technology", request.Scraper.NerdinTechnology);

                // Worker settings
                if (request.Scraper.EnabledProviders is not null)
                    SetNestedArray(settings, "Worker", "EnabledProviders", request.Scraper.EnabledProviders);
                if (request.Scraper.ScrapeIntervalMinutes.HasValue)
                    SetNestedValue(settings, "Worker", "ScrapeIntervalMinutes", request.Scraper.ScrapeIntervalMinutes.Value.ToString());
                if (request.Scraper.MaxPages.HasValue)
                    SetNestedValue(settings, "Worker", "ScrapeMaxPages", request.Scraper.MaxPages.Value.ToString());

                // Camoufox settings
                if (request.Scraper.CamoufoxEnabled.HasValue)
                    SetNestedValue(settings, "Camoufox", "Enabled", request.Scraper.CamoufoxEnabled.Value.ToString().ToLower());
                SetNestedValue(settings, "Camoufox", "ServerUrl", request.Scraper.CamoufoxServerUrl);
            }

            var updatedJson = settings.ToJsonString(new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(settingsPath, updatedJson);

            return Results.Ok(new { message = "Configurações salvas. Reinicie a API para aplicar." });
        })
        .WithName("UpdateSettings")
        .Produces<object>();

        app.MapPost("/api/scraper/run", async (
            [FromServices] JobProviderRegistry registry,
            [FromServices] IOpportunityRepository repository,
            [FromServices] ITelegramNotifier notifier,
            [FromServices] CreateManualOpportunityHandler createHandler,
            [FromServices] EvaluateOpportunityHandler evaluateHandler,
            IConfiguration configuration,
            [FromQuery] string? provider,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var providers = !string.IsNullOrWhiteSpace(provider)
                    ? new[] { registry.GetProvider(provider) }
                    : registry.GetAllProviders().ToArray();

                var maxPages = configuration.GetValue<int>("Worker:ScrapeMaxPages");
                if (maxPages <= 0) maxPages = 1;

                var totalImported = 0;
                var totalEvaluated = 0;
                var totalSkipped = 0;

                foreach (var p in providers)
                {
                    var scraped = await p.FetchOpenJobsAsync(maxPages, cancellationToken);

                    foreach (var item in scraped)
                    {
                        if (string.IsNullOrWhiteSpace(item.ExternalUrl))
                        {
                            totalSkipped++;
                            continue;
                        }

                        var existing = await repository.GetByExternalReferenceAsync(item.ExternalUrl, cancellationToken);
                        if (existing is not null)
                        {
                            totalSkipped++;
                            continue;
                        }

                        var request = new Contracts.Opportunities.CreateManualOpportunityRequest
                        {
                            Marketplace = MapMarketplace(p.Marketplace),
                            Title = item.Title,
                            Description = item.Description,
                            RequiredSkills = item.RequiredSkills,
                            ExternalReference = item.ExternalUrl,
                            PublishedAt = item.PublishedAt
                        };

                        var created = await createHandler.HandleAsync(request, cancellationToken);

                        for (var attempt = 0; attempt < 3; attempt++)
                        {
                            try
                            {
                                await evaluateHandler.HandleAsync(created.Id, cancellationToken);
                                totalEvaluated++;
                                break;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[Scraper] Evaluation attempt {attempt + 1} failed for {item.Title}: {ex.Message}");
                                await Task.Delay(10000 * (attempt + 1), cancellationToken);
                            }
                        }

                        await Task.Delay(5000, cancellationToken);

                        totalImported++;
                    }
                }

                await notifier.SendMessageAsync(
                    $"\ud83d\udce5 <b>Scrape manual completo</b>\n\n" +
                    $"<b>{totalImported}</b> nova(s) oportunidade(s).\n" +
                    $"<b>{totalEvaluated}</b> avaliada(s).\n" +
                    $"<b>{totalSkipped}</b> ignorada(s).",
                    cancellationToken);

                return Results.Ok(new { imported = totalImported, evaluated = totalEvaluated, skipped = totalSkipped });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    title: "Scraper error",
                    statusCode: 500);
            }
        })
        .WithName("RunScraper")
        .Produces<object>();
    }

    private static Contracts.Opportunities.ContractMarketplace MapMarketplace(
        Domain.Enums.Marketplace domain) => domain switch
    {
        Domain.Enums.Marketplace.Workana => Contracts.Opportunities.ContractMarketplace.Workana,
        Domain.Enums.Marketplace.Upwork => Contracts.Opportunities.ContractMarketplace.Upwork,
        Domain.Enums.Marketplace.Freelancer => Contracts.Opportunities.ContractMarketplace.Freelancer,
        Domain.Enums.Marketplace.Nerdin => Contracts.Opportunities.ContractMarketplace.Nerdin,
        Domain.Enums.Marketplace.Other => Contracts.Opportunities.ContractMarketplace.Other,
        _ => Contracts.Opportunities.ContractMarketplace.Unknown
    };

    private static void SetNestedValue(
        System.Text.Json.Nodes.JsonObject root,
        string section,
        string key,
        string? value)
    {
        if (value is null) return;

        if (!root.ContainsKey(section) || root[section] is not System.Text.Json.Nodes.JsonObject nested)
        {
            nested = new System.Text.Json.Nodes.JsonObject();
            root[section] = nested;
        }
        nested[key] = value;
    }

    private static void SetNestedSetting(
        System.Text.Json.Nodes.JsonObject root,
        string section,
        string key,
        string? value)
    {
        if (!root.ContainsKey(section) || root[section] is not System.Text.Json.Nodes.JsonObject nested)
        {
            nested = new System.Text.Json.Nodes.JsonObject();
            root[section] = nested;
        }
        nested[key] = value ?? "";
    }

    private static void SetNestedArray(
        System.Text.Json.Nodes.JsonObject root,
        string section,
        string key,
        string[] values)
    {
        if (!root.ContainsKey(section) || root[section] is not System.Text.Json.Nodes.JsonObject nested)
        {
            nested = new System.Text.Json.Nodes.JsonObject();
            root[section] = nested;
        }

        var array = new System.Text.Json.Nodes.JsonArray();
        foreach (var v in values)
            array.Add(v);
        nested[key] = array;
    }

    private static string FindSettingsFile(IConfiguration configuration)
    {
        if (configuration is IConfigurationRoot root)
        {
            var providerPath = root.Providers
                .OfType<Microsoft.Extensions.Configuration.FileConfigurationProvider>()
                .FirstOrDefault()?.Source?.Path;

            if (!string.IsNullOrWhiteSpace(providerPath) && File.Exists(providerPath))
                return providerPath;
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "appsettings.json");
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        var solutionDir = FindSolutionDirectory();
        if (solutionDir is not null)
        {
            var apiDir = Path.Combine(solutionDir, "src", "FreelaHunter.Api", "appsettings.json");
            if (File.Exists(apiDir))
                return apiDir;
        }

        return Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    }

    private static string? FindSolutionDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.GetFiles("*.sln").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }
}

public sealed class SettingsResponse
{
    public LlmSettingsResponse Llm { get; set; } = new();
    public TelegramSettingsResponse Telegram { get; set; } = new();
    public ScraperSettingsResponse Scraper { get; set; } = new();
}

public sealed class LlmSettingsResponse
{
    public string Provider { get; set; } = "";
    public string Model { get; set; } = "";
    public bool HasApiKey { get; set; }
}

public sealed class TelegramSettingsResponse
{
    public string BotToken { get; set; } = "";
    public string ChatId { get; set; } = "";
    public bool Enabled { get; set; }
}

public sealed class ScraperSettingsResponse
{
    public string WorkanaBaseUrl { get; set; } = "";
    public string WorkanaLanguage { get; set; } = "pt";
    public string WorkanaSkills { get; set; } = "";
    public string WorkanaSort { get; set; } = "date";
    public bool WorkanaLoginEnabled { get; set; }
    public string WorkanaEmail { get; set; } = "";
    public string NerdinBaseUrl { get; set; } = "";
    public string NerdinTechnology { get; set; } = "";
    public string[] EnabledProviders { get; set; } = ["workana"];
    public int ScrapeIntervalMinutes { get; set; }
    public int MaxPages { get; set; }
    public bool CamoufoxEnabled { get; set; }
    public string CamoufoxServerUrl { get; set; } = "";
}

public sealed class UpdateSettingsRequest
{
    public LlmUpdateRequest? Llm { get; set; }
    public TelegramUpdateRequest? Telegram { get; set; }
    public ScraperUpdateRequest? Scraper { get; set; }
}

public sealed class LlmUpdateRequest
{
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public string? ApiKey { get; set; }
}

public sealed class TelegramUpdateRequest
{
    public string? BotToken { get; set; }
    public string? ChatId { get; set; }
    public bool? Enabled { get; set; }
}

public sealed class ScraperUpdateRequest
{
    public string? WorkanaBaseUrl { get; set; }
    public string? WorkanaLanguage { get; set; }
    public string? WorkanaSkills { get; set; }
    public string? WorkanaSort { get; set; }
    public bool? WorkanaLoginEnabled { get; set; }
    public string? WorkanaEmail { get; set; }
    public string? WorkanaPassword { get; set; }
    public string? NerdinBaseUrl { get; set; }
    public string? NerdinTechnology { get; set; }
    public string[]? EnabledProviders { get; set; }
    public int? ScrapeIntervalMinutes { get; set; }
    public int? MaxPages { get; set; }
    public bool? CamoufoxEnabled { get; set; }
    public string? CamoufoxServerUrl { get; set; }
}
