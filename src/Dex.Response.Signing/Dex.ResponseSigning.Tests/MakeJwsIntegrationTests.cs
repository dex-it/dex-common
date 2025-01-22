using Dex.ResponseSigning.Jws;
using Dex.ResponseSigning.Options;
using Dex.ResponseSigning.Serialization;
using Dex.ResponseSigning.Signing;
using Dex.ResponseSigning.Tests.Mocks;
using Xunit;
using Xunit.Abstractions;

namespace Dex.ResponseSigning.Tests;

public class MakeJwsIntegrationTests
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly OptionsMock<ResponseSigningOptions> _options;
    private readonly ISignDataService _signDataService;
    private readonly IVerifySignService _verifySignService;

    private readonly PrivateKeyExtractorMock _privateKeyExtractorMock = new();
    private readonly PublicKeyExtractorMock _publicKeyExtractorMock = new();

    public MakeJwsIntegrationTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
        ResponseSigningOptions optionsValue = new();
        _options = new OptionsMock<ResponseSigningOptions>(optionsValue);

        _signDataService = new SignDataService(_privateKeyExtractorMock.Object);
        _verifySignService = new VerifySignService(_publicKeyExtractorMock.Object);
    }

    [Fact]
    public async Task MakeJwsTestAsync()
    {
        var testEntity = new TestEntity { Id = 2, Name = "test entity 22", Description = "second test" };

        var makeJwsService = new JwsSignatureService(_signDataService, _options.Object,
            new SigningDataSerializationOptions(new()));

        var jws = await makeJwsService.SignDataAsync(testEntity, CancellationToken.None);

        _outputHelper.WriteLine(jws);

        var parseJwsService = new JwsParsingService(_verifySignService, new SigningDataSerializationOptions(new()));

        var parsedEntity = await parseJwsService.ParseJwsAsync<TestEntity>(jws, CancellationToken.None);

        Assert.Equal(testEntity.Id, parsedEntity.Id);
        Assert.Equal(testEntity.Name, parsedEntity.Name);
        Assert.Equal(testEntity.Description, parsedEntity.Description);

        _outputHelper.WriteLine(parsedEntity.ToString());
    }
}