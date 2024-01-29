using Mastonet;
using Mastonet.Entities;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace MastodonGitHubBot;

internal static class Mastodon
{
    public static async Task<MastodonClient> GetClientAsync(Log log, Server server, HttpClient sharedHttpClient)
    {
        string accessToken = await GetAccessTokenAsync(log, server, sharedHttpClient);
        return new MastodonClient(instance: server.MastodonServer, accessToken, sharedHttpClient);
    }

    private static async Task<string> GetAccessTokenAsync(Log log, Server server, HttpClient sharedHttpClient)
    {
        if (string.IsNullOrWhiteSpace(server.MastodonAccessToken))
        {
            AuthenticationClient authClient = new(instance: server.MastodonServer, sharedHttpClient);
            await authClient.CreateApp(appName: server.AppName, Scope.Write);

            string authCode = GetMastodonOAuthCode(log, authClient);

            Auth auth = await authClient.ConnectWithCode(authCode);

            server.MastodonAccessToken = auth.AccessToken;
        }

        return server.MastodonAccessToken;
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
