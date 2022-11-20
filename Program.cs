using Mastonet;
using Octokit;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace MastodonGitHubBot;

public class Program
{
    private static readonly string SettingsFilePath = "appsettings.json";
    private static async Task Main()
    {
        Settings settings = LoadSettings();

        HttpClient sharedHttpClient = new HttpClient();

        MastodonClient mastodon = await Mastodon.GetClientAsync(settings, sharedHttpClient);

        GitHubClient github = await GitHub.GetClientAsync(settings);

        FlushSettings(settings);

       // Status status = await mastodon.PublishStatus("Test", Visibility.Private);
    }

    private static Settings LoadSettings()
    {
        if (!File.Exists(SettingsFilePath))
        {
            throw new FileNotFoundException(SettingsFilePath);
        }

        string json = File.ReadAllText(SettingsFilePath);
        Settings settings = JsonSerializer.Deserialize<Settings>(json) ?? throw new JsonException($"Malformed {SettingsFilePath}.");

        string? message = null;

        if (string.IsNullOrWhiteSpace(settings.AppName))
        {
            message = $"AppName is not set in {SettingsFilePath}. It needs to be a fixed value that the OAuth services can recognize.";
        }
        if (string.IsNullOrWhiteSpace(settings.GitHubClientId))
        {
            message = $"GitHubClientId is not set in {SettingsFilePath}. You must create an OAuth application on GitHub, copy the ClientId value, then paste it in {SettingsFilePath}. The apps are created here: https://github.com/settings/applications/new";
        }
        if (string.IsNullOrWhiteSpace(settings.GitHubSecret))
        {
            message = $"GitHubSecret is not set in {SettingsFilePath}. You must create an OAuth application on GitHub, generate a secret for that app, then paste the value in {SettingsFilePath}. The apps are created here: https://github.com/settings/applications/new";
        }
        if (string.IsNullOrWhiteSpace(settings.GitHubUserName))
        {
            message = $"GitHubUserName is not set in {SettingsFilePath}. It needs to be set to the user account handle that will be executing the requests.";
        }
        if (string.IsNullOrWhiteSpace(settings.MastodonServer))
        {
            message = $"MastodonServer is not set in {SettingsFilePath}. It needs to be set to the name of the Mastodon server that hosts the automated account.";
        }

        if (message != null)
        {
            throw new InvalidDataException(message);
        }

        return settings;
    }

    private static void FlushSettings(Settings settings)
    {
        string json = JsonSerializer.Serialize(settings);
        File.WriteAllText(SettingsFilePath, json);
    }
}