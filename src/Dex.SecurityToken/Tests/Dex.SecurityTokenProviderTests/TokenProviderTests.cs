using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dex.SecurityTokenProvider.Exceptions;
using Dex.SecurityTokenProvider.Extensions;
using Dex.SecurityTokenProvider.Interfaces;
using Dex.SecurityTokenProvider.Options;
using Dex.SecurityTokenProviderTests.TestData;
using Dex.SecurityTokenProviderTests.TestData.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

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

            //Act
            var token = await tokenProvider.CreateTokenAsUrlAsync<TestUserToken>(testUserToken => { testUserToken.UserId = userToken.UserId; },
                TimeSpan.FromSeconds(50));

            var tokenData = await tokenProvider.GetTokenDataAsync<TestUserToken>(token);

            //Assert
            Assert.Equal(tokenData.UserId, userToken.UserId);
            Assert.Equal(tokenData.Audience, userToken.Audience);
        }


        [Fact]
        public async Task TokenExpiredTest()
        {
            //Arrange
            var tokenProvider = _serviceProvider.GetRequiredService<ITokenProvider>();
            var userToken = UserTokensData.ValidUserToken;

            //Act
            var token = await tokenProvider.CreateTokenAsUrlAsync<TestUserToken>(testUserToken => { testUserToken.UserId = userToken.UserId; },
                TimeSpan.FromSeconds(1));

            await Task.Delay(TimeSpan.FromSeconds(2));
            //Assert
            await Assert.ThrowsAsync<TokenExpiredException>(async () => { await tokenProvider.GetTokenDataAsync<TestUserToken>(token); });
        }

        
        [Fact]
        public void NegativeConcurrencyTest()
        {
            //Arrange
            var dataProtectionProvider = _serviceProvider.GetRequiredService<IDataProtectionProvider>();
            var protector1 = dataProtectionProvider.CreateProtector("Protector");
            var protector2 = dataProtectionProvider.CreateProtector("Protector");

            var testString = "testString";
            
            //Act
            var encryptedToken1 = protector1.Protect(testString);
            var encryptedToken2 = protector2.Protect(testString);
            
                            
            var decryptedToken1 = protector1.Unprotect(encryptedToken2);
            var decryptedToken2 = protector2.Unprotect(encryptedToken1);
            
            //Arrange
            Assert.NotEqual(encryptedToken1, encryptedToken2);
            Assert.Equal(decryptedToken1, decryptedToken2);
        }
        
        [Fact]
        public async Task TokenAlreadyActivatedTest()
        {
            //Arrange
            var tokenProvider = _serviceProvider.GetRequiredService<ITokenProvider>();
            var userToken = UserTokensData.ValidUserToken;

            //Act
            var token = await tokenProvider.CreateTokenAsUrlAsync<TestUserToken>(testUserToken => { testUserToken.UserId = userToken.UserId; },
                TimeSpan.FromSeconds(10));
            
            var tokenData = await tokenProvider.GetTokenDataFromUrlAsync<TestUserToken>(token);
            await tokenProvider.MarkTokenAsUsed(tokenData.Id);

            //Assert
            await Assert.ThrowsAsync<TokenAlreadyActivatedException>(async () => { await tokenProvider.GetTokenDataFromUrlAsync<TestUserToken>(token); });
        }


        private static ServiceProvider BuildServiceProvider()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        { $"{nameof(TokenProviderOptions)}:{nameof(TokenProviderOptions.ApiResource)}", "TestAudience" }
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
                .PersistKeysToDbContext<DataProtectionKeyContext>()
                .SetApplicationName("TestApp");

            services.AddSecurityTokenProvider<TestTokenInfoStorage>(config.GetSection(nameof(TokenProviderOptions)));
            return services.BuildServiceProvider();
        }
    }
}