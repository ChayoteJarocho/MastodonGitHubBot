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

        HttpClient sharedHttpClient = new HttpClient();

        MastodonClient mastodon = await Mastodon.GetClientAsync(settings, sharedHttpClient);

        GitHubClient github = await GitHub.GetClientAsync(settings);

        settings.Flush();

       // Status status = await mastodon.PublishStatus("Test", Visibility.Private);
    }
}