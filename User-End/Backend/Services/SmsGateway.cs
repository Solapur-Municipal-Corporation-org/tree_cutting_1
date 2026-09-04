using System.Net;
using Microsoft.Extensions.Options;

namespace TreeCutting.Api.Services;

public interface ISmsGateway
{
    Task<bool> SendSmsAsync(string smsMessage, string mobileNumber, CancellationToken cancellationToken = default);
}

public sealed class SmsGateway : ISmsGateway
{
    private readonly HttpClient _httpClient;
    private readonly SmsGatewayOptions _options;

    public SmsGateway(HttpClient httpClient, IOptions<SmsGatewayOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<bool> SendSmsAsync(string smsMessage, string mobileNumber, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_options.Password))
        {
            throw new InvalidOperationException("SMS gateway is enabled but its password is not configured.");
        }

        var query = string.Join("&", new Dictionary<string, string>
        {
            ["appid"] = _options.AppId,
            ["userId"] = _options.UserId,
            ["pass"] = _options.Password,
            ["contenttype"] = "1",
            ["from"] = _options.Sender,
            ["to"] = $"{_options.CountryCode}{mobileNumber}",
            ["text"] = smsMessage,
            ["alert"] = "1",
            ["selfid"] = "true",
            ["dlrreq"] = "true",
            ["dtm"] = _options.DltTemplateId
        }.Select(parameter => $"{WebUtility.UrlEncode(parameter.Key)}={WebUtility.UrlEncode(parameter.Value)}"));

        using var response = await _httpClient.GetAsync($"{_options.Endpoint}?{query}", cancellationToken);
        var gatewayResponse = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"SMS gateway returned {(int)response.StatusCode}: {gatewayResponse}");
        }

        if (string.IsNullOrWhiteSpace(gatewayResponse))
        {
            throw new InvalidOperationException("SMS gateway returned an empty response.");
        }

        if (gatewayResponse.Contains("error", StringComparison.OrdinalIgnoreCase)
            || gatewayResponse.Contains("failed", StringComparison.OrdinalIgnoreCase)
            || gatewayResponse.Contains("invalid", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"SMS gateway rejected the message: {gatewayResponse}");
        }

        return true;
    }
}