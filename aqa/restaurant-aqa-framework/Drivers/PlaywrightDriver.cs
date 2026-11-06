using Microsoft.Playwright;
using NUnit.Framework;
using RestaurantAqaFramework.Configuration;

namespace RestaurantAqaFramework.Drivers;

public class PlaywrightDriver : IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private IAPIRequestContext? _apiContext;

    public IPage Page => _page ?? throw new InvalidOperationException("Page is not initialized.");
    public IBrowserContext Context => _context ?? throw new InvalidOperationException("Context is not initialized.");
    public IAPIRequestContext ApiContext => _apiContext ?? throw new InvalidOperationException("ApiContext is not initialized.");

    public async Task InitializeAsync(string scenarioTitle)
    {
        _playwright = await Playwright.CreateAsync();

        var browserType = TestConfiguration.Browser.ToLower() switch
        {
            "firefox" => _playwright.Firefox,
            "webkit" => _playwright.Webkit,
            _ => _playwright.Chromium
        };

        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = TestConfiguration.Headless,
            SlowMo = TestConfiguration.SlowMo
        };

        _browser = await browserType.LaunchAsync(launchOptions);

        var sanitizedTitle = SanitizeFileName(scenarioTitle);
        var videoDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResults", "videos", sanitizedTitle);
        Directory.CreateDirectory(videoDir);

        var contextOptions = new BrowserNewContextOptions
        {
            RecordVideoDir = videoDir,
            RecordVideoSize = new RecordVideoSize { Width = 1280, Height = 720 },
            BaseURL = TestConfiguration.BaseUrl
        };

        _context = await _browser.NewContextAsync(contextOptions);
        _context.SetDefaultTimeout(TestConfiguration.DefaultTimeout);

        // Start tracing
        await _context.Tracing.StartAsync(new()
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true,
            Title = scenarioTitle
        });

        _page = await _context.NewPageAsync();

        _page.Request += (_, req) =>
            TestContext.Progress.WriteLine($"--> [REQ] {req.Method} {req.Url}");

        _page.Response += (_, res) =>
            TestContext.Progress.WriteLine($"<-- [RES] {res.Status} {res.Url}");

        _page.Console += (_, msg) =>
            TestContext.Progress.WriteLine($"[CONSOLE] {msg.Type}: {msg.Text}");

        _page.RequestFailed += (_, req) =>
            TestContext.Progress.WriteLine($"[FAILED] {req.Url}: {req.Failure}");

        // API request context
        _apiContext = await _playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = TestConfiguration.ApiUrl,
            IgnoreHTTPSErrors = true
        });
    }

    public async Task<string?> TakeScreenshotAsync(string scenarioTitle)
    {
        if (_page == null) return null;

        var sanitized = SanitizeFileName(scenarioTitle);
        var screenshotsDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResults", "screenshots");
        Directory.CreateDirectory(screenshotsDir);

        var path = Path.Combine(screenshotsDir, $"{sanitized}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png");
        await _page.ScreenshotAsync(new()
        {
            Path = path,
            FullPage = true
        });

        return path;
    }

    public async Task<string?> StopTracingAsync(string scenarioTitle, bool hasError)
    {
        if (_context == null) return null;

        var sanitized = SanitizeFileName(scenarioTitle);
        var tracesDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResults", "traces");
        Directory.CreateDirectory(tracesDir);

        var tracePath = Path.Combine(tracesDir, $"{sanitized}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip");

        if (hasError || TestConfiguration.Tracing.Equals("on", StringComparison.OrdinalIgnoreCase))
        {
            await _context.Tracing.StopAsync(new() { Path = tracePath });
            return tracePath;
        }

        await _context.Tracing.StopAsync();
        return null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_apiContext != null)
        {
            await _apiContext.DisposeAsync();
        }

        if (_page != null)
        {
            await _page.CloseAsync();
        }

        if (_context != null)
        {
            await _context.DisposeAsync();
        }

        if (_browser != null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }

    private static string SanitizeFileName(string name)
    {
        var invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars())
        {
            ':', '<', '>', '"', '|', '?', '*', '/', '\\'
        };
        return new string(name.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray()).Replace(' ', '_');
    }
}

