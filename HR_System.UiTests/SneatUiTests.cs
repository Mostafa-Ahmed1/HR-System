using Microsoft.Playwright;
using Xunit;

namespace HR_System.UiTests;

public sealed class SneatUiFixture : IAsyncLifetime
{
    public string? BaseUrl { get; } = Environment.GetEnvironmentVariable("HR_UI_BASE_URL");
    public string? Username { get; } = Environment.GetEnvironmentVariable("HR_UI_USERNAME");
    public string? Password { get; } = Environment.GetEnvironmentVariable("HR_UI_PASSWORD");
    public IPlaywright? Playwright { get; private set; }
    public IBrowser? Browser { get; private set; }

    public async Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl)) return;
        try
        {
            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        }
        catch
        {
            Browser = null;
        }
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null) await Browser.CloseAsync();
        Playwright?.Dispose();
    }

    public async Task<IPage> NewPageAsync(ViewportSize? viewport = null)
    {
        if (Browser is null) throw new InvalidOperationException("Chromium is not available.");
        return await Browser.NewPageAsync(new BrowserNewPageOptions { ViewportSize = viewport });
    }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class UiFactAttribute : FactAttribute
{
    public UiFactAttribute(bool requiresAuthentication = false)
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HR_UI_BASE_URL")))
            Skip = "Set HR_UI_BASE_URL to run browser UI tests.";
        else if (requiresAuthentication &&
                 (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HR_UI_USERNAME")) ||
                  string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HR_UI_PASSWORD"))))
            Skip = "Set HR_UI_USERNAME and HR_UI_PASSWORD to run authenticated UI tests.";
    }
}

public sealed class SneatUiTests : IClassFixture<SneatUiFixture>
{
    private readonly SneatUiFixture fixture;

    public SneatUiTests(SneatUiFixture fixture) => this.fixture = fixture;

    [UiFact]
    public async Task LoginPage_LoadsWithRequiredFieldsAndNoErrors()
    {
        var page = await fixture.NewPageAsync(new ViewportSize { Width = 1366, Height = 768 });
        var capture = Capture(page);
        var response = await page.GotoAsync(fixture.BaseUrl!, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.True(response?.Ok ?? false);
        Assert.True(await page.Locator("input[name='Username'], input[name='username'], input[type='text']").CountAsync() > 0);
        Assert.True(await page.Locator("input[name='Password'], input[name='password'], input[type='password']").CountAsync() > 0);
        Assert.True(await page.Locator("button[type='submit'], input[type='submit']").CountAsync() > 0);
        Assert.Equal(0, await HorizontalOverflow(page));
        capture.AssertClean();
        await page.SetViewportSizeAsync(390, 844);
        Assert.Equal(0, await HorizontalOverflow(page));
        capture.AssertClean();
    }

    [Fact]
    public async Task LayoutSource_UsesStablePageNames()
    {
        var source = await File.ReadAllTextAsync(FindRepositoryFile("HR_System/Views/Shared/_Layout.cshtml"));
        foreach (var pageId in Enumerable.Range(1, 7))
            Assert.DoesNotMatch($@"PageId\s*==\s*{pageId}\b", source);
        Assert.Contains("PageName", source, StringComparison.Ordinal);
    }

    [UiFact(true)]
    public async Task AuthenticatedDashboard_HasSneatShellAndInitializedMenu()
    {
        var page = await LoginAsync(new ViewportSize { Width = 1366, Height = 768 });
        Assert.True(await page.Locator(".layout-wrapper").IsVisibleAsync());
        Assert.True(await page.Locator("#layout-menu").IsVisibleAsync());
        Assert.True(await page.Locator(".layout-navbar").IsVisibleAsync());
        Assert.True(await page.Locator(".content-wrapper").IsVisibleAsync());
        Assert.True(await page.Locator("h2, h1").CountAsync() > 0);
        Assert.True(await page.Locator(".card").CountAsync() >= 4);
        Assert.Contains("Recent Attendance", await page.Locator("body").InnerTextAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ThemeSelection", await page.Locator("footer").InnerTextAsync());
        Assert.True(await page.EvaluateAsync<bool>("() => typeof window.Helpers !== 'undefined'"));
        Assert.True(await page.EvaluateAsync<bool>("() => typeof window.Menu !== 'undefined'"));
        Assert.True(await page.EvaluateAsync<bool>("() => !!window.Helpers.mainMenu"));
    }

    [UiFact(true)]
    public async Task SneatCriticalAssets_LoadWithoutErrorsOrDuplicates()
    {
        var page = await fixture.NewPageAsync();
        var capture = Capture(page);
        await page.GotoAsync(fixture.BaseUrl!, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        var resources = await page.EvaluateAsync<string[]>("() => performance.getEntriesByType('resource').map(e => e.name)");
        foreach (var asset in new[] { "core.css", "helpers.js", "config.js", "jquery.js", "popper.js", "bootstrap.js", "perfect-scrollbar.js", "menu.js", "main.js" })
            Assert.Contains(resources, url => url.Contains(asset, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, resources.Count(url => url.Contains("jquery", StringComparison.OrdinalIgnoreCase) && url.EndsWith(".js", StringComparison.OrdinalIgnoreCase)));
        Assert.Equal(1, resources.Count(url => url.Contains("bootstrap", StringComparison.OrdinalIgnoreCase) && url.EndsWith(".js", StringComparison.OrdinalIgnoreCase)));
        capture.AssertClean();
    }

    [UiFact(true)]
    public async Task Desktop_HasNoOverflowAndOneLogout()
    {
        var page = await LoginAsync(new ViewportSize { Width = 1366, Height = 768 });
        Assert.Equal(0, await HorizontalOverflow(page));
        Assert.True(await page.Locator("#layout-menu").BoundingBoxAsync() is not null);
        Assert.True(await page.Locator(".layout-navbar").BoundingBoxAsync() is not null);
        Assert.Equal(1, await page.Locator("form[action*='logout'] button[type='submit']").CountAsync());
        foreach (var card in await page.Locator(".card").AllAsync())
            Assert.True((await card.BoundingBoxAsync())?.Width > 0);
    }

    [UiFact(true)]
    public async Task VisibleNavigationItems_LoadInsideSneatShell()
    {
        var page = await LoginAsync();
        var links = await page.Locator("#layout-menu a.menu-link[href]").EvaluateAllAsync<string[]>("els => els.map(e => e.href)");
        foreach (var href in links.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var response = await page.GotoAsync(href, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            Assert.True(response?.Status < 400, $"{href} returned {response?.Status}");
            Assert.True(await page.Locator(".layout-wrapper").IsVisibleAsync());
            Assert.Equal(0, await HorizontalOverflow(page));
        }
    }

    [UiFact(true)]
    public async Task Logout_UsesPostAndEndsSession()
    {
        var page = await LoginAsync();
        var form = page.Locator("form[action*='logout']");
        Assert.Equal("post", (await form.GetAttributeAsync("method"))?.ToLowerInvariant());
        var requestTask = page.WaitForRequestAsync(request => request.Url.Contains("logout", StringComparison.OrdinalIgnoreCase));
        await form.Locator("button[type='submit']").ClickAsync();
        var request = await requestTask;
        Assert.Equal("POST", request.Method);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.Contains("login", page.Url, StringComparison.OrdinalIgnoreCase);
        await page.GotoAsync(fixture.BaseUrl!, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.Equal(0, await page.Locator("#layout-menu").CountAsync());
    }

    [UiFact(true)]
    public async Task Mobile_Menu_OpensClosesAndHasNoOverflow()
    {
        var page = await LoginAsync(new ViewportSize { Width = 390, Height = 844 });
        Assert.Equal(0, await HorizontalOverflow(page));
        var menu = page.Locator("#layout-menu");
        await page.Locator(".layout-menu-toggle").First.ClickAsync();
        Assert.True(await menu.IsVisibleAsync());
        Assert.True(await page.Locator(".layout-overlay").IsVisibleAsync());
        await page.Locator(".layout-overlay").ClickAsync(new LocatorClickOptions { Position = new Position { X = 3, Y = 3 } });
        Assert.False(await page.Locator(".layout-overlay").IsVisibleAsync());
        await page.Locator(".layout-menu-toggle").First.ClickAsync();
        await page.Locator("#layout-menu .layout-menu-toggle").ClickAsync();
        Assert.False(await page.Locator(".layout-overlay").IsVisibleAsync());
        Assert.Equal(0, await HorizontalOverflow(page));
    }

    [UiFact(true)]
    public async Task ResponsiveResize_RestoresDesktopLayout()
    {
        var page = await LoginAsync(new ViewportSize { Width = 1366, Height = 768 });
        Assert.True(await page.Locator("#layout-menu").IsVisibleAsync());
        await page.SetViewportSizeAsync(390, 844);
        Assert.True(await page.Locator(".layout-menu-toggle").First.IsVisibleAsync());
        await page.SetViewportSizeAsync(1366, 768);
        Assert.True(await page.Locator("#layout-menu").IsVisibleAsync());
        Assert.False(await page.Locator(".layout-overlay").IsVisibleAsync());
        Assert.Equal(0, await HorizontalOverflow(page));
    }

    private async Task<IPage> LoginAsync(ViewportSize? viewport = null)
    {
        var page = await fixture.NewPageAsync(viewport);
        var capture = Capture(page);
        var response = await page.GotoAsync(fixture.BaseUrl!, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.True(response?.Ok ?? false);
        var user = page.Locator("input[name='Username'], input[name='username'], input[type='text']");
        var password = page.Locator("input[name='Password'], input[name='password'], input[type='password']");
        var submit = page.Locator("button[type='submit'], input[type='submit']");
        Assert.True(await user.CountAsync() > 0);
        Assert.True(await password.CountAsync() > 0);
        Assert.True(await submit.CountAsync() > 0);
        await user.First.FillAsync(fixture.Username!);
        await password.First.FillAsync(fixture.Password!);
        await submit.First.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        capture.AssertClean();
        return page;
    }

    private static BrowserErrorCapture Capture(IPage page) => new(page);

    private static async Task<int> HorizontalOverflow(IPage page) =>
        await page.EvaluateAsync<int>("() => Math.max(0, document.documentElement.scrollWidth - window.innerWidth)");

    private static string FindRepositoryFile(string relative)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relative);
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException(relative);
    }
}

internal sealed class BrowserErrorCapture
{
    private readonly List<string> errors = [];

    public BrowserErrorCapture(IPage page)
    {
        page.PageError += (_, error) => errors.Add($"pageerror: {error}");
        page.Console += (_, message) =>
        {
            if (string.Equals(message.Type, "error", StringComparison.OrdinalIgnoreCase))
                errors.Add($"console: {message.Text}");
        };
        page.Response += (_, response) =>
        {
            if (response.Status >= 400 && response.Url.Contains("/sneat/", StringComparison.OrdinalIgnoreCase))
                errors.Add($"asset {response.Status}: {response.Url}");
        };
    }

    public void AssertClean() => Assert.Empty(errors);
}
