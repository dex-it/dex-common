namespace Dex.ResponseSigning.Options;

internal sealed class ResponseSigningOptions
{
    public string? DefaultPassword { set; get; }

    public string Algorithm { set; get; } = "RS256";
}
