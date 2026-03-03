using System.Text.Json;
using System.Text.Json.Serialization;

namespace YouTubeShortsPublisher.Models;

/// <summary>
/// Token response from Google OAuth
/// </summary>
public class GoogleTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? access_token { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? refresh_token { get; set; }

    [JsonPropertyName("expires_in")]
    public int expires_in { get; set; }

    [JsonPropertyName("token_type")]
    public string? token_type { get; set; }

    [JsonPropertyName("scope")]
    public string? scope { get; set; }

    [JsonPropertyName("error")]
    public string? error { get; set; }

    [JsonPropertyName("error_description")]
    public string? error_description { get; set; }

    [JsonIgnore]
    public bool HasError => !string.IsNullOrEmpty(error);

    public static GoogleTokenResponse FromJson(string json)
    {
        return JsonSerializer.Deserialize<GoogleTokenResponse>(json)
               ?? throw new InvalidOperationException("Failed to deserialize token response");
    }

    public string ToJson()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(this, options);
    }
}

/// <summary>
/// YouTube video resource
/// </summary>
public class YouTubeVideo
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("snippet")]
    public SnippetSnippet? Snippet { get; set; }

    [JsonPropertyName("status")]
    public StatusStatus? Status { get; set; }
}

/// <summary>
/// Snippet in video resource
/// </summary>
public class SnippetSnippet
{
    [JsonPropertyName("title")]
    public string? title { get; set; }

    [JsonPropertyName("description")]
    public string? description { get; set; }
}

/// <summary>
/// Status in video resource
/// </summary>
public class StatusStatus
{
    [JsonPropertyName("privacyStatus")]
    public string? privacyStatus { get; set; }

    [JsonPropertyName("uploadStatus")]
    public string? uploadStatus { get; set; }
}

/// <summary>
/// Error response from YouTube API
/// </summary>
public class YouTubeError
{
    [JsonPropertyName("error")]
    public ErrorDetail? error { get; set; }

    [JsonIgnore]
    public bool HasError => error != null;
}

public class ErrorDetail
{
    [JsonPropertyName("code")]
    public int code { get; set; }

    [JsonPropertyName("message")]
    public string? message { get; set; }

    [JsonPropertyName("errors")]
    public ErrorItem[]? errors { get; set; }
}

public class ErrorItem
{
    [JsonPropertyName("message")]
    public string? message { get; set; }

    [JsonPropertyName("domain")]
    public string? domain { get; set; }

    [JsonPropertyName("reason")]
    public string? reason { get; set; }
}
