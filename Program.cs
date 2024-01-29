using Mastonet;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MastodonGitHubBot;

public class Program
{
    private static readonly string SettingsFilePath = Path.GetFullPath(Path.Join(AppContext.BaseDirectory, "appsettings.json"));

    private static async Task Main()
    {
        using Log log = new();
        try
        {
            await StartAsync(log);
        }
        catch (Exception e)
        {
            log.WriteFailure($"{e}: {e.Message}{Environment.NewLine}{e.StackTrace}");
            throw;
        }
    }

    private static async Task StartAsync(Log log)
    {
        Settings settings = Load(log);
        HttpClient sharedHttpClient = new();

        List<Publisher> publishers = new();
        foreach (Server server in settings.Servers)
        {
            var publisher = await Publisher.CreateAsync(log, sharedHttpClient, settings, server);
            publishers.Add(publisher);
        }

        log.WriteInfo($"Starting loop...");
        while (true)
        {
            foreach (Publisher publisher in publishers)
            {
                await publisher.PublishAsync();
            }
            Flush(settings);
        }
    }

    private static void Flush(Settings settings)
    {
        JsonSerializerOptions options = new() { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());
        string json = JsonSerializer.Serialize(settings, options);
        File.WriteAllText(SettingsFilePath, json);
    }

    private static Settings Load(Log log)
    {
        ArgumentNullException.ThrowIfNull(log);

        if (!File.Exists(SettingsFilePath))
        {
            throw new FileNotFoundException(SettingsFilePath);
        }
        log.WriteInfo($"Reading settings file: {SettingsFilePath}");

        string json = File.ReadAllText(SettingsFilePath);
        JsonSerializerOptions options = new() { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());
        Settings settings = JsonSerializer.Deserialize<Settings>(json, options) ?? throw new JsonException($"Malformed {SettingsFilePath}.");

        log.WriteInfo("Verifying that all settings values are filled out or prompting user if a value is missing...");

        if (settings.SleepSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException("The SleepSeconds value should be a positive number.", innerException: null);
        }

        log.WriteInfo("Debugging mode " + (settings.Debug ? "enabled" : "disabled") + ". Won't publish to Mastodon.");

        foreach (Server server in settings.Servers)
        {
            if (string.IsNullOrWhiteSpace(server.ServerName))
            {
                throw new JsonException("ServerName should not be empty.");
            }
            if (string.IsNullOrWhiteSpace(server.AppName))
            {
                server.AppName = AskString(server.ServerName, "name of the GitHub application reserved for the OAuth services. If you haven't created it, create it in https://github.com/settings/applications/new. After creating it, make sure to also store the client and the secret values");
            }
            if (string.IsNullOrWhiteSpace(server.GitHubClientId))
            {
                server.GitHubClientId = AskString(server.ServerName, "value of the GitHub OAuth application's ClientId. You should have gotten one after creating your application");
            }
            if (string.IsNullOrWhiteSpace(server.GitHubSecret))
            {
                server.GitHubSecret = AskString(server.ServerName, "value of the GitHub OAuth application's secret. You should have gotten one after creating your application");
            }
            if (string.IsNullOrWhiteSpace(server.GitHubUserName))
            {
                server.GitHubUserName = AskString(server.ServerName, "value of the GitHub account handle that will be used to execute requests");
            }
            if (string.IsNullOrWhiteSpace(server.GitHubRepoOrg))
            {
                server.GitHubRepoOrg = AskString(server.ServerName, "value of the GitHub org or user that owns the target repo");
            }
            if (string.IsNullOrWhiteSpace(server.GitHubRepoName))
            {
                server.GitHubRepoName = AskString(server.ServerName, "value of the target GitHub repo name");
            }
            if (string.IsNullOrWhiteSpace(server.MastodonServer))
            {
                server.MastodonServer = AskString(server.ServerName, "name of the Mastodon server that hosts your automated account");
            }
            if (string.IsNullOrWhiteSpace(server.GitHubAccessToken))
            {
                server.GitHubAccessToken = AskOptionalString(server.ServerName, "GitHub access token");
            }
            if (string.IsNullOrWhiteSpace(server.MastodonAccessToken))
            {
                server.GitHubAccessToken = AskOptionalString(server.ServerName, "Mastodon access token");
            }
            if (server.Visibility is < Mastonet.Visibility.Public or > Mastonet.Visibility.Direct)
            {
                throw new ArgumentOutOfRangeException($"{server.ServerName} - Visibility value is out of range: {server.Visibility}. Allowed numeric values are: 0 (Public), 1 (Unlisted), 2 (Private), 3 (Direct).", innerException: null);
            }
        }

        return settings;
    }

    private static string AskString(string serverName, string description)
    {
        string? value = null;

        while (string.IsNullOrWhiteSpace(value))
        {
            Console.Write($"{serverName} - Please provide the {description}: ");
            value = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(value))
            {
                Console.WriteLine($"{serverName} - Incorrect value (null, empty or whitespace). Try again.");
            }
            else
            {
                string? answer = null;
                while (string.IsNullOrWhiteSpace(answer))
                {
                    Console.Write($"{serverName} - You typed [{value}]. Is this correct? [Y/N]: ");
                    answer = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(answer))
                    {
                        switch (answer)
                        {
                            case "n" or "N":
                                Console.WriteLine($"{serverName} - Ok, starting over.");
                                value = null;
                                break;
                            case "y" or "Y":
                                Console.WriteLine($"{serverName} - Will save [{value}] into the json file.");
                                break;
                            default:
                                Console.WriteLine($"{serverName} - Unexpected value. Asking again.");
                                answer = null;
                                break;
                        }
                    }
                }
            }
        }

        return value;
    }

    private static string AskOptionalString(string serverName, string description)
    {
        string value = string.Empty;
        string? answer = null;
        while (string.IsNullOrWhiteSpace(answer))
        {
            Console.Write($"{serverName} - Do you already have a {description}? [Y/N]: ");
            answer = Console.ReadLine();
            switch (answer)
            {
                case "n" or "N":
                    Console.WriteLine($"{serverName} - No problem. Will go through the authentication process.");
                    break;
                case "y" or "Y":
                    value = AskString(serverName, description);
                    break;
                default:
                    Console.WriteLine($"{serverName}  - Unexpected value. Asking again.");
                    answer = null;
                    break;
            }
        }
        return value;
    }
}