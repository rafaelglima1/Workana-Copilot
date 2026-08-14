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
                Technology = configuration["WorkanaScraper:Technology"] ?? "",
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
            var settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(settingsPath))
            {
                var configFile = configuration as IConfigurationRoot;
                settingsPath = configFile?.Providers
                    .OfType<Microsoft.Extensions.Configuration.FileConfigurationProvider>()
                    .FirstOrDefault()?.Source?.Path ?? settingsPath;
            }

            if (!File.Exists(settingsPath))
                return Results.StatusCode(500);

            var json = await File.ReadAllTextAsync(settingsPath);
            var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json)
                ?? new Dictionary<string, object>();

            if (request.Llm is not null)
            {
                UpdateNestedSetting(settings, "MiMo", "BaseUrl", request.Llm.Provider);
                UpdateNestedSetting(settings, "MiMo", "Model", request.Llm.Model);
                if (!string.IsNullOrWhiteSpace(request.Llm.ApiKey))
                    UpdateNestedSetting(settings, "MiMo", "ApiKey", request.Llm.ApiKey);
            }

            if (request.Telegram is not null)
            {
                UpdateNestedSetting(settings, "Telegram", "BotToken", request.Telegram.BotToken);
                UpdateNestedSetting(settings, "Telegram", "ChatId", request.Telegram.ChatId);
                if (request.Telegram.Enabled.HasValue)
                    UpdateNestedSetting(settings, "Telegram", "Enabled", request.Telegram.Enabled.Value.ToString().ToLower());
            }

            if (request.Scraper is not null)
            {
                UpdateNestedSetting(settings, "WorkanaScraper", "BaseUrl", request.Scraper.BaseUrl);
                UpdateNestedSetting(settings, "WorkanaScraper", "Technology", request.Scraper.Technology);
                if (request.Scraper.MaxPages.HasValue)
                    UpdateNestedSetting(settings, "WorkanaScraper", "MaxPages", request.Scraper.MaxPages.Value.ToString());
                if (request.Scraper.LoginEnabled.HasValue)
                    UpdateNestedSetting(settings, "WorkanaScraper", "LoginEnabled", request.Scraper.LoginEnabled.Value.ToString().ToLower());
                UpdateNestedSetting(settings, "WorkanaScraper", "Email", request.Scraper.Email);
                if (request.Scraper.ScrapeIntervalMinutes.HasValue)
                    UpdateNestedSetting(settings, "Worker", "ScrapeIntervalMinutes", request.Scraper.ScrapeIntervalMinutes.Value.ToString());
            }

            var updatedJson = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions
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
            CancellationToken cancellationToken) =>
        {
            var scraped = await scraper.ScrapeAsync(3, cancellationToken);
            var imported = 0;
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

                await createHandler.HandleAsync(request, cancellationToken);
                imported++;
            }

            await notifier.SendMessageAsync(
                $"📥 <b>Scrape manual completo</b>\n\n" +
                $"<b>{imported}</b> nova(s) oportunidade(s).\n" +
                $"<b>{skipped}</b> ignorada(s).",
                cancellationToken);

            return Results.Ok(new { imported, skipped, total = scraped.Count });
        })
        .WithName("RunScraper")
        .Produces<object>();
    }

    private static void UpdateNestedSetting(
        Dictionary<string, object> settings,
        string section,
        string key,
        string? value)
    {
        if (value is null) return;
        if (!settings.ContainsKey(section) || settings[section] is not Dictionary<string, object> nested)
        {
            nested = new Dictionary<string, object>();
            settings[section] = nested;
        }
        nested[key] = value;
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
    public string Technology { get; set; } = "";
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
    public string? Technology { get; set; }
    public int? MaxPages { get; set; }
    public bool? LoginEnabled { get; set; }
    public string? Email { get; set; }
    public int? ScrapeIntervalMinutes { get; set; }
}
