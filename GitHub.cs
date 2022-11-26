using Octokit;
using System;
using System.Threading.Tasks;

namespace MastodonGitHubBot;

internal static class GitHub
{
    public static async Task<GitHubClient> GetClientAsync(Settings settings)
    {
        ArgumentException.ThrowIfNullOrEmpty(settings.GitHubClientId);
        ArgumentException.ThrowIfNullOrEmpty(settings.GitHubSecret);
        ArgumentException.ThrowIfNullOrEmpty(settings.GitHubUserName);

        GitHubClient github = new(new ProductHeaderValue(settings.AppName));
        string accessToken = await GetAccessTokenAsync(settings, github);
        github.Credentials = new Credentials(accessToken);

        return github;
    }

    private static async Task<string> GetAccessTokenAsync(Settings settings, GitHubClient github)
    {
        if (string.IsNullOrWhiteSpace(settings.GitHubAccessToken))
        {
            OauthLoginRequest oauthLoginRequest = new(settings.GitHubClientId);
            oauthLoginRequest.Scopes.Add(settings.GitHubUserName);
            oauthLoginRequest.Scopes.Add("public_repo");

            string authCode = GetGitHubOAuthCode(github, oauthLoginRequest);

            OauthTokenRequest oauthTokenRequest = new(settings.GitHubClientId, settings.GitHubSecret, authCode);
            OauthToken accessToken = await github.Oauth.CreateAccessToken(oauthTokenRequest);

            settings.GitHubAccessToken = accessToken.AccessToken;
        }

        return settings.GitHubAccessToken;
    }

    private static string GetGitHubOAuthCode(GitHubClient github, OauthLoginRequest oauthLoginRequest)
    {
        Uri url = github.Oauth.GetGitHubLoginUrl(oauthLoginRequest);
        Console.WriteLine($"Go to the authorization page: {url}");
        Console.Write("Paste the authentication code: ");
        string authCode = Console.ReadLine() ?? throw new NullReferenceException(nameof(authCode));
        ArgumentException.ThrowIfNullOrEmpty(authCode);

        return authCode;
    }
}
