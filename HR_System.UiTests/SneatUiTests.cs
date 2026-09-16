using Microsoft.Playwright;
using Xunit;
using Xunit.Sdk;

namespace HR_System.UiTests;

public sealed class SneatUiFixture : IAsyncLifetime
{
    public string? BaseUrl { get; } = Environment.GetEnvironmentVariable("HR_UI_BASE_URL");
    public string? Username { get; } = Environment.GetEnvironmentVariable("HR_UI_USERNAME");
    public string? Password { get; } = Environment.GetEnvironmentVariable("HR_UI_PASSWORD");
    public IPlaywright? Playwright { get; private set; }
    public IBrowser? Browser { get; private set; }
    public string? SetupFailure { get; private set; }

    public async Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl)) return;
        try
        {
            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        }
        catch (Exception ex)
        {
            SetupFailure = $"Chromium unavailable: {ex.Message}";
        }
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null) await Browser.CloseAsync();
        Playwright?.Dispose();
    }

    public async Task<IPage> NewPageAsync(ViewportSize? viewport = null)
    {
        SkipIfUnavailable();
        var page = await Browser!.NewPageAsync(new BrowserNewPageOptions { ViewportSize = viewport });
        return page;
    }

    public void SkipIfUnavailable(bool authenticated = false)
    {
        if (string.IsNullOrWhiteSpace(BaseUrl)) throw new SkipException("Set HR_UI_BASE_URL to run browser UI tests.");
        if (authenticated && (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password)))
            throw new SkipException("Set HR_UI_USERNAME and HR_UI_PASSWORD to run authenticated UI tests.");
        if (SetupFailure is not null) throw new SkipException(SetupFailure);
    }
}

public sealed class SneatUiTests : IClassFixture<SneatUiFixture>
{
    private readonly SneatUiFixture fixture;
    public SneatUiTests(SneatUiFixture fixture) => this.fixture = fixture;

    [Fact]
    public async Task LoginPage_LoadsWithoutCriticalErrorsOrOverflow()
    {
        var page = await fixture.NewPageAsync(new ViewportSize { Width = 1366, Height = 768 });
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);
        var response = await page.GotoAsync(fixture.BaseUrl!, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.True(response?.Ok ?? false);
        Assert.NotNull(await page.Locator("input[type='password']").CountAsync());
        Assert.Equal(0, await page.EvaluateAsync<int>("() => document.documentElement.scrollWidth - window.innerWidth"));
        Assert.Empty(errors);
    }

    [Fact]
    public async Task LayoutSource_UsesStablePageNames()
    {
        var path = FindRepositoryFile("HR_System/Views/Shared/_Layout.cshtml");
        var source = await File.ReadAllTextAsync(path);
        Assert.DoesNotMatch(@"PageIds*==s*[1-7]", source);
        Assert.Contains("PageName", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthenticatedDashboard_HasSneatShellAndInitializedMenu()
    {
        var page = await LoginAsync(new ViewportSize { Width = 1366, Height = 768 });
        Assert.True(await page.Locator(".layout-wrapper").CountAsync() > 0);
        Assert.True(await page.Locator("#layout-menu").CountAsync() > 0);
        Assert.True(await page.Locator(".layout-navbar").CountAsync() > 0);
        Assert.True(await page.Locator(".content-wrapper").CountAsync() > 0);
        Assert.True(await page.Locator("h2").CountAsync() > 0);
        Assert.True(await page.Locator(".card").CountAsync() >= 4);
        Assert.Contains("ThemeSelection", await page.Locator("footer").InnerTextAsync());
        Assert.Equal(true, await page.EvaluateAsync<bool>("() => typeof window.Helpers !== 'undefined'"));
        Assert.Equal(true, await page.EvaluateAsync<bool>("() => typeof window.Menu !== 'undefined'"));
        Assert.Equal(true, await page.EvaluateAsync<bool>("() => !!window.Helpers.mainMenu"));
    }

    [Fact]
    public async Task SneatCriticalAssets_LoadWithoutErrorsOrDuplicates()
    {
        var page = await LoginAsync();
        var failed = new List<string>();
        page.Response += (_, response) => { if (response.Status >= 400) failed.Add($"{response.Status} {response.Url}"); };
        await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.NetworkIdle });
        Assert.DoesNotContain(failed, item => item.Contains("/sneat/", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, await page.Locator("script[src*='jquery']").CountAsync());
        Assert.Equal(1, await page.Locator("script[src*='bootstrap']").CountAsync());
    }

    [Fact]
    public async Task Desktop_HasNoHorizontalOverflowAndOnePostLogout()
    {
        var page = await LoginAsync(new ViewportSize { Width = 1366, Height = 768 });
        Assert.Equal(0, await page.EvaluateAsync<int>("() => document.documentElement.scrollWidth - window.innerWidth"));
        Assert.True(await page.Locator("#layout-menu").BoundingBoxAsync() is not null);
        Assert.Equal(1, await page.Locator("form[action*='logout'] button[type='submit']").CountAsync());
        var cards = await page.Locator(".card").AllAsync();
        foreach (var card in cards) Assert.True((await card.BoundingBoxAsync())?.Width > 0);
    }

    [Fact]
    public async Task Mobile_Menu_OpensClosesAndHasNoOverflow()
    {
        var page = await LoginAsync(new ViewportSize { Width = 390, Height = 844 });
        var menu = page.Locator("#layout-menu");
        await page.Locator("[data-sneat-menu-toggle]").First.ClickAsync();
        Assert.True(await menu.IsVisibleAsync());
        Assert.True(await page.Locator(".layout-overlay").IsVisibleAsync());
        await page.Locator(".layout-overlay").ClickAsync(new LocatorClickOptions { Position = new Position { X = 3, Y = 3 } });
        Assert.Equal(0, await page.EvaluateAsync<int>("() => document.documentElement.scrollWidth - window.innerWidth"));
        await page.Keyboard.PressAsync("Escape");
    }

    [Fact]
    public async Task VisibleNavigationItems_LoadInsideSneatShell()
    {
        var page = await LoginAsync();
        var links = await page.Locator("#layout-menu a.menu-link[href]").EvaluateAllAsync<string[]>("els => els.map(e => e.href)");
        foreach (var href in links.Distinct())
        {
            var response = await page.GotoAsync(href, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            Assert.True(response?.Status < 400, $"{href} returned {response?.Status}");
            Assert.True(await page.Locator(".layout-wrapper").CountAsync() > 0);
        }
    }

    [Fact]
    public async Task Logout_UsesPostForm()
    {
        var page = await LoginAsync();
        var form = page.Locator("form[action*='logout']");
        Assert.Equal("post", (await form.GetAttributeAsync("method"))?.ToLowerInvariant());
        Assert.True(await form.Locator("button[type='submit']").CountAsync() == 1);
    }

    private async Task<IPage> LoginAsync(ViewportSize? viewport = null)
    {
        fixture.SkipIfUnavailable(authenticated: true);
        var page = await fixture.NewPageAsync(viewport);
        await page.GotoAsync(fixture.BaseUrl!, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("input[name='Username'], input[name='username'], input[type='text']").First.FillAsync(fixture.Username!);
        await page.Locator("input[name='Password'], input[name='password'], input[type='password']").First.FillAsync(fixture.Password!);
        await page.Locator("button[type='submit'], input[type='submit']").First.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return page;
    }

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

