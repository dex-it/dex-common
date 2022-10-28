using Dex.SecurityTokenProviderTests.TestData.Models;

namespace Dex.SecurityTokenProviderTests.TestData
{
    internal static class UserTokensData
    {
        public static TestUserToken ValidUserToken =>
            new()
            {
                Audience = "TestAudience",
                Created = DateTimeOffset.UtcNow,
                Expired = DateTimeOffset.UtcNow.AddDays(1),
                UserId = Guid.NewGuid()
            };
    }
}