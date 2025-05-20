using System;
using System.IO;
using MyBuildingBlocks.Logger;

public class FileLoggerService : ILoggerService
{
    private readonly string _logDirectory;

    public FileLoggerService(string logDirectory)
    {
        _logDirectory = logDirectory;

        if (!Directory.Exists(_logDirectory))
            Directory.CreateDirectory(_logDirectory);
    }

    public void LogInfo(string message) => WriteLog("info", message);
    public void LogWarning(string message) => WriteLog("warning", message);
    public void LogError(string message) => WriteLog("error", message);
    public void LogDebug(string message) => WriteLog("debug", message);

    private void WriteLog(string logType, string message)
    {
        string filePath = Path.Combine(_logDirectory, $"{logType}.log");

        string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";

        File.AppendAllText(filePath, logEntry);
    }
}