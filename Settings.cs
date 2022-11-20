namespace MastodonGitHubBot
{
    internal sealed class Settings
    {
        public string AppName { get; set; } = string.Empty;
        public string GitHubClientId { get; set; } = string.Empty;
        public string GitHubSecret { get; set; } = string.Empty;
        public string GitHubAccessToken { get; set; } = string.Empty;
        public string GitHubUserName { get; set; } = string.Empty;
        public string MastodonServer { get; set; } = string.Empty;
        public string MastodonAccessToken { get; set; } = string.Empty;
    }
}
