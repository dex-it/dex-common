using System;

namespace Dex.SecurityTokenProvider.Exceptions
{
    public class InvalidAudienceException : Exception
    {
        public InvalidAudienceException()
        {
            
        }

        public InvalidAudienceException(string errorText) : base(errorText)
        {
            
        }
    }
}