using System.Security.Cryptography;
using Dex.ResponseSigning.KeyExtractor;
using Moq;

namespace Dex.ResponseSigning.Tests.Mocks;

internal class PublicKeyExtractorMock : Mock<IPublicKeyExtractor>
{
    private const string PublicKey =
        "-----BEGIN RSA PUBLIC KEY-----\nMIICCgKCAgEAvL/hvAGJH9sRNGbR2lqk2Y70UMM4hfeA+SC5dHxOADeKYbwKANpDxbjYsRYGVVJaAz+cqVCEYQF59LtQKy6+Ux0qeavE42zlE1ewRZnQnT00ZS5585p3fW24pizyCC2Q/ddt0qGa0FPMrGzqo/8OGbIbScwYgI/UAALzb4fbSGcVNVxw0MtUXhncrML16fS7j9SXOYwNjoI2MmzossVsP8LGOQUF9irxEbjAFaLwciWSV/aCs39J65SdhDyv53eARRYzXtdsAXQ1hvzot2QVl4ScSENJWFyEIfWOIj2MIw4GjmRO4ufQfhM54mMGruGPBhy6UOPEyyvRzWdvAlVV4VHh6lyZhzXNMgw/sCAFwex9kf/X3HDehiC+AXHM9TvWljm6ngiZardtO68BuFssuRbotNxyb8K/+D2zU7iF5P4lwapZHiFMrCTSuHXvEhvXBE2vY+h+InrQdiuEt4JEptwvrfACdW6X6UmhwsKdQh5EHD4wE4EL6BL+VL8ddp8q2rs0d5rQyK3NlYPfOkQAXmc/rl71NKgebL8SE4XdtbIn9Bn006/a22DzEh7e0vdwJof1DD2m0Apz/Ue3sJjrz0XlktNw+kTq09NyN1sz/T6edHCAnK7gkPzKYM10SarMasFqwGVfSSehhNzsuJPdsogpDMsY2zP6l59/e7A5KkUCAwEAAQ==\n-----END RSA PUBLIC KEY-----";

    public PublicKeyExtractorMock()
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(PublicKey.ToCharArray());

        Setup(x => x.GetKeyAsync(It.IsAny<CancellationToken>())).ReturnsAsync(rsa);

        Setup(x => x.GetKey()).Returns(rsa);
    }
}