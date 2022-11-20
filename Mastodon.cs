using Mastonet;
using Mastonet.Entities;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace MastodonGitHubBot
{
    internal static class Mastodon
    {
        public static async Task<MastodonClient> GetClientAsync(Settings settings, HttpClient sharedHttpClient)
        {
            string accessToken = await GetAccessTokenAsync(settings, sharedHttpClient);
            return new MastodonClient(instance: settings.MastodonServer, accessToken, sharedHttpClient);
        }

        private static async Task<string> GetAccessTokenAsync(Settings settings, HttpClient sharedHttpClient)
        {
            if (string.IsNullOrWhiteSpace(settings.MastodonAccessToken))
            {
                AuthenticationClient authClient = new(instance: settings.MastodonServer, sharedHttpClient);
                await authClient.CreateApp(appName: settings.AppName, Scope.Write);

                string authCode = GetMastodonOAuthCode(authClient);

                Auth auth = await authClient.ConnectWithCode(authCode);

                settings.MastodonAccessToken = auth.AccessToken;
            }

            return settings.MastodonAccessToken;
        }

        private static string GetMastodonOAuthCode(AuthenticationClient authClient)
        {
            string url = authClient.OAuthUrl();
            Console.WriteLine($"Go to the authorization page: {url}");
            Console.Write("Paste the authentication code: ");
            string authCode = Console.ReadLine() ?? throw new NullReferenceException(nameof(authCode));
            ArgumentException.ThrowIfNullOrEmpty(authCode);

            return authCode;
        }
    }
}
