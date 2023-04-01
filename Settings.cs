using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MastodonGitHubBot;

internal sealed class Settings
{
    private static readonly string SettingsFilePath = Path.GetFullPath(Path.Join(AppContext.BaseDirectory, "appsettings.json"));

    public string AppName { get; set; } = string.Empty;
    public string GitHubClientId { get; set; } = string.Empty;
    public string GitHubSecret { get; set; } = string.Empty;
    public string GitHubAccessToken { get; set; } = string.Empty;
    public string GitHubUserName { get; set; } = string.Empty;
    public string GitHubRepoOrg { get; set; } = string.Empty;
    public string GitHubRepoName { get; set; } = string.Empty;
    public int GitHubLatestIssueNumber { get; set; } = 0;
    public string MastodonServer { get; set; } = string.Empty;
    public string MastodonAccessToken { get; set; } = string.Empty;
    public Mastonet.Visibility Visibility { get; set; } = Mastonet.Visibility.Unlisted;
    public int SleepSeconds { get; set; } = 120;
    public bool Debug { get; set; } = true;

    public void Flush()
    {
        JsonSerializerOptions options = new() { WriteIndented = true };
        string json = JsonSerializer.Serialize(this, options);
        File.WriteAllText(SettingsFilePath, json);
    }

    public static Settings Load(Log log)
    {
        ArgumentNullException.ThrowIfNull(log);

        if (!File.Exists(SettingsFilePath))
        {
            throw new FileNotFoundException(SettingsFilePath);
        }
        log.WriteInfo($"Reading settings file: {SettingsFilePath}");

        string json = File.ReadAllText(SettingsFilePath);
        JsonSerializerOptions options = new();
        options.Converters.Add(new JsonStringEnumConverter());
        Settings settings = JsonSerializer.Deserialize<Settings>(json, options) ?? throw new JsonException($"Malformed {SettingsFilePath}.");

        log.WriteInfo("Verifying that all settings values are filled out or prompting user if a value is missing...");
        if (string.IsNullOrWhiteSpace(settings.AppName))
        {
            settings.AppName = AskString("name of the GitHub application reserved for the OAuth services. If you haven't created it, create it in https://github.com/settings/applications/new. After creating it, make sure to also store the client and the secret values");
        }
        if (string.IsNullOrWhiteSpace(settings.GitHubClientId))
        {
            settings.GitHubClientId = AskString("value of the GitHub OAuth application's ClientId. You should have gotten one after creating your application");
        }
        if (string.IsNullOrWhiteSpace(settings.GitHubSecret))
        {
            settings.GitHubSecret = AskString("value of the GitHub OAuth application's secret. You should have gotten one after creating your application");
        }
        if (string.IsNullOrWhiteSpace(settings.GitHubUserName))
        {
            settings.GitHubUserName = AskString("value of the GitHub account handle that will be used to execute requests");
        }
        if (string.IsNullOrWhiteSpace(settings.GitHubRepoOrg))
        {
            settings.GitHubRepoOrg = AskString("value of the GitHub org or user that owns the target repo");
        }
        if (string.IsNullOrWhiteSpace(settings.GitHubRepoName))
        {
            settings.GitHubRepoName = AskString("value of the target GitHub repo name");
        }
        if (string.IsNullOrWhiteSpace(settings.MastodonServer))
        {
            settings.MastodonServer = AskString("name of the Mastodon server that hosts your automated account");
        }
        if (string.IsNullOrWhiteSpace(settings.GitHubAccessToken))
        {
            settings.GitHubAccessToken = AskOptionalString("GitHub access token");
        }
        if (string.IsNullOrWhiteSpace(settings.MastodonAccessToken))
        {
            settings.GitHubAccessToken = AskOptionalString("Mastodon access token");
        }
        if (settings.Visibility is < Mastonet.Visibility.Public or > Mastonet.Visibility.Direct)
        {
            throw new ArgumentOutOfRangeException($"Visibility value is out of range: {settings.Visibility}. Allowed numeric values are: 0 (Public), 1 (Unlisted), 2 (Private), 3 (Direct).");
        }
        if (settings.SleepSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException("SleepSeconds", "The SleepSeconds value should be a positive number.");
        }

        log.WriteInfo(settings.Debug ?
            "Debugging mode enabled. Won't publish to Mastodon." :
            "Debugging mode disabled. Will publish to Mastodon.");

        return settings;
    }

    private static string AskString(string description)
    {
        string? value = null;

        while (string.IsNullOrWhiteSpace(value))
        {
            Console.Write($"Please provide the {description}: ");
            value = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(value))
            {
                Console.WriteLine("Incorrect value (null, empty or whitespace). Try again.");
            }
            else
            {
                string? answer = null;
                while (string.IsNullOrWhiteSpace(answer))
                {
                    Console.Write($"You typed [{value}]. Is this correct? [Y/N]: ");
                    answer = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(answer))
                    {
                        switch (answer)
                        {
                            case "n" or "N":
                                Console.WriteLine("Ok, starting over.");
                                value = null;
                                break;
                            case "y" or "Y":
                                Console.WriteLine($"Will save [{value}] into the json file.");
                                break;
                            default:
                                Console.WriteLine("Unexpected value. Asking again.");
                                answer = null;
                                break;
                        }
                    }
                }
            }
        }

        return value;
    }

    private static string AskOptionalString(string description)
    {
        string value = string.Empty;
        string? answer = null;
        while (string.IsNullOrWhiteSpace(answer))
        {
            Console.Write($"Do you already have a {description}? [Y/N]: ");
            answer = Console.ReadLine();
            switch (answer)
            {
                case "n" or "N":
                    Console.WriteLine("No problem. Will go through the authentication process.");
                    break;
                case "y" or "Y":
                    value = AskString(description);
                    break;
                default:
                    Console.WriteLine("Unexpected value. Asking again.");
                    answer = null;
                    break;
            }
        }
        return value;
    }
}
