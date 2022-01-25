using System;

namespace ModelExporter.Core.Exceptions
{
    public class NotEnoughSpaceException : Exception
    {
        public NotEnoughSpaceException(Exception innerException) :
            base(Constants.NOT_ENOUGH_SPACE_MESSAGE, innerException)
        { }
    }
}
