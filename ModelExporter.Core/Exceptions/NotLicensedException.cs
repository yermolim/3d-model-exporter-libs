using System;

namespace ModelExporter.Core.Exceptions
{
    public class NotLicensedException : Exception
    {
        public NotLicensedException(Exception innerException) :
            base(Constants.NOT_LICENSED_MESSAGE, innerException)
        { }
    }
}