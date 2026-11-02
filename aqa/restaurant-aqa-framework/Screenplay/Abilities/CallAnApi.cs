using System.Text.Json;
using Microsoft.Playwright;
using RestaurantAqaFramework.Configuration;

namespace RestaurantAqaFramework.Screenplay.Abilities;

public class CallAnApi
{
    private readonly IAPIRequestContext _requestContext;
    public string BaseUrl { get; }

    public int? LastStatusCode { get; private set; }
    public string? LastResponseBody { get; private set; }
    public IReadOnlyDictionary<string, string>? LastHeaders { get; private set; }

    public CallAnApi(IAPIRequestContext requestContext, string baseUrl)
    {
        _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
        BaseUrl = baseUrl.TrimEnd('/');
    }

    public static CallAnApi At(IAPIRequestContext requestContext, string baseUrl) =>
        new(requestContext, baseUrl);

    public async Task<IAPIResponse> GetAsync(string endpoint, Dictionary<string, string>? headers = null)
    {
        var url = BuildUrl(endpoint);
        var options = new APIRequestContextOptions();
        if (headers != null)
        {
            options.Headers = headers;
        }

        var response = await _requestContext.GetAsync(url, options);
        await RecordResponse(response);
        return response;
    }

    public async Task<IAPIResponse> PostAsync(string endpoint, object? data = null, Dictionary<string, string>? headers = null)
    {
        var url = BuildUrl(endpoint);
        var options = new APIRequestContextOptions();
        if (headers != null)
        {
            options.Headers = headers;
        }
        if (data != null)
        {
            options.DataObject = data;
        }

        var response = await _requestContext.PostAsync(url, options);
        await RecordResponse(response);
        return response;
    }

    public async Task<IAPIResponse> PutAsync(string endpoint, object? data = null, Dictionary<string, string>? headers = null)
    {
        var url = BuildUrl(endpoint);
        var options = new APIRequestContextOptions();
        if (headers != null)
        {
            options.Headers = headers;
        }
        if (data != null)
        {
            options.DataObject = data;
        }

        var response = await _requestContext.PutAsync(url, options);
        await RecordResponse(response);
        return response;
    }

    public async Task<IAPIResponse> DeleteAsync(string endpoint, Dictionary<string, string>? headers = null)
    {
        var url = BuildUrl(endpoint);
        var options = new APIRequestContextOptions();
        if (headers != null)
        {
            options.Headers = headers;
        }

        var response = await _requestContext.DeleteAsync(url, options);
        await RecordResponse(response);
        return response;
    }

    public T? DeserializeLastResponse<T>()
    {
        if (string.IsNullOrWhiteSpace(LastResponseBody)) return default;
        return JsonSerializer.Deserialize<T>(LastResponseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    private string BuildUrl(string endpoint)
    {
        if (endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return endpoint;
        }

        return $"{BaseUrl}/{(endpoint.StartsWith('/') ? endpoint[1..] : endpoint)}";
    }

    private async Task RecordResponse(IAPIResponse response)
    {
        LastStatusCode = response.Status;
        LastResponseBody = await response.TextAsync();
        LastHeaders = response.Headers;
    }
}

