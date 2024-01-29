using System;

namespace MastodonGitHubBot;

internal sealed class Settings
{
    public int SleepSeconds { get; set; } = 120;
    public bool Debug { get; set; } = true;
    public Server[] Servers { get; set; } = Array.Empty<Server>();
}
