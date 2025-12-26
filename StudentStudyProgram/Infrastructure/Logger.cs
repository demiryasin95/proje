using System;
using System.IO;
using System.Web;

namespace StudentStudyProgram.Infrastructure
{
    /// <summary>
    /// Simple file-based logging service for application errors and information
    /// </summary>
    public static class Logger
    {
        private static readonly string LogDirectory = HttpContext.Current?.Server.MapPath("~/App_Data/Logs") ?? "Logs";
        private static readonly object _lockObj = new object();

        static Logger()
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }
            }
            catch
            {
                // If we can't create log directory, logging will fail silently
            }
        }

        public static void LogError(Exception ex, string message = null)
        {
            try
            {
                var logMessage = $"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
                if (!string.IsNullOrEmpty(message))
                {
                    logMessage += $"Message: {message}\n";
                }
                logMessage += $"Exception: {ex?.GetType().Name}\n";
                logMessage += $"Message: {ex?.Message}\n";
                logMessage += $"StackTrace: {ex?.StackTrace}\n";
                if (ex?.InnerException != null)
                {
                    logMessage += $"Inner Exception: {ex.InnerException.Message}\n";
                }
                logMessage += new string('-', 80) + "\n\n";

                WriteToFile(logMessage, "error");
            }
            catch
            {
                // Never throw from logger
            }
        }

        public static void LogInfo(string message)
        {
            try
            {
                var logMessage = $"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n";
                WriteToFile(logMessage, "info");
            }
            catch
            {
                // Never throw from logger
            }
        }

        public static void LogWarning(string message)
        {
            try
            {
                var logMessage = $"[WARNING] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n";
                WriteToFile(logMessage, "warning");
            }
            catch
            {
                // Never throw from logger
            }
        }

        private static void WriteToFile(string message, string level)
        {
            var fileName = $"{level}_{DateTime.Now:yyyy-MM-dd}.log";
            var filePath = Path.Combine(LogDirectory, fileName);

            lock (_lockObj)
            {
                File.AppendAllText(filePath, message);
            }
        }
    }
}
