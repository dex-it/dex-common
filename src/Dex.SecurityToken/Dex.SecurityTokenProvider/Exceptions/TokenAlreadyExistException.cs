namespace Dex.SecurityTokenProvider.Exceptions;

public class TokenAlreadyExistException : Exception
{
    public TokenAlreadyExistException()
    {
            
    }

    public TokenAlreadyExistException(string errorText): base(errorText)
    {
            
    }
}