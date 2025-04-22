using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu.Repository.Common
{
    public class FileLoggerService
    {
        private static readonly object _lock = new object();
        private readonly string _logFilePath;

        public FileLoggerService()
        {
            // Create ErrorLogs directory if it doesn't exist
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string errorLogsDirectory = Path.Combine(baseDirectory, "ErrorLogs");

            if (!Directory.Exists(errorLogsDirectory))
            {
                Directory.CreateDirectory(errorLogsDirectory);
            }

            _logFilePath = Path.Combine(errorLogsDirectory, "LogFile.txt");
        }

        public void LogInformation(string category, string message)
        {
            WriteLog(LogLevel.Information, category, message, null);
        }

        public void LogWarning(string category, string message)
        {
            WriteLog(LogLevel.Warning, category, message, null);
        }

        public void LogError(string category, string message, Exception ex = null)
        {
            WriteLog(LogLevel.Error, category, message, ex);
        }

        private void WriteLog(LogLevel logLevel, string category, string message, Exception exception)
        {
            try
            {
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel}] [{category}] {message}";

                if (exception != null)
                {
                    logMessage += $"{Environment.NewLine}Exception: {exception.Message}";
                    logMessage += $"{Environment.NewLine}StackTrace: {exception.StackTrace}";

                    if (exception.InnerException != null)
                    {
                        logMessage += $"{Environment.NewLine}Inner Exception: {exception.InnerException.Message}";
                        logMessage += $"{Environment.NewLine}Inner StackTrace: {exception.InnerException.StackTrace}";
                    }
                }
                logMessage += $"{Environment.NewLine}";
                logMessage += $"{Environment.NewLine}";
                logMessage += $"{Environment.NewLine}";
                logMessage += $"{Environment.NewLine}";
                logMessage += $"{Environment.NewLine}";

                // Use lock to prevent multiple threads from writing to the file simultaneously
                lock (_lock)
                {
                    // Append to the log file
                    File.AppendAllText(_logFilePath, logMessage + Environment.NewLine + Environment.NewLine);
                }
            }
            catch
            {
                // Fail silently if logging itself throws an exception
            }
        }
    }
}