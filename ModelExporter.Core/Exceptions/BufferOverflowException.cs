using System;

namespace ModelExporter.Core.Exceptions
{
    public class BufferOverflowException : Exception
    {
        public BufferOverflowException(Exception innerException) :
            base(Constants.BUFFER_OVERFLOW_MESSAGE, innerException)
        { }
    }
}
