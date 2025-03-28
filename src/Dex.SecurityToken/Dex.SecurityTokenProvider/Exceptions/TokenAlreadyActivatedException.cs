using System;

namespace Dex.SecurityTokenProvider.Exceptions
{
    public class TokenAlreadyActivatedException : Exception
    {
        public TokenAlreadyActivatedException()
        {
            
        }

        public TokenAlreadyActivatedException(string errorText): base(errorText)
        {
            
        }
    }
}