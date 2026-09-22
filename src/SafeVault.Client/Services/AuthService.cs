using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using SafeVault.Shared.DTOs.Auth;

namespace SafeVault.Client.Services;

public class AuthService
{
    private readonly HttpClient _http;

    public AuthService(HttpClient http)
    {
        _http = http;
    }

    public async Task<bool> RegisterAsync(RegisterRequest request)
    {
        var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "api/Auth/register")
        {
            Content = JsonContent.Create(request)
        };

        httpRequest.SetBrowserRequestCredentials(
            BrowserRequestCredentials.Include);

        using var response = await _http.SendAsync(httpRequest);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> LoginAsync(LoginRequest request)
    {
        var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "api/Auth/login")
        {
            Content = JsonContent.Create(request)
        };

        httpRequest.SetBrowserRequestCredentials(
            BrowserRequestCredentials.Include);

        using var response = await _http.SendAsync(httpRequest);

        return response.IsSuccessStatusCode;
    }

    public async Task LogoutAsync()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "api/Auth/logout");

        request.SetBrowserRequestCredentials(
            BrowserRequestCredentials.Include);

        await _http.SendAsync(request);
    }

    public async Task<UserProfileResponse?> GetCurrentUserAsync()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "api/Auth/me");

        request.SetBrowserRequestCredentials(
            BrowserRequestCredentials.Include);

        using var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content
            .ReadFromJsonAsync<UserProfileResponse>();
    }
}