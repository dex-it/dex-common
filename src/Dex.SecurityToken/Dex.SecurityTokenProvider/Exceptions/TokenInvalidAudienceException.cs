using System;

namespace Dex.SecurityTokenProvider.Exceptions
{
    public class TokenInvalidAudienceException : Exception
    {
        public TokenInvalidAudienceException()
        {
            
        }

        public TokenInvalidAudienceException(string errorText) : base(errorText)
        {
            
        }
    }
}