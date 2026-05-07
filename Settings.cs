using System;

namespace BlueskyGitHubBot;

internal sealed class Settings
{
    public int SleepSeconds { get; set; } = 120;
    public int RateLimitingSeconds { get; set; } = 60;
    public bool Debug { get; set; } = true;
    public Server[] Servers { get; set; } = Array.Empty<Server>();
}
