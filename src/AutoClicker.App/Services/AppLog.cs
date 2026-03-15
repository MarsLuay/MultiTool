using System.IO;
using System.Text;

namespace AutoClicker.App.Services;

public static class AppLog
{
    private static readonly object SyncRoot = new();

    public static string LogsDirectoryPath => Path.Combine(AppContext.BaseDirectory, "Logs");

    public static void Info(string message) => Write("INFO", message);

    public static void Error(string message) => Write("ERROR", message);

    public static void Error(Exception exception, string message)
    {
        var builder = new StringBuilder();
        builder.AppendLine(message);
        builder.AppendLine(exception.ToString());
        Write("ERROR", builder.ToString().TrimEnd());
    }

    private static void Write(string level, string message)
    {
        lock (SyncRoot)
        {
            Directory.CreateDirectory(LogsDirectoryPath);

            var filePath = Path.Combine(LogsDirectoryPath, $"MultiTool-{DateTime.Now:yyyyMMdd}.log");
            var lines = new[]
            {
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {level}",
                message,
                string.Empty,
            };

            File.AppendAllLines(filePath, lines);
        }
    }
}
