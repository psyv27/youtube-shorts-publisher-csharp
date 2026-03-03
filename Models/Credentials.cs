using System.Text.Json;
using System.Text.Json.Serialization;

namespace YouTubeShortsPublisher.Models;

/// <summary>
/// YouTube API Credentials
/// </summary>
public class Credentials
{
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; set; } = string.Empty;

    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string Scope { get; set; = "https://www.googleapis.com/auth/youtube";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "not_configured";

    /// <summary>
    /// Check if credentials are valid
    /// </summary>
    public bool IsValid()
    {
        if (Status == "not_configured")
        {
            return false;
        }

        bool hasClientId = !string.IsNullOrWhiteSpace(ClientId);
        bool hasClientSecret = !string.IsNullOrWhiteSpace(ClientSecret);
        bool hasAccessToken = !string.IsNullOrWhiteSpace(AccessToken);
        bool hasRefreshToken = !string.IsNullOrWhiteSpace(RefreshToken);

        return hasClientId && hasClientSecret && hasAccessToken && hasRefreshToken;
    }

    /// <summary>
    /// Check if access token is expired
    /// </summary>
    public bool IsAccessTokenExpired(DateTime tokenIssuedAt)
    {
        if (string.IsNullOrEmpty(AccessToken))
        {
            return true;
        }

        var expiresAfter = TimeSpan.FromSeconds(ExpiresIn);

        // Refresh 5 minutes before actual expiration
        var expiresAt = tokenIssuedAt.Add(expiresAfter).Subtract(TimeSpan.FromMinutes(5));

        return DateTime.UtcNow >= expiresAt;
    }

    /// <summary>
    /// Load credentials from JSON file
    /// </summary>
    public static Credentials Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Credentials file not found: {filePath}");
        }

        var jsonContent = File.ReadAllText(filePath);
        var credentials = JsonSerializer.Deserialize<Credentials>(jsonContent)
            ?? throw new InvalidOperationException("Failed to deserialize credentials");

        return credentials;
    }

    /// <summary>
    /// Save credentials to JSON file
    /// </summary>
    public void Save(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var jsonContent = JsonSerializer.Serialize(this, options);
        File.WriteAllText(filePath, jsonContent);
    }
}
