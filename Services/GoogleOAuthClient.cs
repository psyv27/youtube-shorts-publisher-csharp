using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using YouTubeShortsPublisher.Models;

namespace YouTubeShortsPublisher.Services;

/// <summary>
/// Google OAuth client for YouTube Data API
/// </summary>
public class GoogleOAuthClient
{
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _redirectUri;

    private const string TokenUrl = "https://oauth2.googleapis.com/token";
    private const string AuthUrl = "https://accounts.google.com/o/oauth2/v2/auth";

    public GoogleOAuthClient(HttpClient httpClient, string clientId, string clientSecret, string redirectUri)
    {
        _httpClient = httpClient;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _redirectUri = redirectUri;
    }

    /// <summary>
    /// Generate authorization URL
    /// </summary>
    public string GenerateAuthUrl(string scope = "https://www.googleapis.com/auth/youtube", string state = "youtube_shorts_bot")
    {
        var scopeEncoded = Uri.EscapeDataString(scope);
        var redirectUriEncoded = Uri.EscapeDataString(_redirectUri);

        var url = $"{AuthUrl}?" +
                  $"client_id={_clientId}&" +
                  $"redirect_uri={redirectUriEncoded}&" +
                  $"response_type=code&" +
                  $"scope={scopeEncoded}&" +
                  $"access_type=offline&" +
                  $"prompt=consent&" +
                  $"state={state}";

        return url;
    }

    /// <summary>
    /// Exchange authorization code for tokens
    /// </summary>
    public async Task<GoogleTokenResponse?> ExchangeCodeForTokenAsync(string authorizationCode)
    {
        var requestBody = new Dictionary<string, string>
        {
            { "client_id", _clientId },
            { "client_secret", _clientSecret },
            { "code", authorizationCode },
            { "grant_type", "authorization_code" },
            { "redirect_uri", _redirectUri },
            { "access_type", "offline" }
        };

        var content = new FormUrlEncodedContent(requestBody);

        try
        {
            var response = await _httpClient.PostAsync(TokenUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Token Response Status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Token exchange failed: {responseContent}");
                return null;
            }

            var tokenResponse = JsonSerializer.Deserialize<GoogleTokenResponse>(responseContent);

            if (tokenResponse != null && tokenResponse.HasError)
            {
                Console.WriteLine($"Token error: {tokenResponse.error_description}");
                return null;
            }

            Console.WriteLine("✅ Token exchange successful!");
            Console.WriteLine($"   Access Token: {tokenResponse?.access_token?[0..30]}...");
            Console.WriteLine($"   Refresh Token: {tokenResponse?.refresh_token?[0..30]}...");
            Console.WriteLine($"   Expires in: {tokenResponse?.expires_in} seconds");

            return tokenResponse;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Token exchange failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    public async Task<GoogleTokenResponse?> RefreshAccessTokenAsync(string refreshToken)
    {
        var requestBody = new Dictionary<string, string>
        {
            { "client_id", _clientId },
            { "client_secret", _clientSecret },
            { "refresh_token", refreshToken },
            { "grant_type", "refresh_token" }
        };

        var content = new FormUrlEncodedContent(requestBody);

        try
        {
            var response = await _httpClient.PostAsync(TokenUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            var tokenResponse = JsonSerializer.Deserialize<GoogleTokenResponse>(responseContent);

            if (tokenResponse != null && tokenResponse.HasError)
            {
                Console.WriteLine($"Refresh error: {tokenResponse.error_description}");
                return null;
            }

            Console.WriteLine("✅ Token refresh successful!");
            return tokenResponse;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Token refresh failed: {ex.Message}");
            return null;
        }
    }
}
