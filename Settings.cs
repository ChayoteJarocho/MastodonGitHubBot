using System.IO;
using System.Text.Json;

namespace MastodonGitHubBot
{
    internal sealed class Settings
    {
        private static readonly string SettingsFilePath = "appsettings.json";

        public string AppName { get; set; } = string.Empty;
        public string GitHubClientId { get; set; } = string.Empty;
        public string GitHubSecret { get; set; } = string.Empty;
        public string GitHubAccessToken { get; set; } = string.Empty;
        public string GitHubUserName { get; set; } = string.Empty;
        public string MastodonServer { get; set; } = string.Empty;
        public string MastodonAccessToken { get; set; } = string.Empty;

        public void Flush()
        {
            JsonSerializerOptions options = new() { WriteIndented = true };
            string json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(SettingsFilePath, json);
        }

        public static Settings Load()
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
    }
}
