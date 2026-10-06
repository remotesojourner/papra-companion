using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Papra.Companion.Tests.Browser;

[Collection(BrowserTestGroup.Name)]
[Trait("Category", "Browser")]
public sealed class ThemeTests : BrowserTest, IClassFixture<BrowserApp>
{
    private readonly BrowserApp _app;

    public ThemeTests(BrowserApp app, Chromium chromium) : base(chromium)
    {
        _app = app;
    }

    [Theory]
    [InlineData(ColorScheme.Dark, BrowserTheme.Dark)]
    [InlineData(ColorScheme.Light, BrowserTheme.Light)]
    public async Task ThemeWithNothingSavedFollowsTheSystem(ColorScheme system, BrowserTheme expected)
    {
        await using var browser = await OpenBrowserAsync(_app, savedTheme: null, systemTheme: system);
        var page = await browser.NewPageAsync();

        await page.GotoAsync("/");

        await WaitForThemeAsync(page, expected);
        AssertNoBrowserErrors();
    }

    [Fact]
    public async Task ThemeToggleIsRememberedAfterReload()
    {
        await using var browser = await OpenBrowserAsync(_app, savedTheme: null, systemTheme: ColorScheme.Dark);
        var page = await browser.NewPageAsync();
        await page.GotoAsync("/");
        await WaitForThemeAsync(page, BrowserTheme.Dark);

        await page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await WaitForThemeAsync(page, BrowserTheme.Light);
        await page.ReloadAsync();

        await Expect(page.GetByText("Recent Activity")).ToBeVisibleAsync();
        await WaitForThemeAsync(page, BrowserTheme.Light);
        Assert.Equal("light", await page.EvaluateAsync<string>("localStorage.getItem('color-theme')"));
        AssertNoBrowserErrors();
    }
}
