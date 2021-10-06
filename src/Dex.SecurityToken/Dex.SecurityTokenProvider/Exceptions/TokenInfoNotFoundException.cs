using System;

namespace Dex.SecurityTokenProvider.Exceptions
{
    public class TokenInfoNotFoundException : Exception

    {
        public TokenInfoNotFoundException()
        {
            
        }

        public TokenInfoNotFoundException(string errorText) : base(errorText)
        {
            
        }
        
    }
}