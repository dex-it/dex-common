using Dex.SecurityTokenProvider.Models;

namespace Dex.SecurityTokenProviderTests.TestData.Models
{
    internal class TestUserToken : BaseToken 
    {
        public Guid UserId { get; set; }
    }
}