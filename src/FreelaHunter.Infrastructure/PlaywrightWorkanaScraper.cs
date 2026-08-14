using FreelaHunter.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace FreelaHunter.Infrastructure;

public sealed class PlaywrightWorkanaScraper : IWorkanaScraper, IAsyncDisposable
{
    private readonly WorkanaScraperOptions _options;
    private readonly ILogger<PlaywrightWorkanaScraper> _logger;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public PlaywrightWorkanaScraper(
        IOptions<WorkanaScraperOptions> options,
        ILogger<PlaywrightWorkanaScraper> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ScrapedOpportunity>> ScrapeAsync(
        int maxPages,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Workana scraper, maxPages={MaxPages}", maxPages);

        await EnsureBrowserInitializedAsync(cancellationToken);

        var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
            Locale = "pt-BR"
        });

        var page = await context.NewPageAsync();
        var opportunities = new List<ScrapedOpportunity>();

        try
        {
            for (var pageNum = 1; pageNum <= maxPages; pageNum++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var url = BuildPageUrl(pageNum);
                _logger.LogInformation("Navigating to page {Page}: {Url}", pageNum, url);

                await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = 30000
                });

                await page.WaitForTimeoutAsync(_options.PageDelayMs);

                var cards = await page.QuerySelectorAllAsync(".project-item, .job-item, [class*='project'], article[class*='card']");

                if (cards.Count == 0)
                {
                    _logger.LogWarning("No opportunity cards found on page {Page}", pageNum);
                    cards = await page.QuerySelectorAllAsync("a[href*='/jobs/']");
                }

                _logger.LogInformation("Found {Count} cards on page {Page}", cards.Count, pageNum);

                foreach (var card in cards)
                {
                    try
                    {
                        var opportunity = await ParseCardAsync(card, cancellationToken);
                        if (opportunity is not null)
                            opportunities.Add(opportunity);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse card");
                    }
                }

                if (pageNum < maxPages)
                    await page.WaitForTimeoutAsync(_options.PageDelayMs * 2);
            }
        }
        finally
        {
            await context.CloseAsync();
        }

        _logger.LogInformation("Scraped {Count} opportunities total", opportunities.Count);
        return opportunities;
    }

    private async Task<ScrapedOpportunity?> ParseCardAsync(IElementHandle card, CancellationToken cancellationToken)
    {
        var titleEl = await card.QuerySelectorAsync("h2, h3, .project-title, [class*='title'] a, a[href*='/jobs/']");
        var title = titleEl is not null ? (await titleEl.TextContentAsync())?.Trim() : null;
        if (string.IsNullOrWhiteSpace(title))
            return null;

        var linkEl = await card.QuerySelectorAsync("a[href*='/jobs/']");
        var href = linkEl is not null ? await linkEl.GetAttributeAsync("href") : null;
        var externalUrl = !string.IsNullOrWhiteSpace(href)
            ? (href.StartsWith("http") ? href : $"https://www.workana.com{href}")
            : null;

        var descEl = await card.QuerySelectorAsync(".project-description, [class*='description'], .text-muted, p");
        var description = descEl is not null ? (await descEl.TextContentAsync())?.Trim() ?? "" : "";

        var skills = new List<string>();
        var skillEls = await card.QuerySelectorAllAsync(".skill, .tag, [class*='skill'], [class*='tag'], .badge");
        foreach (var skillEl in skillEls)
        {
            var skillText = (await skillEl.TextContentAsync())?.Trim();
            if (!string.IsNullOrWhiteSpace(skillText))
                skills.Add(skillText);
        }

        var budgetEl = await card.QuerySelectorAsync(".budget, [class*='budget'], [class*='price'], .amount");
        var budgetSummary = budgetEl is not null ? (await budgetEl.TextContentAsync())?.Trim() : null;

        var categoryEl = await card.QuerySelectorAsync(".category, [class*='category']");
        var category = categoryEl is not null ? (await categoryEl.TextContentAsync())?.Trim() : null;

        var countryEl = await card.QuerySelectorAsync(".country, [class*='country'], [class*='location']");
        var clientCountry = countryEl is not null ? (await countryEl.TextContentAsync())?.Trim() : null;

        DateTime? publishedAt = null;
        var dateEl = await card.QuerySelectorAsync("time, [class*='date'], [class*='time'], .timeago");
        if (dateEl is not null)
        {
            var dateStr = await dateEl.GetAttributeAsync("datetime") ?? (await dateEl.TextContentAsync())?.Trim();
            if (DateTime.TryParse(dateStr, out var parsed))
                publishedAt = parsed;
        }

        return new ScrapedOpportunity
        {
            Title = title,
            Description = description.Length > 2000 ? description[..2000] : description,
            Category = category,
            RequiredSkills = skills,
            BudgetSummary = budgetSummary,
            ClientCountry = clientCountry,
            ExternalUrl = externalUrl,
            PublishedAt = publishedAt
        };
    }

    private string BuildPageUrl(int page)
    {
        var url = _options.BaseUrl;
        if (!string.IsNullOrWhiteSpace(_options.Technology))
            url += $"/{_options.Technology}";
        if (page > 1)
            url += $"?page={page}";
        return url;
    }

    private async Task EnsureBrowserInitializedAsync(CancellationToken cancellationToken)
    {
        if (_browser is not null && _browser.IsConnected)
            return;

        _playwright ??= await Playwright.CreateAsync();
        _browser ??= await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = _options.Headless,
            Args = ["--disable-blink-features=AutomationControlled"]
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
            _browser = null;
        }
        _playwright?.Dispose();
        _playwright = null;
    }
}
