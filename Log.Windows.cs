using System;
using System.Diagnostics;

namespace MastodonGitHubBot;

internal partial class Log : IDisposable
{
    private const string LogName = "MastodonGitHubBot";
    private const string LogSource = LogName;
    private const string LogMachineName = ".";

    private readonly EventLog _eventLog;

    public Log()
    {
        // Throws if non-elevated due to attempt to read "Security" source.
        if (!EventLog.SourceExists(LogSource, LogMachineName))
        {
            EventLog.CreateEventSource(new EventSourceCreationData(LogSource, LogName) { MachineName = LogMachineName });
        }
        _eventLog = new EventLog(LogName, LogMachineName, LogSource);
    }

    public void Dispose() => _eventLog.Dispose();

    private void WriteInternal(string message, LogType type)
    {
        Validate(message, type);
        ConsoleWrite(message, type);
        // Throws if non-elevated due to attempt to read "Security" source.
        _eventLog.WriteEntry(message, (EventLogEntryType)type);
    }
}
