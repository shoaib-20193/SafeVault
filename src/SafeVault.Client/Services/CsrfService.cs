using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace SafeVault.Client.Services;

public class CsrfService
{
    private readonly HttpClient _http;
    private string? _token;

    public CsrfService(HttpClient http)
    {
        _http = http;
    }

    public async Task<string?> GetTokenAsync()
    {
        if (!string.IsNullOrWhiteSpace(_token))
        {
            return _token;
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "api/Antiforgery/token");

        request.SetBrowserRequestCredentials(
            BrowserRequestCredentials.Include);

        using var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result =
            await response.Content.ReadFromJsonAsync<CsrfTokenResponse>();

        _token = result?.Token;

        return _token;
    }

    private sealed class CsrfTokenResponse
    {
        public string? Token { get; set; }
    }
}