using System;
using Dex.SecurityTokenProvider.Models;

namespace Dex.SecurityTokenProviderTests
{
    internal class TestUserToken : BaseToken 
    {
        public Guid UserId { get; set; }
    }
}