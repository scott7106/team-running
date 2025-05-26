using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TeamStride.Domain.Identity;

namespace TeamStride.Application.Authentication.Services;

public interface IExternalAuthService
{
    Task<ExternalUserInfo> GetUserInfoAsync(string provider, string accessToken);
}

public class ExternalAuthService : IExternalAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public ExternalAuthService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<ExternalUserInfo> GetUserInfoAsync(string provider, string accessToken)
    {
        return provider.ToLowerInvariant() switch
        {
            "microsoft" => await GetMicrosoftUserInfoAsync(accessToken),
            "google" => await GetGoogleUserInfoAsync(accessToken),
            "facebook" => await GetFacebookUserInfoAsync(accessToken),
            "twitter" => await GetTwitterUserInfoAsync(accessToken),
            _ => throw new AuthenticationException($"Unsupported provider: {provider}")
        };
    }

    private async Task<ExternalUserInfo> GetMicrosoftUserInfoAsync(string accessToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync("https://graph.microsoft.com/v1.0/me");
        if (!response.IsSuccessStatusCode)
        {
            throw new AuthenticationException("Failed to get Microsoft user info", AuthenticationException.ErrorCodes.ExternalAuthError);
        }

        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new ExternalUserInfo
        {
            Email = data.GetProperty("mail").GetString() ?? data.GetProperty("userPrincipalName").GetString(),
            FirstName = data.GetProperty("givenName").GetString(),
            LastName = data.GetProperty("surname").GetString(),
            ProviderId = data.GetProperty("id").GetString()
        };
    }

    private async Task<ExternalUserInfo> GetGoogleUserInfoAsync(string accessToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo");
        if (!response.IsSuccessStatusCode)
        {
            throw new AuthenticationException("Failed to get Google user info", AuthenticationException.ErrorCodes.ExternalAuthError);
        }

        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new ExternalUserInfo
        {
            Email = data.GetProperty("email").GetString(),
            FirstName = data.GetProperty("given_name").GetString(),
            LastName = data.GetProperty("family_name").GetString(),
            ProviderId = data.GetProperty("sub").GetString()
        };
    }

    private async Task<ExternalUserInfo> GetFacebookUserInfoAsync(string accessToken)
    {
        var client = _httpClientFactory.CreateClient();
        var fields = "id,email,first_name,last_name";
        var response = await client.GetAsync($"https://graph.facebook.com/v12.0/me?fields={fields}&access_token={accessToken}");
        
        if (!response.IsSuccessStatusCode)
        {
            throw new AuthenticationException("Failed to get Facebook user info", AuthenticationException.ErrorCodes.ExternalAuthError);
        }

        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new ExternalUserInfo
        {
            Email = data.GetProperty("email").GetString(),
            FirstName = data.GetProperty("first_name").GetString(),
            LastName = data.GetProperty("last_name").GetString(),
            ProviderId = data.GetProperty("id").GetString()
        };
    }

    private async Task<ExternalUserInfo> GetTwitterUserInfoAsync(string accessToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync("https://api.twitter.com/2/users/me?user.fields=name");
        if (!response.IsSuccessStatusCode)
        {
            throw new AuthenticationException("Failed to get Twitter user info", AuthenticationException.ErrorCodes.ExternalAuthError);
        }

        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        var user = data.GetProperty("data");
        
        // Twitter doesn't provide email in the initial response, need to make another call
        var emailResponse = await client.GetAsync("https://api.twitter.com/2/users/me?user.fields=email");
        if (!emailResponse.IsSuccessStatusCode)
        {
            throw new AuthenticationException("Failed to get Twitter email", AuthenticationException.ErrorCodes.ExternalAuthError);
        }

        var emailData = await emailResponse.Content.ReadFromJsonAsync<JsonElement>();
        var emailUser = emailData.GetProperty("data");

        // Split the name into first and last name (best effort)
        var fullName = user.GetProperty("name").GetString() ?? "";
        var nameParts = fullName.Split(' ', 2);
        
        return new ExternalUserInfo
        {
            Email = emailUser.GetProperty("email").GetString(),
            FirstName = nameParts.Length > 0 ? nameParts[0] : fullName,
            LastName = nameParts.Length > 1 ? nameParts[1] : "",
            ProviderId = user.GetProperty("id").GetString()
        };
    }
} 