# YouTube Shorts Publisher - C#/.NET

🎬 Automated YouTube Shorts uploader using YouTube Data API v3 built with C#/.NET

## Features

- ✅ **Official YouTube API** - YouTube Data API v3
- ✅ **OAuth 2.0** - Secure authentication with Google
- ✅ **Direct Publishing** - Upload videos as Shorts directly
- ✅ **Batch Upload** - Upload all videos from directory
- ✅ **Token Management** - Automated access token refresh
- ✅ **CLI Interface** - Command-line interface for all operations
- ✅ **.NET 8.0** - Built with modern C# features
- ✅ **Progress Tracking** - Real-time upload progress display

## Requirements

- .NET 8.0 SDK
- [Google Cloud Project](https://console.cloud.google.com/)
- YouTube Data API v3 enabled
- OAuth Client credentials

## Installation

1. **Clone repository**
```bash
git clone https://github.com/YOUR_USERNAME/youtube-shorts-publisher-csharp.git
cd youtube-shorts-publisher-csharp
```

2. **Install .NET SDK** (if not installed)
```bash
# Linux/macOS
curl -sSL https://dot.net/v1/dotnet-install.sh | bash

# Windows
# Download from: https://dotnet.microsoft.com/download
```

3. **Restore NuGet packages**
```bash
dotnet restore
```

## Setup

### 1. Create Google Cloud Project & OAuth Client

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project
3. Navigate to: **APIs & Services → OAuth consent screen**
   - Type: External
   - Add scopes: `https://www.googleapis.com/auth/youtube`
4. Navigate to: **APIs & Services → Credentials**
   - Create OAuth 2.0 Client ID
   - Application type: Web application
   - Add redirect URI: `https://oauth.pstmn.io/v1/callback`
   - Save **CLIENT_ID** and **CLIENT_SECRET**
5. Enable **YouTube Data API v3**:
   - Navigate to: **APIs & Services → Library**
   - Search: YouTube Data API v3
   - Click: Enable

### 2. Configure Credentials

Run the setup command:
```bash
dotnet run setup
```

Or manually create `Config/api_credentials.json`:
```json
{
  "client_id": "your_client_id.apps.googleusercontent.com",
  "client_secret": "your_client_secret",
  "access_token": "",
  "refresh_token": "",
  "token_type": "Bearer",
  "expires_in": 0,
  "scope": "https://www.googleapis.com/auth/youtube",
  "status": "configured"
}
```

### 3. Authorize

```bash
dotnet run exchange
```

Follow these steps:
1. Open the Authorization URL that appears
2. Choose your Google account and authorize
3. Paste the callback URL back into the terminal
4. Tokens will be saved automatically

## Usage

### CLI Commands

**Show help:**
```bash
dotnet run -- --help
```

**Setup credentials:**
```bash
dotnet run setup
```

**Exchange authorization code:**
```bash
dotnet run exchange
```

**Upload single video:**
```bash
dotnet run upload video.mp4
# Or with privacy:
dotnet run upload video.mp4 private
```

Privacy options:
- `public` - Public video
- `unlisted` - Unlisted (link only)
- `private` - Private only

**Batch upload all videos:**
```bash
dotnet run upload-all ../videos/
# Or with privacy:
dotnet run upload-all ../videos/ unlisted
```

### Results

After upload:
- Video URL format: `https://youtube.com/shorts/{VIDEO_ID}`
- Video appears automatically in YouTube Shorts

### Example Output

**Single Upload:**
```
╔═══════════════════════════════════════════════════════════╗
║     YouTube Shorts Publisher - C#/.NET                  ║
║     YouTube Data API v3                                   ║
╚═══════════════════════════════════════════════════════════╝

📤 Upload Single Video
============================================================
📤 Uploading: my_short.mp4
   Title: my_short #Shorts
   Privacy: public
   Size: 2.5 MB

   Starting upload...
   Progress: 100% (2.5 MB / 2.5 MB)
✅ Upload complete!
   Video ID: dQw4w9WgXcQ
   URL: https://youtube.com/shorts/dQw4w9WgXcQ

✅ Video uploaded successfully!
   Video ID: dQw4w9WgXcQ
   URL: https://youtube.com/shorts/dQw4w9WgXcQ
   Note: Video will appear in Shorts!
```

**Batch Upload:**
```
============================================================
📦 Batch Upload: ../videos/
============================================================
📋 Found 10 video file(s)

[1/10] 📤 Uploading: video_001.mp4
   Progress: 100%
✅ Video uploaded successfully! Video ID: xxx

[2/10] 📤 Uploading: video_002.mp4
   Progress: 100%
✅ Video uploaded successfully! Video ID: xxx
...

============================================================
📊 BATCH UPLOAD SUMMARY
============================================================
📁 Total files: 10
✅ Success: 10
❌ Failed: 0
⏱️  Duration: 5.23 minutes
```

## File Structure

```
youtube-shorts-publisher-csharp/
├── youtube-shorts-publisher-csharp.csproj
├── Program.cs
├── Models/
│   ├── Credentials.cs
│   ├── VideoUploadRequest.cs
│   └── YouTubeApiResponses.cs
├── Services/
│   ├── GoogleOAuthClient.cs
│   ├── TokenManager.cs
│   └── YouTubeApiClient.cs
├── Config/
│   └── api_credentials.json
├── README.md
└── .gitignore
```

## API Endpoints

### YouTube Data API v3

| Endpoint | Method | Description |
|----------|--------|-------------|
| /youtube/v3/videos | POST | Upload video |
| /youtube/v3/videos | GET | Get video info |
| /oauth2/v4/token | POST | Exchange token |

### Upload Flow

1. **Initialize Upload** - Create video resource
2. **Upload Video** - Resumable upload with chunks
3. **Complete Upload** - Finalize and publish
4. **Get Video Info** - Retrieve video ID

## Video Requirements

- **Format:** `.mp4`, `.mov`, `.avi`
- **Max size:** 256 GB
- **Max duration:** 12 hours
- **Recommended aspect ratio:** 9:16 (vertical for Shorts)
- **Recommended resolution:** 1080x1920

## Troubleshooting

### Missing .NET SDK

```bash
# Install .NET SDK 8.0
curl -sSL https://dot.net/v1/dotnet-install.sh | bash
export PATH=$PATH:~/.dotnet
```

### Invalid Grant

**Problem:** Refresh token expired or revoked

**Solution:**
- Run `dotnet run exchange` again
- Re-authorize the application

### Quota Exceeded

**Problem:** API quota exceeded (24-hour limit)

**Solution:**
- Wait until quota resets
- Request quota increase via Google Cloud Console

## Security

- ❌ **NOT** commit `Config/api_credentials.json` to git repository
- ❌ **NOT** share OAuth tokens
- ✅ Use environment variables for production
- ✅ Secure your Google Cloud Project

## Deployment

### Build for Production

```bash
# Build Release
dotnet build --configuration Release

# Publish as self-contained app
dotnet publish -c Release -r linux-x64 --self-contained
```

### Docker Deployment

Create `Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "youtube-shorts-publisher-csharp.dll"]
```

Build and run:
```bash
docker build -t youtube-shorts-publisher .
docker run -v $(pwd)/Config:/app/Config -v $(pwd)/videos:/app/videos youtube-shorts-publisher upload-all /app/videos
```

### Cron Job

```bash
# Edit crontab
crontab -e

# Run every day at 2 AM
0 2 * * * cd /path/to/youtube-shorts-publisher && ~/.dotnet/dotnet run -- upload-all ../videos/ >> logs/bot.log 2>&1
```

## NuGet Dependencies

- `Google.Apis.YouTube.v3` - YouTube Data API
- `Google.Apis.Auth` - Google OAuth
- `Newtonsoft.Json` - JSON serialization
- `Microsoft.Extensions.DependencyInjection` - DI container
- `Microsoft.Extensions.Logging.Console` - Logging

## Code Features

- ✅ Google Apis .NET client
- ✅ OAuth 2.0 with refresh tokens
- ✅ Async/await throughout
- ✅ Progress tracking during upload
- ✅ Strong typing with C# records
- ✅ Error handling with detailed messages
- ✅ CLI with intuitive commands
- ✅ Token auto-refresh
- ✅ Batch uploading with summary

## API References

- [YouTube Data API v3](https://developers.google.com/youtube/v3)
- [YouTube API Upload](https://developers.google.com/youtube/v3/guides/uploading_a_video)
- [Google OAuth 2.0](https://developers.google.com/identity/protocols/oauth2)
- [.NET Documentation](https://docs.microsoft.com/dotnet/)

## License

MIT

## Contributing

PRs welcome! Please:
- Follow C# coding conventions
- Run `dotnet build` before committing
- Update documentation

---

**Built with ❤️ using C#/.NET 8.0 + Google.Apis** 🚀
