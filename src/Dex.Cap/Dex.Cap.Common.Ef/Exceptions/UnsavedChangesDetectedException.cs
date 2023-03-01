using System;

namespace Dex.Cap.Common.Ef.Exceptions
{
    public class UnsavedChangesDetectedException : Exception
    {
        public UnsavedChangesDetectedException()
        {
        }

        public UnsavedChangesDetectedException(string message) : base(message)
        {
        }

        public UnsavedChangesDetectedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}