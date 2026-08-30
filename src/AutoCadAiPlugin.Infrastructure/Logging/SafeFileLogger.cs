using System;
using System.IO;
using System.Text.RegularExpressions;
using AutoCadAiPlugin.Core.Interfaces;

namespace AutoCadAiPlugin.Infrastructure.Logging;

public class SafeFileLogger : ILoggerService
{
    private readonly string _logDirectory;
    private readonly object _fileLock = new();
    private static readonly Regex ApiKeyPattern = new(@"([sS]k-[a-zA-Z0-9_-]{10,}|AIza[0-9A-Za-z-_]{35}|Bearer\s+[a-zA-Z0-9._-]+)", RegexOptions.Compiled);

    public SafeFileLogger(string? customLogDir = null)
    {
        if (!string.IsNullOrWhiteSpace(customLogDir))
        {
            _logDirectory = customLogDir;
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _logDirectory = Path.Combine(appData, "Ark4Studio", "Autocat", "logs");
        }

        try
        {
            Directory.CreateDirectory(_logDirectory);
        }
        catch
        {
            // Ignored
        }
    }

    public void LogInfo(string message) => WriteLog("INFO", message);

    public void LogWarning(string message) => WriteLog("WARN", message);

    public void LogError(string message, Exception? ex = null)
    {
        string fullMessage = ex != null ? $"{message} | Exception: {ex.Message} | StackTrace: {ex.StackTrace}" : message;
        WriteLog("ERROR", fullMessage);
    }

    public void LogToolExecution(string toolName, string argumentsJson, bool success, string? resultOrError, long elapsedMs)
    {
        string status = success ? "SUCCESS" : "FAILED";
        string logMsg = $"Tool: {toolName} | Status: {status} | Elapsed: {elapsedMs}ms | Args: {Sanitize(argumentsJson)} | Output: {Sanitize(resultOrError ?? string.Empty)}";
        WriteLog("TOOL", logMsg);
    }

    private void WriteLog(string level, string message)
    {
        try
        {
            string sanitized = Sanitize(message);
            string line = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {sanitized}";
            string logFileName = $"autocat_{DateTime.UtcNow:yyyyMMdd}.log";
            string logFilePath = Path.Combine(_logDirectory, logFileName);

            lock (_fileLock)
            {
                File.AppendAllText(logFilePath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Logging never crashes the host
        }
    }

    private static string Sanitize(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return ApiKeyPattern.Replace(input, "[REDACTED_SECRET]");
    }
}
