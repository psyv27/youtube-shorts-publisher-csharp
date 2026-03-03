using System.Text.Json.Serialization;

namespace YouTubeShortsPublisher.Models;

/// <summary>
/// YouTube video upload request
/// </summary>
public class VideoUploadRequest
{
    [JsonPropertyName("snippet")]
    public required Snippet Snippet { get; set; }

    [JsonPropertyName("status")]
    public required Status Status { get; set; }

    public static VideoUploadRequest CreateShort(string title, string description = "", string privacy = "public")
    {
        return new VideoUploadRequest
        {
            Snippet = new Snippet
            {
                Title = title,
                Description = description,
                CategoryId = "22", // People & Blogs
                Tags = new[] { "Shorts", "YouTubeShorts", "viral", "trending" }
            },
            Status = new Status
            {
                PrivacyStatus = privacy,
                SelfDeclaredMadeForKids = false
            }
        };
    }
}

/// <summary>
/// Video snippet metadata
/// </summary>
public class Snippet
{
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("categoryId")]
    public string CategoryId { get; set; } = "22";

    [JsonPropertyName("tags")]
    public string[] Tags { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Video status settings
/// </summary>
public class Status
{
    [JsonPropertyName("privacyStatus")]
    public required string PrivacyStatus { get; set; }

    [JsonPropertyName("selfDeclaredMadeForKids")]
    public bool SelfDeclaredMadeForKids { get; set; } = false;

    [JsonPropertyName("embeddable")]
    public bool Embeddable { get; set; } = true;
}
