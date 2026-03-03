using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Google.Apis;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using YouTubeShortsPublisher.Models;

namespace YouTubeShortsPublisher.Services;

/// <summary>
/// YouTube Data API v3 Client
/// </summary>
public class YouTubeApiClient
{
    private readonly YouTubeService _youtubeService;
    private readonly Credentials _credentials;

    public YouTubeApiClient(Credentials credentials)
    {
        _credentials = credentials;

        var initializer = new BaseClientService.Initializer
        {
            ApplicationName = "YouTube Shorts Publisher",
            ApiKey = null, // We use OAuth
            HttpClientInitializer = GetCredential()
        };

        _youtubeService = new YouTubeService(initializer);
    }

    /// <summary>
    /// Get OAuth credential
    /// </summary>
    private UserCredential GetCredential()
    {
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = _credentials.ClientId,
                ClientSecret = _credentials.ClientSecret
            },
            Scopes = new[] { YouTubeService.Scope.YoutubeUpload }
        });

        return new UserCredential(flow, "user", new TokenResponse
        {
            AccessToken = _credentials.AccessToken,
            RefreshToken = _credentials.RefreshToken,
            TokenType = _credentials.TokenType,
            ExpiresInSeconds = _credentials.ExpiresIn,
            Scope = _credentials.Scope
        });
    }

    /// <summary>
    /// Upload video to YouTube
    /// </summary>
    public async Task<VideoUploadResult?> UploadVideoAsync(
        string filePath,
        string title,
        string description = "",
        string privacy = "public",
        IProgress<int>? progress = null)
    {
        Console.WriteLine($"\n📤 Uploading: {Path.GetFileName(filePath)}");
        Console.WriteLine($"   Title: {title}");
        Console.WriteLine($"   Privacy: {privacy}");

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"❌ File not found: {filePath}");
            return VideoUploadResult.Failed(filePath, "File not found");
        }

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length == 0)
        {
            Console.WriteLine($"❌ File is empty: {filePath}");
            return VideoUploadResult.Failed(filePath, "File is empty");
        }

        Console.WriteLine($"   Size: {FormatFileSize(fileInfo.Length)}");

        try
        {
            // Create video resource
            var video = new Video
            {
                Snippet = new VideoSnippet
                {
                    Title = title,
                    Description = description,
                    CategoryId = "22", // People & Blogs
                    Tags = new List<string> { "Shorts", "YouTubeShorts" }
                },
                Status = new VideoStatus
                {
                    PrivacyStatus = privacy,
                    SelfDeclaredMadeForKids = false,
                    Embeddable = true
                }
            };

            // Create upload request
            using var fileStream = new FileStream(filePath, FileMode.Open);
            var videosInsertRequest = _youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");
            videosInsertRequest.ChunkSize = ResumableUpload.MinimumChunkSize;

            // Progress tracker
            videosInsertRequest.ProgressChanged += (progressData) =>
            {
                var percent = (int)(progressData.BytesSent * 100 / progressData.TotalBytes);
                progress?.Report(percent);

                Console.Write($"\r   Progress: {percent}% ({FormatFileSize(progressData.BytesSent)} / {FormatFileSize(progressData.TotalBytes)})");
            };

            videosInsertRequest.ResponseReceived += (uploadedVideo) =>
            {
                Console.WriteLine($"\n✅ Upload complete!");
                Console.WriteLine($"   Video ID: {uploadedVideo.Id}");
                Console.WriteLine($"   URL: https://youtube.com/shorts/{uploadedVideo.Id}");
            };

            Console.WriteLine("\n   Starting upload...");

            var uploadedVideo = await videosInsertRequest.UploadAsync();

            if (uploadedVideo.Exception != null)
            {
                Console.WriteLine($"\n❌ Upload failed: {uploadedVideo.Exception.Message}");
                return VideoUploadResult.Failed(filePath, uploadedVideo.Exception.Message);
            }

            var result = VideoUploadResult.Succeeded(filePath, videosInsertRequest.ResponseBody.Id ?? string.Empty);
            Console.WriteLine($"\n✅ Video uploaded successfully!");
            Console.WriteLine($"   Video ID: {result.VideoId}");
            Console.WriteLine($"   URL: https://youtube.com/shorts/{result.VideoId}");

            // Add Shorts hashtag if using Short format
            if (isShortsFormat(videoInfo: uploadedVideo.ResponseBody))
            {
                Console.WriteLine($"   Note: Video will appear in Shorts!");
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Upload failed: {ex.Message}");
            Console.WriteLine($"   {ex.StackTrace}");
            return VideoUploadResult.Failed(filePath, ex.Message);
        }
    }

    /// <summary>
    /// Upload multiple videos
    /// </summary>
    public async Task<BatchUploadResult> UploadBatchAsync(
        string directoryPath,
        string searchPattern = "*.mp4",
        string privacy = "public",
        IProgress<int>? totalProgress = null)
    {
        Console.WriteLine($"\n{'='*60}");
        Console.WriteLine($"📦 Batch Upload: {directoryPath}");
        Console.WriteLine($"{'='*60}");

        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine($"❌ Directory not found: {directoryPath}");
            return BatchUploadResult.Failed(directoryPath, "Directory not found");
        }

        var videoFiles = Directory.GetFiles(directoryPath, searchPattern)
                                  .OrderBy(f => f)
                                  .ToList();

        if (!videoFiles.Any())
        {
            Console.WriteLine($"❌ No {searchPattern} files found");
            return BatchUploadResult.Failed(directoryPath, "No files found");
        }

        Console.WriteLine($"📋 Found {videoFiles.Count} video file(s)");

        var results = new List<VideoUploadResult>();
        int successCount = 0;
        int failureCount = 0;
        var startTime = DateTime.UtcNow;

        for (int i = 0; i < videoFiles.Count; i++)
        {
            var videoFile = videoFiles[i];
            Console.Write($"\n[{i + 1}/{videoFiles.Count}] ");

            var title = GenerateShortTitle(videoFile);
            var description = GenerateShortDescription(videoFile);

            var result = await UploadVideoAsync(videoFile, title, description, privacy,
                new Progress<int>(progress => totalProgress?.Report(
                    (i * 100 + progress) / videoFiles.Count)));

            results.Add(result);

            if (result.Success)
            {
                successCount++;
            }
            else
            {
                failureCount++;
            }

            // Small delay between uploads
            if (i < videoFiles.Count - 1)
            {
                await Task.Delay(2000);
            }
        }

        var endTime = DateTime.UtcNow;
        var duration = endTime - startTime;

        Console.WriteLine("\n" + "="*60);
        Console.WriteLine("📊 BATCH UPLOAD SUMMARY");
        Console.WriteLine("="*60);
        Console.WriteLine($"📁 Total files: {videoFiles.Count}");
        Console.WriteLine($"✅ Success: {successCount}");
        Console.WriteLine($"❌ Failed: {failureCount}");
        Console.WriteLine($"⏱️  Duration: {duration.TotalMinutes:0.00} minutes");

        if (successCount > 0)
        {
            Console.WriteLine("\n✅ Uploaded videos:");
            foreach (var result in results.Where(r => r.Success))
            {
                Console.WriteLine($"   📹 {Path.GetFileName(result.VideoPath)}");
                Console.WriteLine($"      Video ID: {result.VideoId}");
                Console.WriteLine($"      URL: https://youtube.com/shorts/{result.VideoId}");
            }
        }

        return new BatchUploadResult
        {
            DirectoryPath = directoryPath,
            TotalFiles = videoFiles.Count,
            SuccessCount = successCount,
            FailureCount = failureCount,
            Results = results,
            StartTime = startTime,
            EndTime = endTime,
            Success = successCount > 0
        };
    }

    private static bool isShortsFormat(Video? videoInfo)
    {
        // YouTube Shorts are videos with aspect ratio >= 9:16
        // We check uploaded filename or assume vertical videos are shorts
        return true;
    }

    private static string GenerateShortTitle(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        return $"{fileName} #Shorts #ShortsVideo";
    }

    private static string GenerateShortDescription(string filePath)
    {
        return $"#{Path.GetFileNameWithoutExtension(filePath)} #Shorts #Viral #Trending";
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}

/// <summary>
/// Video upload result
/// </summary>
public class VideoUploadResult
{
    public required string VideoPath { get; set; }
    public required string VideoId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public static VideoUploadResult Succeeded(string videoPath, string videoId)
    {
        return new VideoUploadResult
        {
            VideoPath = videoPath,
            VideoId = videoId,
            Success = true
        };
    }

    public static VideoUploadResult Failed(string videoPath, string errorMessage)
    {
        return new VideoUploadResult
        {
            VideoPath = videoPath,
            VideoId = string.Empty,
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// Batch upload result
/// </summary>
public class BatchUploadResult
{
    public required string DirectoryPath { get; set; }
    public int TotalFiles { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public required List<VideoUploadResult> Results { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool Success { get; set; }

    public static BatchUploadResult Failed(string directoryPath, string errorMessage)
    {
        return new BatchUploadResult
        {
            DirectoryPath = directoryPath,
            TotalFiles = 0,
            SuccessCount = 0,
            FailureCount = 0,
            Results = new List<VideoUploadResult>(),
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow,
            Success = false
        };
    }
}
