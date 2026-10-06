using System.Collections.Concurrent;
using Microsoft.Playwright;

namespace Papra.Companion.Tests.Browser;

public abstract class BrowserTest
{
    public const int DesktopWidth = 1280;
    public const int PhoneWidth = 390;
    public const int WideWidth = 1600;

    private readonly Chromium _chromium;
    private readonly ConcurrentQueue<string> _browserErrors = new();

    static BrowserTest()
    {
        Assertions.SetDefaultExpectTimeout(15000);
    }

    protected BrowserTest(Chromium chromium)
    {
        _chromium = chromium;
    }

    protected static string ScreenshotFolder { get; } = Path.Combine(AppContext.BaseDirectory, "TestResults", "browser");

    protected async Task<IBrowserContext> OpenBrowserAsync(BrowserApp app, BrowserTheme? savedTheme, int width = DesktopWidth, ColorScheme systemTheme = ColorScheme.Light)
    {
        if (_chromium.Browser is not { } browser)
        {
            Assert.Skip(_chromium.Unavailable);
            throw new InvalidOperationException(_chromium.Unavailable);
        }

        var appAddress = app.BaseAddress.ToString();
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = appAddress,
            ViewportSize = new ViewportSize { Width = width, Height = width == PhoneWidth ? 844 : 900 },
            ColorScheme = systemTheme,
            Locale = "en-GB"
        });
        context.SetDefaultTimeout(15000);
        await context.RouteAsync(url => !url.StartsWith(appAddress, StringComparison.Ordinal), AnswerOutsideRequestAsync);
        if (savedTheme is { } theme)
            await context.AddInitScriptAsync($"if (location.origin !== 'null') localStorage.setItem('color-theme', '{theme.ToString().ToLowerInvariant()}')");
        context.Page += (_, page) => Watch(page);
        return context;
    }

    protected static Task WaitForThemeAsync(IPage page, BrowserTheme theme)
    {
        var name = theme.ToString().ToLowerInvariant();
        var background = theme == BrowserTheme.Dark ? "rgb(11, 11, 11)" : "rgb(247, 247, 248)";
        return page.WaitForFunctionAsync(
            $"document.querySelector('.mud-layout')?.dataset.theme === '{name}' && getComputedStyle(document.body).backgroundColor === '{background}'");
    }

    protected void AssertNoBrowserErrors() => Assert.True(_browserErrors.IsEmpty, string.Join(Environment.NewLine, _browserErrors));

    protected static async Task<bool> ShowsAnErrorAsync(IPage page) =>
        await page.GetByText("This page ran into a problem").CountAsync() > 0 || await page.Locator("#blazor-error-ui").IsVisibleAsync();

    protected static Task<bool> ScrollsSidewaysAsync(IPage page) =>
        page.EvaluateAsync<bool>("document.documentElement.scrollWidth > document.documentElement.clientWidth");

    protected static Task<bool> HasSettingsOutOfLineAsync(IPage page) =>
        page.EvaluateAsync<bool>("""
            () => {
                const status = document.querySelector('.pc-status-overview');
                const content = document.querySelector('.pc-settings-content');
                return !!status && !!content && Math.abs(status.getBoundingClientRect().right - content.getBoundingClientRect().right) > 1;
            }
            """);

    protected static Task<bool> HasTableWiderThanItsCardAsync(IPage page) =>
        page.EvaluateAsync<bool>("[...document.querySelectorAll('.mud-table-container')].some(c => c.scrollWidth > c.clientWidth)");

    private async Task AnswerOutsideRequestAsync(IRoute route)
    {
        _browserErrors.Enqueue($"The page asked another site for {route.Request.Url}");
        await route.AbortAsync();
    }

    private void Watch(IPage page)
    {
        page.Console += (_, message) =>
        {
            if (message.Type == "error") _browserErrors.Enqueue($"{page.Url}: {message.Text}");
        };
        page.PageError += (_, error) => _browserErrors.Enqueue($"{page.Url}: {error}");
    }
}
