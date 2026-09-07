using FreelaHunter.Application;
using FreelaHunter.Application.Providers;
using FreelaHunter.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace FreelaHunter.Infrastructure.Providers.Nerdin;

public sealed class NerdinProvider : IJobProvider, IAsyncDisposable
{
    private readonly NerdinProviderOptions _options;
    private readonly CamoufoxOptions _camoufoxOptions;
    private readonly ILogger<NerdinProvider> _logger;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public NerdinProvider(
        IOptions<NerdinProviderOptions> options,
        IOptions<CamoufoxOptions> camoufoxOptions,
        ILogger<NerdinProvider> logger)
    {
        _options = options.Value;
        _camoufoxOptions = camoufoxOptions.Value;
        _logger = logger;
    }

    public string ProviderId => "nerdin";

    public string DisplayName => "Nerdin";

    public Marketplace Marketplace => Marketplace.Nerdin;

    public async Task<IReadOnlyList<ScrapedOpportunity>> FetchOpenJobsAsync(
        int maxPages,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Nerdin scraper, maxPages={MaxPages}", maxPages);

        await DisposeBrowserAsync();
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

                try
                {
                    await page.GotoAsync(url, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.Load,
                        Timeout = 45000
                    });

                    await page.WaitForTimeoutAsync(_options.PageDelayMs);

                    var cards = await page.QuerySelectorAllAsync(
                        ".job-card, .vaga-card, [class*='vaga'], [class*='job-item'], article[class*='card']");

                    if (cards.Count == 0)
                    {
                        _logger.LogWarning("No opportunity cards found on page {Page}", pageNum);
                        cards = await page.QuerySelectorAllAsync("a[href*='/vaga/'], a[href*='/job/']");
                    }

                    _logger.LogInformation("Found {Count} cards on page {Page}", cards.Count, pageNum);

                    foreach (var card in cards)
                    {
                        try
                        {
                            var opportunity = await ParseCardAsync(card);
                            if (opportunity is not null)
                                opportunities.Add(opportunity);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse Nerdin card");
                        }
                    }
                }
                catch (TimeoutException ex)
                {
                    _logger.LogWarning(ex, "Timeout on Nerdin page {Page}, continuing", pageNum);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error on Nerdin page {Page}, continuing", pageNum);
                    break;
                }

