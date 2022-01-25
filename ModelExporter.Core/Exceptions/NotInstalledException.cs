using System;

namespace ModelExporter.Core.Exceptions
{
    public class NotInstalledException : Exception
    {
        public NotInstalledException(Exception innerException) : 
            base(Constants.NOT_INSTALLED_MESSAGE, innerException) { }
    }
}
