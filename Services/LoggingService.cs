using System.IO;

namespace Deskplorer.Services
{
    public class LoggingService
    {
        private static readonly object _lock = new();
        private static string? _logFilePath;
        
        public static void Initialize(string logDirectory)
        {
         var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
         var deskplorerLogDir = string.IsNullOrWhiteSpace(logDirectory)
            ? Path.Combine(appDataPath, "Deskplorer", "logs")
            : logDirectory;
         Directory.CreateDirectory(deskplorerLogDir);
         _logFilePath = Path.Combine(deskplorerLogDir, $"deskplorer_{DateTime.Now:yyyyMMdd}.log");
        }
        
        public static void Log(string message)
        {
            if (string.IsNullOrWhiteSpace(_logFilePath))
                return;
                
            try
            {
                lock (_lock)
                {
                    File.AppendAllText(_logFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
                }
            }
            catch
            {
                // Silent failure - logging shouldn't break the application
            }
        }
        
        public static void LogException(Exception ex, string context = "")
        {
            var message = $"EXCEPTION in {context}: {ex.Message}";
            if (!string.IsNullOrEmpty(context))
            {
                message += $"\nContext: {context}";
            }
            message += $"\nStack Trace:\n{ex.StackTrace}";
            
            Log(message);
        }
    }
}