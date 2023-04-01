using Mastonet;
using Octokit;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace MastodonGitHubBot;

public class Program
{
    private static async Task Main()
    {
        using Log log = new();
        try
        {
            Settings settings = Settings.Load(log);
            HttpClient sharedHttpClient = new();
            MastodonClient mastodon = await Mastodon.GetClientAsync(log, settings, sharedHttpClient);
            GitHubClient github = await GitHub.GetClientAsync(log, settings);
            settings.Flush();

            Publisher publisher = new(log, settings, mastodon, github);
            await publisher.StartAsync();
        }
        catch (Exception e)
        {
            log.WriteFailure($"{e}: {e.Message}{Environment.NewLine}{e.StackTrace}");
            throw;
        }
    }
}