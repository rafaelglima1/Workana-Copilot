using FreelaHunter.Application;
using FreelaHunter.Application.Providers;
using FreelaHunter.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace FreelaHunter.Infrastructure.Providers.Workana;

#pragma warning disable CS0618 // Intentional backward compatibility with legacy IWorkanaScraper
public sealed class WorkanaProvider : IWorkanaScraper, IJobProvider, IAsyncDisposable
{
    private readonly WorkanaProviderOptions _options;
    private readonly CamoufoxOptions _camoufoxOptions;
    private readonly ILogger<WorkanaProvider> _logger;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public WorkanaProvider(
        IOptions<WorkanaProviderOptions> options,
        IOptions<CamoufoxOptions> camoufoxOptions,
        ILogger<WorkanaProvider> logger)
    {
        _options = options.Value;
        _camoufoxOptions = camoufoxOptions.Value;
        _logger = logger;
    }

    public string ProviderId => "workana";

    public string DisplayName => "Workana";

    public Marketplace Marketplace => Marketplace.Workana;

    public async Task<IReadOnlyList<ScrapedOpportunity>> ScrapeAsync(
        int maxPages,
        CancellationToken cancellationToken)
    {
        return await FetchOpenJobsAsync(maxPages, cancellationToken);
    }

    public async Task<IReadOnlyList<ScrapedOpportunity>> FetchOpenJobsAsync(
        int maxPages,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Workana scraper, maxPages={MaxPages}", maxPages);

        await DisposeBrowserAsync();
        await EnsureBrowserInitializedAsync(cancellationToken);

        var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
            Locale = "pt-BR"
        });

        var page = await context.NewPageAsync();

        if (_options.LoginEnabled && !string.IsNullOrWhiteSpace(_options.Email))
        {
            await LoginAsync(page, cancellationToken);
        }

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

                    var cards = await page.QuerySelectorAllAsync(".project-item, .job-item, [class*='project'], article[class*='card']");

                    if (cards.Count == 0)
                    {
                        _logger.LogWarning("No opportunity cards found on page {Page}", pageNum);
                        cards = await page.QuerySelectorAllAsync("a[href*='/job/']");
                    }

                    if (pageNum == 1)
                    {
                        _logger.LogWarning("Saving debug HTML for page {Page} (found {Count} cards)", pageNum, cards.Count);
                        var html = await page.ContentAsync();
                        var debugPath = Path.Combine(Path.GetTempPath(), "workana-debug-latest.html");
                        await File.WriteAllTextAsync(debugPath, html);
                        _logger.LogWarning("HTML saved to {Path}", debugPath);
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
                            _logger.LogWarning(ex, "Failed to parse card");
                        }
                    }
                }
                catch (TimeoutException ex)
                {
                    _logger.LogWarning(ex, "Timeout on page {Page}, continuing with results so far", pageNum);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error on page {Page}, continuing with results so far", pageNum);
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

        _logger.LogInformation("Scraped {Count} opportunities total", opportunities.Count);
        return opportunities;
    }

    public async Task<ScrapedOpportunity?> ExtractJobDetailsAsync(
        string jobIdOrUrl,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Extracting details for Workana job: {Url}", jobIdOrUrl);

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

            var titleEl = await page.QuerySelectorAsync("h1, .project-title, [class*='title']");
            var title = titleEl is not null ? (await titleEl.TextContentAsync())?.Trim() ?? "" : "";

            var descEl = await page.QuerySelectorAsync(".project-description, .job-description, [class*='description']");
            var description = descEl is not null ? (await descEl.TextContentAsync())?.Trim() ?? "" : "";

            var skills = new List<string>();
            var skillEls = await page.QuerySelectorAllAsync(".skill, .tag, [class*='skill'], [class*='tag'], .badge");
            foreach (var s in skillEls)
            {
                var text = (await s.TextContentAsync())?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                    skills.Add(text);
            }

            var budgetEl = await page.QuerySelectorAsync(".budget, [class*='budget'], [class*='price']");
            var budgetSummary = budgetEl is not null ? (await budgetEl.TextContentAsync())?.Trim() : null;

            var clientEl = await page.QuerySelectorAsync(".client-info, [class*='client'], .company-name");
            var clientCountry = clientEl is not null ? (await clientEl.TextContentAsync())?.Trim() : null;

            DateTime? publishedAt = null;
            var dateEl = await page.QuerySelectorAsync("time, [class*='date'], [class*='time'], .timeago");
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
            "WorkanaProvider.SubmitApplicationAsync not yet implemented for {Title}",
            applicationData.OpportunityTitle);
        return Task.FromResult(false);
    }

    private async Task LoginAsync(IPage page, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Logging into Workana...");

        try
        {
            await page.GotoAsync("https://www.workana.com/login", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.Load,
                Timeout = 30000
            });

            await page.WaitForTimeoutAsync(2000);

            await page.FillAsync("input[name='email'], input[type='email'], #email", _options.Email!);
            await page.WaitForTimeoutAsync(500);

            if (!string.IsNullOrWhiteSpace(_options.Password))
            {
                await page.FillAsync("input[name='password'], input[type='password'], #password", _options.Password);
                await page.WaitForTimeoutAsync(500);
            }

            await page.ClickAsync("button[type='submit'], input[type='submit']");
            await page.WaitForTimeoutAsync(5000);

            _logger.LogInformation("Workana login completed.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Workana login failed. Continuing without authentication.");
        }
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

        _logger.LogInformation("Visiting {Count} detail pages...", indicesToVisit.Count);

        foreach (var idx in indicesToVisit)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var opp = opportunities[idx];
            try
            {
                _logger.LogDebug("Visiting detail: {Url}", opp.ExternalUrl);
                await page.GotoAsync(opp.ExternalUrl!, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.Load,
                    Timeout = 30000
                });

                await page.WaitForTimeoutAsync(_options.DetailDelayMs);

                var fullDesc = await page.QuerySelectorAsync(".project-description, .job-description, [class*='description']");
                if (fullDesc is not null)
                {
                    var descText = (await fullDesc.TextContentAsync())?.Trim();
                    if (!string.IsNullOrWhiteSpace(descText) && descText.Length > opp.Description.Length)
                    {
                        opportunities[idx] = opp with { Description = descText.Length > 5000 ? descText[..5000] : descText };
                        opp = opportunities[idx];
                    }
                }

                var skillsEl = await page.QuerySelectorAllAsync(".skill, .tag, [class*='skill'], [class*='tag'], .badge");
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

                var budgetEl = await page.QuerySelectorAsync(".budget, [class*='budget'], [class*='price']");
                if (budgetEl is not null)
                {
                    var budgetText = (await budgetEl.TextContentAsync())?.Trim();
                    if (!string.IsNullOrWhiteSpace(budgetText))
                    {
                        opportunities[idx] = opp with { BudgetSummary = budgetText };
                        opp = opportunities[idx];
                    }
                }

                var clientEl = await page.QuerySelectorAsync(".client-info, [class*='client'], .company-name");
                if (clientEl is not null)
                {
                    var clientText = (await clientEl.TextContentAsync())?.Trim();
                    if (!string.IsNullOrWhiteSpace(clientText))
                    {
                        var parts = clientText.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0)
                            opportunities[idx] = opp with { ClientCountry = parts[0].Trim() };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to visit detail page: {Url}", opp.ExternalUrl);
            }
        }
    }

    private async Task<ScrapedOpportunity?> ParseCardAsync(IElementHandle card)
    {
        var titleEl = await card.QuerySelectorAsync("h2, h3, .project-title, [class*='title'] a, a[href*='/job/']");
        var title = titleEl is not null ? (await titleEl.TextContentAsync())?.Trim() : null;
        if (string.IsNullOrWhiteSpace(title))
            return null;

        var linkEl = await card.QuerySelectorAsync("a[href*='/job/']");
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
            ProviderId = ProviderId,
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
        var queryParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(_options.Language))
            queryParts.Add($"language={Uri.EscapeDataString(_options.Language)}");

        if (!string.IsNullOrWhiteSpace(_options.Skills))
            queryParts.Add($"skills={Uri.EscapeDataString(_options.Skills)}");

        if (!string.IsNullOrWhiteSpace(_options.Sort))
            queryParts.Add($"sort={Uri.EscapeDataString(_options.Sort)}");

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
