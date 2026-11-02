using DotNetEnv;

namespace RestaurantAqaFramework.Configuration;

public static class TestConfiguration
{
    static TestConfiguration()
    {
        // Load .env from current directory or any parent directory
        try
        {
            Env.TraversePath().Load();
        }
        catch
        {
            // Fallback to process environment variables if .env not found
        }
    }

    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("BASE_URL") ?? "http://localhost:4200";

    public static string ApiUrl =>
        Environment.GetEnvironmentVariable("API_URL") ?? "http://localhost:5000";

    public static string Browser =>
        Environment.GetEnvironmentVariable("BROWSER") ?? "chromium";

    public static bool Headless =>
        bool.TryParse(Environment.GetEnvironmentVariable("HEADLESS"), out var headless) ? headless : true;

    public static float SlowMo =>
        float.TryParse(Environment.GetEnvironmentVariable("SLOW_MO"), out var slowMo) ? slowMo : 0;

    public static float DefaultTimeout =>
        float.TryParse(Environment.GetEnvironmentVariable("DEFAULT_TIMEOUT"), out var timeout) ? timeout : 15000;

    public static string Tracing =>
        Environment.GetEnvironmentVariable("TRACING") ?? "on-failure";

    public static string Video =>
        Environment.GetEnvironmentVariable("VIDEO") ?? "retain-on-failure";

    public static string Screenshot =>
        Environment.GetEnvironmentVariable("SCREENSHOT") ?? "only-on-failure";

    public static string OwnerEmail =>
        Environment.GetEnvironmentVariable("OWNER_EMAIL") ?? "owner@demo.test";

    public static string OwnerPassword =>
        Environment.GetEnvironmentVariable("OWNER_PASSWORD") ?? "OwnerPassword123!";

    public static string OwnerToken =>
        Environment.GetEnvironmentVariable("OWNER_TOKEN") ?? "demo-owner-token";

    public static string GuestToken =>
        Environment.GetEnvironmentVariable("GUEST_TOKEN") ?? "demo-guest-token";

    public static string WaiterToken =>
        Environment.GetEnvironmentVariable("WAITER_TOKEN") ?? "demo-waiter-token";

    public static string OtherClientToken =>
        Environment.GetEnvironmentVariable("OTHER_CLIENT_TOKEN") ?? "demo-other-client-token";
}

