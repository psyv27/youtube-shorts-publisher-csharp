using Microsoft.Extensions.DependencyInjection;
using YouTubeShortsPublisher.Services;

namespace YouTubeShortsPublisher;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     YouTube Shorts Publisher - C#/.NET                  ║");
        Console.WriteLine("║     YouTube Data API v3                                   ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
        Console.WriteLine("");

        if (args.Length == 0 || args.Contains("-h") || args.Contains("--help"))
        {
            ShowHelp();
            return 0;
        }

        var command = args[0].ToLower();

        var services = ConfigureServices();
        var serviceProvider = services.BuildServiceProvider();

        int exitCode = 0;

        try
        {
            switch (command)
            {
                case "exchange":
                    await ExecuteExchangeCommand(serviceProvider);
                    break;

                case "upload":
                    await ExecuteUploadSingleCommand(serviceProvider, args);
                    break;

                case "upload-all":
                case "batch":
                    await ExecuteBatchUploadCommand(serviceProvider, args);
                    break;

                case "setup":
                    await ExecuteSetupCommand();
                    break;

                default:
                    Console.WriteLine($"❌ Unknown command: {command}");
                    exitCode = 1;
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Fatal error: {ex.Message}");
            Console.WriteLine($"   {ex.StackTrace}");
            exitCode = 1;
        }
        finally
        {
            serviceProvider.Dispose();
        }

        return exitCode;
    }

    static void ShowHelp()
    {
        Console.WriteLine("Usage: dotnet run <command> [options]");
        Console.WriteLine("");
        Console.WriteLine("Commands:");
        Console.WriteLine("  setup                       Setup OAuth credentials");
        Console.WriteLine("  exchange                    Exchange authorization code for tokens");
        Console.WriteLine("  upload <file> [privacy]     Upload single video");
        Console.WriteLine("  upload-all [directory]      Upload all videos from directory");
        Console.WriteLine("");
        Console.WriteLine("Options:");
        Console.WriteLine("  privacy                     public|unlisted|private (default: public)");
        Console.WriteLine("");
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run setup");
        Console.WriteLine("  dotnet run exchange");
        Console.WriteLine("  dotnet run upload video.mp4");
        Console.WriteLine("  dotnet run upload video.mp4 private");
        Console.WriteLine("  dotnet run upload-all ../videos/");
        Console.WriteLine("");
        Console.WriteLine("Setup:");
        Console.WriteLine("  1. Go to https://console.cloud.google.com/");
        Console.WriteLine("  2. Create Project & OAuth Client");
        Console.WriteLine("  3. Enable YouTube Data API v3");
        Console.WriteLine("  4. Run 'dotnet run setup'");
        Console.WriteLine("  5. Run 'dotnet run exchange'");
        Console.WriteLine("");
        Console.WriteLine("Configuration:");
        Console.WriteLine("  Credentials are loaded from: Config/api_credentials.json");
    }

    static void ConfigureServices(IServiceCollection services)
    {
        var tokenManager = new TokenManager();
        services.AddSingleton(tokenManager);
        services.AddHttpClient();
    }

    static async Task ExecuteSetupCommand()
    {
        Console.WriteLine("🔧 YouTube Data API Setup");
        Console.WriteLine("="*60);
        Console.WriteLine("");
        Console.WriteLine("To use this publisher, you need to:");
        Console.WriteLine("");
        Console.WriteLine("1. Go to Google Cloud Console:");
        Console.WriteLine("   https://console.cloud.google.com/");
        Console.WriteLine("");
        Console.WriteLine("2. Create a new project");
        Console.WriteLine("");
        Console.WriteLine("3. Navigate to: APIs & Services → OAuth consent screen");
        Console.WriteLine("   - Type: External");
        Console.WriteLine("   - Add scopes: https://www.googleapis.com/auth/youtube");
        Console.WriteLine("");
        Console.WriteLine("4. Navigate to: APIs & Services → Credentials");
        Console.WriteLine("   - Create OAuth 2.0 Client ID");
        Console.WriteLine("   - Application type: Web application");
        Console.WriteLine("   - Add redirect URI: https://oauth.pstmn.io/v1/callback");
        Console.WriteLine("");
        Console.WriteLine("5. Enable YouTube Data API v3:");
        Console.WriteLine("   - Navigate to: APIs & Services → Library");
        Console.WriteLine("   - Search: YouTube Data API v3");
        Console.WriteLine("   - Click: Enable");
        Console.WriteLine("");
        Console.WriteLine("6. Save your CLIENT_ID and CLIENT_SECRET");
        Console.WriteLine("");
        Console.WriteLine("7. Run 'dotnet run exchange' to authorize");
        Console.WriteLine("");
        Console.WriteLine("="*60);

        Console.Write("\n📝 Enter CLIENT_ID: ");
        var clientId = Console.ReadLine() ?? string.Empty;

        Console.Write("📝 Enter CLIENT_SECRET: ");
        var clientSecret = Console.ReadLine() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            Console.WriteLine("❌ Client ID and Secret are required");
            return;
        }

        var credentials = new Models.Credentials
        {
            ClientId = clientId.Trim(),
            ClientSecret = clientSecret.Trim(),
            AccessToken = string.Empty,
            RefreshToken = string.Empty,
            TokenType = "Bearer",
            ExpiresIn = 0,
            Scope = "https://www.googleapis.com/auth/youtube",
            Status = "configured"
        };

        var tokenManager = new TokenManager();
        var saved = tokenManager.SaveCredentials(credentials);

        if (saved)
        {
            Console.WriteLine("");
            Console.WriteLine("✅ Credentials saved!");
            Console.WriteLine("");
            Console.WriteLine("Next steps:");
            Console.WriteLine("  1. Run: dotnet run exchange");
            Console.WriteLine("  2. Authorize the app");
            Console.WriteLine("  3. Start uploading videos!");
        }
        else
        {
            Console.WriteLine("❌ Failed to save credentials");
        }
    }

    static async Task ExecuteExchangeCommand(IServiceProvider serviceProvider)
    {
        Console.WriteLine("🔐 OAuth - Exchange Authorization Code for Tokens");
        Console.WriteLine("="*60);

        var tokenManager = serviceProvider.GetRequiredService<TokenManager>();

        if (!tokenManager.LoadCredentials())
        {
            Console.WriteLine("❌ Failed to load credentials");
            Console.WriteLine("   Please run 'dotnet run setup' first");
            return;
        }

        var authUrl = tokenManager.GenerateAuthUrl();
        Console.WriteLine("1️⃣  Open this URL in your browser:");
        Console.WriteLine("");
        Console.WriteLine(authUrl);
        Console.WriteLine("");
        Console.WriteLine("2️⃣  Choose your Google account and authorize");
        Console.WriteLine("3️⃣  Copy the full callback URL from the browser");
        Console.WriteLine("");

        Console.Write("📋 Paste the callback URL: ");
        var callbackUrl = Console.ReadLine() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(callbackUrl))
        {
            Console.WriteLine("❌ No callback URL provided");
            return;
        }

        var authCode = TokenManager.ExtractAuthorizationCode(callbackUrl);

        if (string.IsNullOrEmpty(authCode))
        {
            Console.WriteLine("❌ Failed to extract authorization code");
            return;
        }

        Console.WriteLine($"✅ Authorization code extracted: {authCode[0..20]}...");

        var tokenResponse = await tokenManager.ExchangeCodeForTokenAsync(authCode);

        if (tokenResponse == null || tokenResponse.HasError)
        {
            Console.WriteLine("❌ Token exchange failed");
            return;
        }

        Console.WriteLine("");
        Console.WriteLine("✅ Tokens saved successfully!");
        Console.WriteLine("");
        Console.WriteLine("You can now use other commands to upload videos");
    }

    static async Task ExecuteUploadSingleCommand(IServiceProvider serviceProvider, string[] args)
    {
        Console.WriteLine("📤 Upload Single Video");
        Console.WriteLine("="*60);

        string filePath;
        string privacy = "public";

        if (args.Length > 1)
        {
            filePath = args[1];
        }
        else
        {
            Console.Write("📹 Enter video file path: ");
            filePath = Console.ReadLine() ?? string.Empty;
        }

        if (args.Length > 2)
        {
            privacy = args[2].ToLower();
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            Console.WriteLine("❌ No file path provided");
            return;
        }

        var tokenManager = serviceProvider.GetRequiredService<TokenManager>();

        if (!tokenManager.LoadCredentials())
        {
            Console.WriteLine("❌ Failed to load credentials");
            return;
        }

        if (tokenManager.ShouldRefreshAccessToken())
        {
            await tokenManager.RefreshAccessTokenAsync();
        }

        var credentials = tokenManager.GetCredentials();
        var apiClient = new YouTubeApiClient(credentials!);

        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var title = $"{fileName} #Shorts";
        var description = $"#{fileName.Replace("_", " ")} #Shorts #ShortsVideo";

        await apiClient.UploadVideoAsync(filePath, title, description, privacy);
    }

    static async Task ExecuteBatchUploadCommand(IServiceProvider serviceProvider, string[] args)
    {
        Console.WriteLine("📦 Batch Upload");
        Console.WriteLine("="*60);

        string directoryPath;
        string privacy = "public";

        if (args.Length > 1)
        {
            directoryPath = args[1];
        }
        else
        {
            Console.Write("📁 Enter directory path: ");
            directoryPath = Console.ReadLine() ?? string.Empty;
        }

        if (args.Length > 2)
        {
            privacy = args[2].ToLower();
        }

        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            directoryPath = "../youtube-shorts/downloads/";
            Console.WriteLine($"Using default directory: {directoryPath}");
        }

        var tokenManager = serviceProvider.GetRequiredService<TokenManager>();

        if (!tokenManager.LoadCredentials())
        {
            Console.WriteLine("❌ Failed to load credentials");
            return;
        }

        if (tokenManager.ShouldRefreshAccessToken())
        {
            await tokenManager.RefreshAccessTokenAsync();
        }

        var credentials = tokenManager.GetCredentials();
        var apiClient = new YouTubeApiClient(credentials!);

        await apiClient.UploadBatchAsync(directoryPath, "*.mp4", privacy);
    }
}
