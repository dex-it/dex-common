using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dex.SecurityTokenProvider.Exceptions;
using Dex.SecurityTokenProvider.Extentions;
using Dex.SecurityTokenProvider.Interfaces;
using Dex.SecurityTokenProvider.Options;
using Dex.SecurityTokenProviderTests.TestData;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using static System.DateTimeOffset;

namespace Dex.SecurityTokenProviderTests
{
    public class TokenProviderTests
    {
        private readonly IServiceProvider _serviceProvider;

        public TokenProviderTests()
        {
            _serviceProvider = BuildServiceProvider();
        }

        [Fact]
        public async Task TokenProviderSmokeTest()
        {
            //Arrange
            var tokenProvider = _serviceProvider.GetRequiredService<ITokenProvider>();
            var userToken = UserTokensData.ValidUserToken;

            userToken.Expired = Now.AddSeconds(10);

            //Act
            var token = await tokenProvider.CreateTokenUrlEscapedAsync(userToken);
            var tokenData = await tokenProvider.GetTokenDataAsync<TestUserToken>(token);

            //Assert
            Assert.Equal(tokenData.UserId, userToken.UserId);
            Assert.Equal(tokenData.Audience, userToken.Audience);
            Assert.Equal(tokenData.Created, userToken.Created);
            Assert.Equal(tokenData.Expired, userToken.Expired);
        }

        [Fact]
        public async Task TokenExpiredTest()
        {
            //Arrange
            var tokenProvider = _serviceProvider.GetRequiredService<ITokenProvider>();
            var userToken = UserTokensData.ValidUserToken;
            userToken.Expired = Now.AddSeconds(1);

            //Act
            var token = await tokenProvider.CreateTokenUrlEscapedAsync(userToken);

            await Task.Delay(TimeSpan.FromSeconds(2));
            //Assert
            await Assert.ThrowsAsync<TokenExpiredException>(async () => { await tokenProvider.GetTokenDataAsync<TestUserToken>(token); });
        }


        [Fact]
        public async Task InvalidAudienceTest()
        {
            //Arrange
            var tokenProvider = _serviceProvider.GetRequiredService<ITokenProvider>();
            var userToken = UserTokensData.ValidUserToken;
            userToken.Audience = "Invalid Audience" + Guid.NewGuid();
            //Act
            var token = await tokenProvider.CreateTokenUrlEscapedAsync(userToken);

            await Task.Delay(TimeSpan.FromSeconds(2));
            //Assert
            await Assert.ThrowsAsync<InvalidAudienceException>(async () => { await tokenProvider.GetTokenDataAsync<TestUserToken>(token); });
        }


        [Fact]
        public async Task TokenAlreadyActivatedTest()
        {
            //Arrange
            var tokenProvider = _serviceProvider.GetRequiredService<ITokenProvider>();
            var userToken = UserTokensData.ValidUserToken;

            //Act
            var token = await tokenProvider.CreateTokenUrlEscapedAsync(userToken);

            await tokenProvider.GetTokenDataAsync<TestUserToken>(token);

            //Assert
            await Assert.ThrowsAsync<TokenAlreadyActivatedException>(async () => { await tokenProvider.GetTokenDataAsync<TestUserToken>(token); });
        }


        private ServiceProvider BuildServiceProvider()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        { $"{nameof(TokenProviderOptions)}:{nameof(TokenProviderOptions.ApiResource)}", "TestAudience" },
                        { $"{nameof(TokenProviderOptions)}:{nameof(TokenProviderOptions.ApplicationName)}", "ApplicationName" }
                    }).Build();


            var services = new ServiceCollection();

            services.AddLogging(o => o.AddConsole().SetMinimumLevel(LogLevel.Debug))
                .AddDbContext<DataProtectionKeyContext>(o =>
                {
                    o.UseInMemoryDatabase("DataProtection_EntityFrameworkCore");
                    o.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                    o.EnableSensitiveDataLogging();
                })
                .AddDataProtection()
                .PersistKeysToDbContext<DataProtectionKeyContext>();

            services.AddDexSecurityTokenProvider<TestTokenInfoStorage>(config.GetSection(nameof(TokenProviderOptions)));
            return services.BuildServiceProvider();
        }
    }
}