using System.Net;
using FreelaHunter.Application;
using FreelaHunter.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FreelaHunter.Api;

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/settings", (
            [FromServices] IConfiguration configuration) =>
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
                BaseUrl = configuration["WorkanaScraper:BaseUrl"] ?? "https://www.workana.com/jobs",
                Language = configuration["WorkanaScraper:Language"] ?? "pt",
                Skills = configuration["WorkanaScraper:Skills"] ?? "",
                Technology = configuration["WorkanaScraper:Technology"] ?? "",
                Sort = configuration["WorkanaScraper:Sort"] ?? "date",
                MaxPages = configuration.GetValue<int>("WorkanaScraper:MaxPages"),
                LoginEnabled = configuration.GetValue<bool>("WorkanaScraper:LoginEnabled"),
                Email = configuration["WorkanaScraper:Email"] ?? "",
                ScrapeIntervalMinutes = configuration.GetValue<int>("Worker:ScrapeIntervalMinutes")
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
            [FromServices] IConfiguration configuration) =>
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
                SetNestedValue(settings, "WorkanaScraper", "BaseUrl", request.Scraper.BaseUrl);
                SetNestedValue(settings, "WorkanaScraper", "Language", request.Scraper.Language);
                SetNestedValue(settings, "WorkanaScraper", "Skills", request.Scraper.Skills);
                SetNestedValue(settings, "WorkanaScraper", "Technology", request.Scraper.Technology);
                if (!string.IsNullOrWhiteSpace(request.Scraper.Sort))
                    SetNestedValue(settings, "WorkanaScraper", "Sort", request.Scraper.Sort);
                if (request.Scraper.MaxPages.HasValue)
                    SetNestedValue(settings, "WorkanaScraper", "MaxPages", request.Scraper.MaxPages.Value.ToString());
                if (request.Scraper.LoginEnabled.HasValue)
                    SetNestedValue(settings, "WorkanaScraper", "LoginEnabled", request.Scraper.LoginEnabled.Value.ToString().ToLower());
                SetNestedValue(settings, "WorkanaScraper", "Email", request.Scraper.Email);
                if (!string.IsNullOrWhiteSpace(request.Scraper.Password))
                    SetNestedValue(settings, "WorkanaScraper", "Password", request.Scraper.Password);
                if (request.Scraper.ScrapeIntervalMinutes.HasValue)
                    SetNestedValue(settings, "Worker", "ScrapeIntervalMinutes", request.Scraper.ScrapeIntervalMinutes.Value.ToString());
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
            [FromServices] IWorkanaScraper scraper,
            [FromServices] IOpportunityRepository repository,
            [FromServices] ITelegramNotifier notifier,
            [FromServices] CreateManualOpportunityHandler createHandler,
            [FromServices] EvaluateOpportunityHandler evaluateHandler,
            [FromServices] IConfiguration configuration,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var maxPages = configuration.GetValue<int>("WorkanaScraper:MaxPages");
                if (maxPages <= 0) maxPages = 1;

                var scraped = await scraper.ScrapeAsync(maxPages, cancellationToken);
                var imported = 0;
                var evaluated = 0;
                var skipped = 0;

                foreach (var item in scraped)
                {
                    if (string.IsNullOrWhiteSpace(item.ExternalUrl))
                    {
                        skipped++;
                        continue;
                    }

                    var existing = await repository.GetByExternalReferenceAsync(item.ExternalUrl, cancellationToken);
                    if (existing is not null)
                    {
                        skipped++;
                        continue;
                    }

                    var request = new Contracts.Opportunities.CreateManualOpportunityRequest
                    {
                        Marketplace = Contracts.Opportunities.ContractMarketplace.Workana,
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
                            evaluated++;
                            break;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Scraper] Evaluation attempt {attempt + 1} failed for {item.Title}: {ex.Message}");
                            await Task.Delay(10000 * (attempt + 1), cancellationToken);
                        }
                    }

                    await Task.Delay(5000, cancellationToken);

                    imported++;
                }

                await notifier.SendMessageAsync(
                    $"\ud83d\udce5 <b>Scrape manual completo</b>\n\n" +
                    $"<b>{imported}</b> nova(s) oportunidade(s).\n" +
                    $"<b>{evaluated}</b> avaliada(s).\n" +
                    $"<b>{skipped}</b> ignorada(s).",
                    cancellationToken);

                return Results.Ok(new { imported, evaluated, skipped, total = scraped.Count });
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

    private static string FindSettingsFile(IConfiguration configuration)
    {
        // Try to find via IConfigurationRoot provider path first
        if (configuration is IConfigurationRoot root)
        {
            var providerPath = root.Providers
                .OfType<Microsoft.Extensions.Configuration.FileConfigurationProvider>()
                .FirstOrDefault()?.Source?.Path;

            if (!string.IsNullOrWhiteSpace(providerPath) && File.Exists(providerPath))
                return providerPath;
        }

        // Walk up from BaseDirectory to find appsettings.json
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "appsettings.json");
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        // Fallback to project root relative to solution
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
    public string BaseUrl { get; set; } = "";
    public string Language { get; set; } = "pt";
    public string Skills { get; set; } = "";
    public string Technology { get; set; } = "";
    public string Sort { get; set; } = "date";
    public int MaxPages { get; set; }
    public bool LoginEnabled { get; set; }
    public string Email { get; set; } = "";
    public int ScrapeIntervalMinutes { get; set; }
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
    public string? BaseUrl { get; set; }
    public string? Language { get; set; }
    public string? Skills { get; set; }
    public string? Technology { get; set; }
    public string? Sort { get; set; }
    public int? MaxPages { get; set; }
    public bool? LoginEnabled { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public int? ScrapeIntervalMinutes { get; set; }
}
