using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TeamStride.Domain.Identity;
using System.Net.Http.Headers;
using System.Linq;
using TeamStride.Application.Authentication.Services;

namespace TeamStride.Infrastructure.Authentication.Services;

public class ExternalAuthService : IExternalAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public ExternalAuthService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<ExternalUserInfo?> GetUserInfoAsync(string provider, string accessToken)
    {
        var userInfo = provider.ToLowerInvariant() switch
        {
            "microsoft" => await GetMicrosoftUserInfoAsync(accessToken),
            "google" => await GetGoogleUserInfoAsync(accessToken),
            "facebook" => await GetFacebookUserInfoAsync(accessToken),
            "twitter" => await GetTwitterUserInfoAsync(accessToken),
            _ => throw new AuthenticationException($"Unsupported provider: {provider}", AuthenticationException.ErrorCodes.ExternalAuthError)
        };

        return userInfo;
    }

    private async Task<ExternalUserInfo?> GetMicrosoftUserInfoAsync(string accessToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync("https://graph.microsoft.com/v1.0/me");
        if (!response.IsSuccessStatusCode) return null;

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (content.ValueKind != JsonValueKind.Object) return null;

        return new ExternalUserInfo
        {
            Email = content.GetProperty("mail").GetString() ?? 
                   content.GetProperty("userPrincipalName").GetString() ?? 
                   throw new InvalidOperationException("No email found in Microsoft user info"),
            FirstName = content.GetProperty("givenName").GetString() ?? string.Empty,
            LastName = content.GetProperty("surname").GetString() ?? string.Empty,
            ProviderId = content.GetProperty("id").GetString() ?? 
                throw new InvalidOperationException("No id found in Microsoft user info")
        };
    }

    private async Task<ExternalUserInfo?> GetGoogleUserInfoAsync(string accessToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
        if (!response.IsSuccessStatusCode) return null;

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (content.ValueKind != JsonValueKind.Object) return null;

        return new ExternalUserInfo
        {
            Email = content.GetProperty("email").GetString() ?? 
                throw new InvalidOperationException("No email found in Google user info"),
            FirstName = content.GetProperty("given_name").GetString() ?? string.Empty,
            LastName = content.GetProperty("family_name").GetString() ?? string.Empty,
            ProviderId = content.GetProperty("id").GetString() ?? 
                throw new InvalidOperationException("No id found in Google user info")
        };
    }

    private async Task<ExternalUserInfo?> GetFacebookUserInfoAsync(string accessToken)
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(
            $"https://graph.facebook.com/v12.0/me?fields=id,email,first_name,last_name&access_token={accessToken}");
        
        if (!response.IsSuccessStatusCode) return null;

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (content.ValueKind != JsonValueKind.Object) return null;

        return new ExternalUserInfo
        {
            Email = content.GetProperty("email").GetString() ?? 
                throw new InvalidOperationException("No email found in Facebook user info"),
            FirstName = content.GetProperty("first_name").GetString() ?? string.Empty,
            LastName = content.GetProperty("last_name").GetString() ?? string.Empty,
            ProviderId = content.GetProperty("id").GetString() ?? 
                throw new InvalidOperationException("No id found in Facebook user info")
        };
    }

    private async Task<ExternalUserInfo?> GetTwitterUserInfoAsync(string accessToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync("https://api.twitter.com/2/users/me?user.fields=name,email");
        if (!response.IsSuccessStatusCode) return null;

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (content.ValueKind != JsonValueKind.Object || !content.TryGetProperty("data", out var data)) 
            return null;

        var nameParts = (data.GetProperty("name").GetString() ?? string.Empty).Split(' ');
        return new ExternalUserInfo
        {
            Email = data.GetProperty("email").GetString() ?? 
                throw new InvalidOperationException("No email found in Twitter user info"),
            FirstName = nameParts.Length > 0 ? nameParts[0] : string.Empty,
            LastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : string.Empty,
            ProviderId = data.GetProperty("id").GetString() ?? 
                throw new InvalidOperationException("No id found in Twitter user info")
        };
    }
} 