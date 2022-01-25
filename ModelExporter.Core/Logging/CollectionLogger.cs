using ModelExporter.Core.Interfaces;

using System;
using System.Collections.Concurrent;

namespace ModelExporter.Core.Logging
{
    public class CollectionLogger : ILogger
    {
        private const string _errorPrefix = "Error:";
        private const string _warningPrefix = "Warning:";
        private const string _informationPrefix = "Information:";

        private readonly ConcurrentStack<string> _messages = new ConcurrentStack<string>();

        public string LastMessage 
        { 
            get
            {
                var result = _messages.TryPop(out var lastMessage);
                return result ? lastMessage : null;
            }
        }

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
            _messages.Push(text);
        }
    }
}
