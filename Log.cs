using System;

namespace MastodonGitHubBot;

internal partial class Log : IDisposable
{
    // Intermediate enum to avoid exposing System.Diagnostics.EventLogEntryType to non-Windows platforms.
    internal enum LogType
    {
        Error = 1,
        Warning = 2,
        Information = 4,
        Success = 8,
        Failure = 16,
    }

    internal void WriteInfo(string message) => WriteInternal(message, LogType.Information);
    internal void WriteWarning(string message) => WriteInternal(message, LogType.Warning);
    internal void WriteError(string message) => WriteInternal(message, LogType.Error);
    internal void WriteFailure(string message) => WriteInternal(message, LogType.Failure);
    internal void WriteSuccess(string message) => WriteInternal(message, LogType.Success);

    private static void ConsoleWrite(string message, LogType type)
    {
        ConsoleColor originalColor = Console.ForegroundColor;
        ConsoleColor newColor = type switch
        {
            LogType.Information => ConsoleColor.White,
            LogType.Warning => ConsoleColor.Yellow,
            LogType.Error => ConsoleColor.Red,
            LogType.Success => ConsoleColor.Green,
            LogType.Failure => ConsoleColor.DarkRed,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
        Console.ForegroundColor = newColor;
        Console.WriteLine(message);
        Console.ForegroundColor = originalColor;
    }

    private static void Validate(string message, LogType type)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }
    }
}
