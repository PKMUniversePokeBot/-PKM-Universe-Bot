// PKM Universe Bot - Logger
// Written by PKM Universe - 2025

namespace PKMUniverse.Core.Logging;

public static class Logger
{
    public static event Action<string, string, LogLevel>? OnLog;

    public static void Info(string source, string message)
    {
        Log(source, message, LogLevel.Info);
    }

    public static void Error(string source, string message)
    {
        Log(source, message, LogLevel.Error);
    }

    public static void Warning(string source, string message)
    {
        Log(source, message, LogLevel.Warning);
    }

    public static void Trade(string source, string message)
    {
        Log(source, message, LogLevel.Trade);
    }

    public static void Debug(string source, string message)
    {
        Log(source, message, LogLevel.Debug);
    }

    private static void Log(string source, string message, LogLevel level)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var formatted = $"[{timestamp}] [{level}] [{source}] {message}";
        Console.WriteLine(formatted);
        OnLog?.Invoke(source, message, level);
    }
}

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
    Trade
}
