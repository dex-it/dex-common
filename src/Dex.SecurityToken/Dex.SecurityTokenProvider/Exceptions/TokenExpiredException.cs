using System;

namespace Dex.SecurityTokenProvider.Exceptions
{
    public class TokenExpiredException : Exception
    {
        public TokenExpiredException()
        {
            
        }

        public TokenExpiredException(string errorText) : base(errorText)
        {
            
        }
    }
}