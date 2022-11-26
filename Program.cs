using Mastonet;
using Octokit;
using System.Net.Http;
using System.Threading.Tasks;

namespace MastodonGitHubBot;

public class Program
{
    private static async Task Main()
    {
        Settings settings = Settings.Load();
        HttpClient sharedHttpClient = new();
        MastodonClient mastodon = await Mastodon.GetClientAsync(settings, sharedHttpClient);
        GitHubClient github = await GitHub.GetClientAsync(settings);
        settings.Flush();

        Publisher publisher = new(settings, mastodon, github);

        await publisher.StartAsync();
    }
}