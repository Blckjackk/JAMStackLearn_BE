using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using api_app.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace api_app.Services;

public class OtpService : IOtpService
{
    private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(5);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;

    public OtpService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _configuration = configuration;
    }

    public async Task SendOtpAsync(
        string phoneNumber,
        string otpCode,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["Fontee:Token"];
        var baseUrl = _configuration["Fontee:BaseUrl"];

        if (string.IsNullOrWhiteSpace(apiKey)
            || string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("Fontee configuration is missing.");
        }

        var code = otpCode;
        var cacheKey = GetCacheKey(phoneNumber);
        _cache.Set(cacheKey, code, OtpTtl);

        var normalizedPhoneNumber = phoneNumber.StartsWith("+", StringComparison.Ordinal)
            ? phoneNumber[1..]
            : phoneNumber;
        if (normalizedPhoneNumber.StartsWith("0", StringComparison.Ordinal))
        {
            normalizedPhoneNumber = $"62{normalizedPhoneNumber[1..]}";
        }

        var baseAddress = baseUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? baseUrl
            : $"https://{baseUrl}";
        var requestUri = new Uri(new Uri(baseAddress), "/send");

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Add("Authorization", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var payload = new
        {
            target = normalizedPhoneNumber,
            message = $"Kode OTP kamu adalah: {code}",
            countryCode = "62"
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        var client = _httpClientFactory.CreateClient("Fontee");
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Fontee Error: {response.StatusCode} at {requestUri} - {errorBody}");
        }
    }

    public Task<bool> VerifyOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(phoneNumber);
        if (!_cache.TryGetValue(cacheKey, out string? storedCode))
        {
            return Task.FromResult(false);
        }

        if (!string.Equals(storedCode, code, StringComparison.Ordinal))
        {
            return Task.FromResult(false);
        }

        _cache.Remove(cacheKey);
        return Task.FromResult(true);
    }

    private static string GetCacheKey(string phoneNumber) => $"otp:{phoneNumber}";
}
