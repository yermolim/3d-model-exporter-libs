using ModelExporter.Core.Interfaces;

using System;

namespace ModelExporter.Core.Logging
{
    public class ConsoleLogger : ILogger
    {
        private const string _errorPrefix = "Error:";
        private const string _warningPrefix = "Warning:";
        private const string _informationPrefix = "Information:";

        public void LogError(Exception ex)
        {
            Log(_errorPrefix, $"{ex.Message} ({ex.StackTrace})");
        }

        public void LogWarning(string message)
        {
            Log(_warningPrefix, message);
        }

        public void LogInformation(string message)
        {
            Log(_informationPrefix, message);
        }

        private void Log(string prefix, string message)
        {
            var text = $"\n({DateTime.Now}) {prefix} {message}";
            Console.WriteLine(text);
        }
    }
}