                if (pageNum < maxPages)
                    await page.WaitForTimeoutAsync(_options.PageDelayMs * 2);
            }

            if (_options.VisitDetails)
            {
                await VisitDetailPagesAsync(page, opportunities, cancellationToken);
            }
        }
        finally
        {
            await context.CloseAsync();
        }

        _logger.LogInformation("Nerdin scrape complete: {Count} opportunities", opportunities.Count);
        return opportunities;
    }

    public async Task<ScrapedOpportunity?> ExtractJobDetailsAsync(
        string jobIdOrUrl,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Extracting details for Nerdin job: {Url}", jobIdOrUrl);

        await DisposeBrowserAsync();
        await EnsureBrowserInitializedAsync(cancellationToken);

        var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
            Locale = "pt-BR"
        });

        var page = await context.NewPageAsync();

        try
        {
            await page.GotoAsync(jobIdOrUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.Load,
                Timeout = 30000
            });

            await page.WaitForTimeoutAsync(_options.DetailDelayMs);

            var titleEl = await page.QuerySelectorAsync("h1, .job-title, .vaga-title, [class*='title']");
            var title = titleEl is not null ? (await titleEl.TextContentAsync())?.Trim() ?? "" : "";

            var descEl = await page.QuerySelectorAsync(".job-description, .vaga-description, [class*='description']");
            var description = descEl is not null ? (await descEl.TextContentAsync())?.Trim() ?? "" : "";

            var skills = new List<string>();
            var skillEls = await page.QuerySelectorAllAsync(
                ".skill, .tag, .tecnologia, [class*='skill'], [class*='tag'], [class*='tech']");
            foreach (var s in skillEls)
            {
                var text = (await s.TextContentAsync())?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                    skills.Add(text);
            }

            var salaryEl = await page.QuerySelectorAsync(
                ".salary, .salario, [class*='salary'], [class*='remuneracao'], [class*='budget']");
            var budgetSummary = salaryEl is not null ? (await salaryEl.TextContentAsync())?.Trim() : null;

            var locationEl = await page.QuerySelectorAsync(
                ".location, .localizacao, [class*='location'], [class*='cidade']");
            var clientCountry = locationEl is not null ? (await locationEl.TextContentAsync())?.Trim() : null;

            DateTime? publishedAt = null;
            var dateEl = await page.QuerySelectorAsync("time, [class*='date'], [class*='time']");
            if (dateEl is not null)
            {
                var dateStr = await dateEl.GetAttributeAsync("datetime") ?? (await dateEl.TextContentAsync())?.Trim();
                if (DateTime.TryParse(dateStr, out var parsed))
                    publishedAt = parsed;
            }

            return new ScrapedOpportunity
            {
                ProviderId = ProviderId,
                Title = title,
                Description = description.Length > 5000 ? description[..5000] : description,
                RequiredSkills = skills,
                BudgetSummary = budgetSummary,
                ClientCountry = clientCountry,
                ExternalUrl = jobIdOrUrl,
                PublishedAt = publishedAt
            };
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    public Task<bool> SubmitApplicationAsync(
        JobApplicationData applicationData,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "NerdinProvider.SubmitApplicationAsync not yet implemented for {Title}",
            applicationData.OpportunityTitle);
        return Task.FromResult(false);
    }

    private async Task VisitDetailPagesAsync(
        IPage page,
        List<ScrapedOpportunity> opportunities,
        CancellationToken cancellationToken)
    {
        var indicesToVisit = opportunities
            .Select((o, i) => new { o.ExternalUrl, Index = i })
            .Where(o => !string.IsNullOrWhiteSpace(o.ExternalUrl))
            .Take(20)
            .Select(o => o.Index)
            .ToList();

        _logger.LogInformation("Visiting {Count} Nerdin detail pages...", indicesToVisit.Count);

        foreach (var idx in indicesToVisit)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var opp = opportunities[idx];
            try
            {
                _logger.LogDebug("Visiting Nerdin detail: {Url}", opp.ExternalUrl);
                await page.GotoAsync(opp.ExternalUrl!, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.Load,
                    Timeout = 30000
                });

                await page.WaitForTimeoutAsync(_options.DetailDelayMs);

                var fullDesc = await page.QuerySelectorAsync(
                    ".job-description, .vaga-description, [class*='description']");
                if (fullDesc is not null)
                {
                    var descText = (await fullDesc.TextContentAsync())?.Trim();
                    if (!string.IsNullOrWhiteSpace(descText) && descText.Length > opp.Description.Length)
                    {
                        opportunities[idx] = opp with
                        {
                            Description = descText.Length > 5000 ? descText[..5000] : descText
                        };
                        opp = opportunities[idx];
                    }
                }

                var skillsEl = await page.QuerySelectorAllAsync(
                    ".skill, .tag, .tecnologia, [class*='skill'], [class*='tag'], [class*='tech']");
                if (skillsEl.Count > opp.RequiredSkills.Count)
                {
                    var newSkills = new List<string>();
                    foreach (var s in skillsEl)
                    {
                        var text = (await s.TextContentAsync())?.Trim();
                        if (!string.IsNullOrWhiteSpace(text))
                            newSkills.Add(text);
                    }
                    if (newSkills.Count > 0)
                    {
                        opportunities[idx] = opp with { RequiredSkills = newSkills };
                        opp = opportunities[idx];
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to visit Nerdin detail page: {Url}", opp.ExternalUrl);
            }
        }
    }

    private async Task<ScrapedOpportunity?> ParseCardAsync(IElementHandle card)
    {
        var titleEl = await card.QuerySelectorAsync(
            "h2, h3, .job-title, .vaga-title, [class*='title'] a, a[href*='/vaga/']");
        var title = titleEl is not null ? (await titleEl.TextContentAsync())?.Trim() : null;
        if (string.IsNullOrWhiteSpace(title))
            return null;

        var linkEl = await card.QuerySelectorAsync("a[href*='/vaga/'], a[href*='/job/']");
        var href = linkEl is not null ? await linkEl.GetAttributeAsync("href") : null;
        var externalUrl = !string.IsNullOrWhiteSpace(href)
            ? (href.StartsWith("http") ? href : $"https://www.nerdin.com.br{href}")
            : null;

        var descEl = await card.QuerySelectorAsync(
            ".job-description, [class*='description'], .text-muted, p");
        var description = descEl is not null ? (await descEl.TextContentAsync())?.Trim() ?? "" : "";

        var skills = new List<string>();
        var skillEls = await card.QuerySelectorAllAsync(
            ".skill, .tag, .tecnologia, [class*='skill'], [class*='tag'], [class*='tech']");
        foreach (var skillEl in skillEls)
        {
            var skillText = (await skillEl.TextContentAsync())?.Trim();
            if (!string.IsNullOrWhiteSpace(skillText))
                skills.Add(skillText);
        }

        var salaryEl = await card.QuerySelectorAsync(
            ".salary, .salario, [class*='salary'], [class*='remuneracao'], [class*='budget']");
        var budgetSummary = salaryEl is not null ? (await salaryEl.TextContentAsync())?.Trim() : null;

        var locationEl = await card.QuerySelectorAsync(
            ".location, .localizacao, [class*='location'], [class*='cidade']");
        var clientCountry = locationEl is not null ? (await locationEl.TextContentAsync())?.Trim() : null;

        DateTime? publishedAt = null;
        var dateEl = await card.QuerySelectorAsync("time, [class*='date'], [class*='time']");
        if (dateEl is not null)
        {
            var dateStr = await dateEl.GetAttributeAsync("datetime") ?? (await dateEl.TextContentAsync())?.Trim();
            if (DateTime.TryParse(dateStr, out var parsed))
                publishedAt = parsed;
        }

        return new ScrapedOpportunity
        {
            ProviderId = ProviderId,
            Title = title,
            Description = description.Length > 2000 ? description[..2000] : description,
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
        var queryParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(_options.Technology))
            queryParts.Add($"tecnologia={Uri.EscapeDataString(_options.Technology)}");

        if (!string.IsNullOrWhiteSpace(_options.Location))
            queryParts.Add($"localizacao={Uri.EscapeDataString(_options.Location)}");

        if (page > 1)
            queryParts.Add($"page={page}");

        if (queryParts.Count > 0)
            url += "?" + string.Join("&", queryParts);

        return url;
    }

    private async Task EnsureBrowserInitializedAsync(CancellationToken cancellationToken)
    {
        _playwright ??= await Playwright.CreateAsync();

        if (_camoufoxOptions.Enabled)
        {
            _logger.LogInformation(
                "Connecting to Camoufox server at {ServerUrl}",
                _camoufoxOptions.ServerUrl);

            _browser = await _playwright.Chromium.ConnectOverCDPAsync(
                _camoufoxOptions.ServerUrl,
                new BrowserTypeConnectOverCDPOptions
                {
                    Timeout = _camoufoxOptions.ConnectTimeoutSeconds * 1000
                });
        }
        else
        {
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = _options.Headless,
                Args = ["--disable-blink-features=AutomationControlled"]
            });
        }
    }

    private async Task DisposeBrowserAsync()
    {
        if (_browser is not null)
        {
            try { await _browser.CloseAsync(); } catch { }
            _browser = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeBrowserAsync();
        _playwright?.Dispose();
        _playwright = null;
    }
}
