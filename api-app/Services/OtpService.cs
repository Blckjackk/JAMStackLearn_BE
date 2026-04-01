using System.Net.Http.Json;
using System.Security.Cryptography;
using api_app.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace api_app.Services;

public class OtpService : IOtpService
{
    private const int OtpLength = 6;
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

    public async Task SendOtpAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["Infobip:ApiKey"];
        var sender = _configuration["Infobip:Sender"];

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(sender))
        {
            throw new InvalidOperationException("Infobip configuration is missing.");
        }

        var code = GenerateOtpCode();
        var cacheKey = GetCacheKey(phoneNumber);
        _cache.Set(cacheKey, code, OtpTtl);

        var client = _httpClientFactory.CreateClient("Infobip");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("App", apiKey);

        var payload = new
        {
            from = sender,
            to = phoneNumber,
            message = new
            {
                text = $"Kode OTP kamu {code}. Berlaku 5 menit."
            }
        };

        using var response = await client.PostAsJsonAsync("/whatsapp/1/message/text", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to send OTP: {response.StatusCode} {errorBody}");
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

    private static string GenerateOtpCode()
    {
        var number = RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, OtpLength));
        return number.ToString($"D{OtpLength}");
    }

    private static string GetCacheKey(string phoneNumber) => $"otp:{phoneNumber}";
}
