using System.Text;
using System.Text.Json;
using YouTubeShortsPublisher.Models;

namespace YouTubeShortsPublisher.Services;

/// <summary>
/// Token manager for YouTube API
/// </summary>
public class TokenManager
{
    private readonly string _credentialsPath;
    private readonly GoogleOAuthClient _oauthClient;
    private Credentials? _credentials;
    private DateTime _tokenIssuedAt = DateTime.UtcNow;

    private const string Scope = "https://www.googleapis.com/auth/youtube";

    public TokenManager(string credentialsPath = "Config/api_credentials.json")
    {
        _credentialsPath = credentialsPath;
        _oauthClient = new GoogleOAuthClient(
            new HttpClient(),
            "", // Will be set from credentials
            "",
            "https://oauth.pstmn.io/v1/callback");
    }

    /// <summary>
    /// Load credentials from file
    /// </summary>
    public bool LoadCredentials()
    {
        if (!File.Exists(_credentialsPath))
        {
            Console.WriteLine($"❌ Credentials file not found: {_credentialsPath}");
            Console.WriteLine("   Please create api_credentials.json from the example template");
            return false;
        }

        try
        {
            var jsonContent = File.ReadAllText(_credentialsPath);
            _credentials = JsonSerializer.Deserialize<Credentials>(jsonContent);

            if (_credentials == null || !_credentials.IsValid())
            {
                Console.WriteLine("❌ Invalid credentials or not configured");
                return false;
            }

            Console.WriteLine("✅ Credentials loaded successfully");
            Console.WriteLine($"   Client ID: {_credentials.ClientId}");

            // Update oauth client with credentials
            _oauthClient = new GoogleOAuthClient(
                new HttpClient(),
                _credentials.ClientId,
                _credentials.ClientSecret,
                "https://oauth.pstmn.io/v1/callback");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to load credentials: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Save credentials to file
    /// </summary>
    public bool SaveCredentials(Credentials credentials)
    {
        try
        {
            var directory = Path.GetDirectoryName(_credentialsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var jsonContent = JsonSerializer.Serialize(credentials, options);
            File.WriteAllText(_credentialsPath, jsonContent);

            _credentials = credentials;
            _tokenIssuedAt = DateTime.UtcNow;

            // Update oauth client
            _oauthClient = new GoogleOAuthClient(
                new HttpClient(),
                credentials.ClientId,
                credentials.ClientSecret,
                "https://oauth.pstmn.io/v1/callback");

            Console.WriteLine($"✅ Credentials saved to: {_credentialsPath}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to save credentials: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Update credentials with new token response
    /// </summary>
    public bool UpdateTokens(GoogleTokenResponse? tokenResponse)
    {
        if (tokenResponse == null || tokenResponse.HasError)
        {
            return false;
        }

        var updatedCredentials = new Credentials
        {
            ClientId = _credentials?.ClientId ?? string.Empty,
            ClientSecret = _credentials?.ClientSecret ?? string.Empty,
            AccessToken = tokenResponse.access_token ?? string.Empty,
            RefreshToken = tokenResponse.refresh_token ?? string.Empty,
            TokenType = tokenResponse.token_type ?? "Bearer",
            ExpiresIn = tokenResponse.expires_in,
            Scope = tokenResponse.scope ?? Scope,
            Status = "configured"
        };

        return SaveCredentials(updatedCredentials);
    }

    /// <summary>
    /// Get credentials
    /// </summary>
    public Credentials? GetCredentials()
    {
        return _credentials;
    }

    /// <summary>
    /// Check if access token needs refresh
    /// </summary>
    public bool ShouldRefreshAccessToken()
    {
        if (_credentials == null)
        {
            return false;
        }

        return _credentials.IsAccessTokenExpired(_tokenIssuedAt);
    }

    /// <summary>
    /// Refresh access token automatically
    /// </summary>
    public async Task<bool> RefreshAccessTokenAsync()
    {
        Console.WriteLine("🔄 Refreshing access token...");

        if (_credentials == null)
        {
            Console.WriteLine("❌ No credentials available for refresh");
            return false;
        }

        var tokenResponse = await _oauthClient.RefreshAccessTokenAsync(_credentials.RefreshToken);

        if (tokenResponse == null || tokenResponse.HasError)
        {
            Console.WriteLine("❌ Failed to refresh access token");
            Console.WriteLine("   Please re-run authentication flow or check refresh token");
            return false;
        }

        var success = UpdateTokens(tokenResponse);

        if (success)
        {
            Console.WriteLine("✅ Access token refreshed successfully");
            Console.WriteLine($"   New Access Token: {tokenResponse.access_token?[0..30]}...");
        }

        return success;
    }

    /// <summary>
    /// Generate authorization URL
    /// </summary>
    public string GenerateAuthUrl(string state = "youtube_shorts_bot")
    {
        if (_credentials == null)
        {
            throw new InvalidOperationException("Credentials not loaded");
        }

        return _oauthClient.GenerateAuthUrl(Scope, state);
    }

    /// <summary>
    /// Exchange authorization code for tokens
    /// </summary>
    public async Task<GoogleTokenResponse?> ExchangeCodeForTokenAsync(string authorizationCode)
    {
        var tokenResponse = await _oauthClient.ExchangeCodeForTokenAsync(authorizationCode);

        if (tokenResponse != null && !tokenResponse.HasError)
        {
            UpdateTokens(tokenResponse);
        }

        return tokenResponse;
    }

    /// <summary>
    /// Extract authorization code from callback URL
    /// </summary>
    public static string? ExtractAuthorizationCode(string callbackUrl)
    {
        try
        {
            // Extract code from Google OAuth callback URL
            // Format: https://oauth.pstmn.io/v1/callback?code=4/0A...&scope=...

            if (!callbackUrl.Contains("code="))
            {
                return null;
            }

            var startIndex = callbackUrl.IndexOf("code=") + 5;
            var endIndex = callbackUrl.IndexOf("&", startIndex);

            if (endIndex == -1 || endIndex < startIndex)
            {
                endIndex = callbackUrl.Length;
            }

            return callbackUrl.Substring(startIndex, endIndex - startIndex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to extract authorization code: {ex.Message}");
            return null;
        }
    }
}
