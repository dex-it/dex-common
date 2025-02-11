using Microsoft.Extensions.Options;
using Moq;
using MSOptions = Microsoft.Extensions.Options.Options;

namespace Dex.ResponseSigning.Tests.Mocks;

internal class OptionsMock<T> : Mock<IOptions<T>>
    where T : class
{
    private readonly T _value;

    public OptionsMock(T value)
    {
        _value = value;
    }

    public override IOptions<T> Object => MSOptions.Create(_value);
}
