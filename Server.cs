using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MastodonGitHubBot;

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
    public string MastodonServer { get; set; } = string.Empty;
    public string MastodonAccessToken { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Mastonet.Visibility Visibility { get; set; } = Mastonet.Visibility.Unlisted;
}
