using System;

namespace MastodonGitHubBot;

internal partial class Log : IDisposable
{
    public Log()
    {
    }

    public void Dispose()
    {
    }

    private void WriteInternal(string message, LogType type)
    {
        Validate(message, type);
        ConsoleWrite(message, type);
    }
}
