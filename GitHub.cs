using Octokit;
using System;
using System.Threading.Tasks;

namespace MastodonGitHubBot;

internal static class GitHub
{
    public static async Task<GitHubClient> GetClientAsync(Log log, Server server)
    {
        ArgumentException.ThrowIfNullOrEmpty(server.GitHubClientId);
        ArgumentException.ThrowIfNullOrEmpty(server.GitHubSecret);
        ArgumentException.ThrowIfNullOrEmpty(server.GitHubUserName);

        GitHubClient github = new(new ProductHeaderValue(server.AppName));
        string accessToken = await GetAccessTokenAsync(log, server, github);
        github.Credentials = new Credentials(accessToken);
        return github;
    }

    private static async Task<string> GetAccessTokenAsync(Log log, Server server, GitHubClient github)
    {
        if (string.IsNullOrWhiteSpace(server.GitHubAccessToken))
        {
            OauthLoginRequest oauthLoginRequest = new(server.GitHubClientId);
            oauthLoginRequest.Scopes.Add(server.GitHubUserName);
            oauthLoginRequest.Scopes.Add("public_repo");

            string authCode = GetGitHubOAuthCode(log, github, oauthLoginRequest);

            OauthTokenRequest oauthTokenRequest = new(server.GitHubClientId, server.GitHubSecret, authCode);
            OauthToken accessToken = await github.Oauth.CreateAccessToken(oauthTokenRequest);

            server.GitHubAccessToken = accessToken.AccessToken;
        }

        return server.GitHubAccessToken;
    }

    private static string GetGitHubOAuthCode(Log log, GitHubClient github, OauthLoginRequest oauthLoginRequest)
    {
        Uri url = github.Oauth.GetGitHubLoginUrl(oauthLoginRequest);
        log.WriteWarning($"Go to the authorization page '{url}' then paste the authentication code in the console.");
        Console.Write("Paste the authentication code: ");
        string authCode = Console.ReadLine() ?? throw new NullReferenceException(nameof(authCode));
        ArgumentException.ThrowIfNullOrEmpty(authCode);

        return authCode;
    }
}
