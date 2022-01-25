using System;

namespace ModelExporter.Core.Interfaces
{
    public interface ILogger
    {
        void LogError(Exception ex);

        void LogWarning(string message);

        void LogInformation(string message);
    }
}
