using System;
using Dex.SecurityTokenProvider.Models;

namespace Dex.SecurityTokenProviderTests.TestData.Models
{
    internal class TestUserToken : BaseToken 
    {
        public TestUserToken()
        {
        }

        public Guid UserId { get; set; }
    }
}