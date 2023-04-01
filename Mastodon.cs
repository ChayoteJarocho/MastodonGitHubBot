using Mastonet;
using Mastonet.Entities;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace MastodonGitHubBot;

internal static class Mastodon
{
    public static async Task<MastodonClient> GetClientAsync(Log log, Settings settings, HttpClient sharedHttpClient)
    {
        string accessToken = await GetAccessTokenAsync(log, settings, sharedHttpClient);
        return new MastodonClient(instance: settings.MastodonServer, accessToken, sharedHttpClient);
    }

    private static async Task<string> GetAccessTokenAsync(Log log, Settings settings, HttpClient sharedHttpClient)
    {
        if (string.IsNullOrWhiteSpace(settings.MastodonAccessToken))
        {
            AuthenticationClient authClient = new(instance: settings.MastodonServer, sharedHttpClient);
            await authClient.CreateApp(appName: settings.AppName, Scope.Write);

            string authCode = GetMastodonOAuthCode(log, authClient);

            Auth auth = await authClient.ConnectWithCode(authCode);

            settings.MastodonAccessToken = auth.AccessToken;
        }

        return settings.MastodonAccessToken;
    }

    private static string GetMastodonOAuthCode(Log log, AuthenticationClient authClient)
    {
        string url = authClient.OAuthUrl();
        log.WriteWarning($"Go to the authorization page '{url}' then paste the authentication code in the console.");
        Console.Write("Paste the authentication code: ");
        string authCode = Console.ReadLine() ?? throw new NullReferenceException(nameof(authCode));
        ArgumentException.ThrowIfNullOrEmpty(authCode);

        return authCode;
    }
}
