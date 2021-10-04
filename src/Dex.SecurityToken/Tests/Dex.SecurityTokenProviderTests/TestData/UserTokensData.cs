﻿using System;

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
                Id = Guid.NewGuid(),
                Reason = "testReason",
                UserId = Guid.NewGuid()
            };
    }
}