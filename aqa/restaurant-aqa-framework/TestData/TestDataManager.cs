using System.Collections.Concurrent;
using RestaurantAqaFramework.Configuration;

namespace RestaurantAqaFramework.TestData;

public class TestDataManager
{
    private static readonly ConcurrentBag<string> CreatedGroupIds = new();
    private static readonly HttpClient HttpClient = new();

    public static void RegisterGroup(string groupId)
    {
        if (!string.IsNullOrWhiteSpace(groupId))
        {
            CreatedGroupIds.Add(groupId);
        }
    }

    public static async Task CleanupAllAsync()
    {
        var apiUrl = TestConfiguration.ApiUrl.TrimEnd('/');
        while (CreatedGroupIds.TryTake(out var groupId))
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Delete, $"{apiUrl}/test/groups/{groupId}");
                request.Headers.Add("Authorization", "Bearer owner");
                request.Headers.Add("X-User-Role", "Owner");
                await HttpClient.SendAsync(request);
            }
            catch
            {
                // Ignore cleanup errors during teardown
            }
        }
    }

    public static async Task CleanupGroupAsync(string groupId)
    {
        if (string.IsNullOrWhiteSpace(groupId)) return;

        var apiUrl = TestConfiguration.ApiUrl.TrimEnd('/');
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"{apiUrl}/test/groups/{groupId}");
            request.Headers.Add("Authorization", "Bearer owner");
            request.Headers.Add("X-User-Role", "Owner");
            await HttpClient.SendAsync(request);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}

