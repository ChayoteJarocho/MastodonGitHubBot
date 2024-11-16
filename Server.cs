using System;
using System.IO;
using System.Threading.Tasks;

namespace BlueskyGitHubBot;

internal sealed class Server
{
    public string ServerName { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;
    public string GitHubClientId { get; set; } = string.Empty;
    public string GitHubSecret { get; set; } = string.Empty;
    public string GitHubAccessToken { get; set; } = string.Empty;
    public string GitHubUserName { get; set; } = string.Empty;
    public string GitHubRepoOrg { get; set; } = string.Empty;
    public string GitHubRepoName { get; set; } = string.Empty;
    public int GitHubLatestIssueNumber { get; set; } = 0;
    public string BlueskyUserName { get; set; } = string.Empty;
    public string BlueskyPasswordFilePath { get; set; } = string.Empty;
    public async Task<string> GetBlueskyPasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(BlueskyPasswordFilePath))
        {
            throw new NullReferenceException("No Bluesky password file path has been provided.");
        }
        else if (!File.Exists(BlueskyPasswordFilePath))
        {
            throw new FileNotFoundException($"The file containing the Bluesky password does not exist: {BlueskyPasswordFilePath}");
        }

        return await File.ReadAllTextAsync(BlueskyPasswordFilePath);
    }
}
