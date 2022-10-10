using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dex.DistributedCache.Extensions;
using Dex.DistributedCache.Helpers;
using Dex.DistributedCache.Models;
using Dex.DistributedCache.Services;
using Dex.DistributedCache.Tests.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Moq;
using NUnit.Framework;

namespace Dex.DistributedCache.Tests.Tests
{
    public class DistributedCacheTests : BaseTest
    {
        private const int ExpirationInSeconds = 600;

        [Test]
        public async Task CheckExistingCacheValueReturnFalseTest()
        {
            await using var serviceProvider = InitServiceCollection()
                .AddDistributedCache()
                .AddSingleton<IDistributedCache, MemoryDistributedCache>()
                .BuildServiceProvider();

            var cacheActionFilterService = serviceProvider.GetRequiredService<ICacheActionFilterService>();
            var cacheService = serviceProvider.GetRequiredService<ICacheService>();

            var variableKeys = cacheActionFilterService.GetVariableKeys(Array.Empty<Type>());
            var paramsList = new List<string> { "paramTest" };
            var cacheKey = cacheService.GenerateCacheKey(variableKeys, paramsList);

            var actionExecutingContext = CreateDefaultActionExecutingContext();
            var checkExistingCacheValue = await cacheActionFilterService.CheckExistingCacheValue(cacheKey, actionExecutingContext);

            Assert.IsFalse(checkExistingCacheValue);
        }

        [Test]
        public async Task GetNotModifiedStatusCodeTest()
        {
            await using var serviceProvider = InitServiceCollection()
                .AddDistributedCache()
                .AddSingleton<IDistributedCache, MemoryDistributedCache>()
                .AddTransient<ICacheUserVariableKey, CacheUserVariableKeyTest>()
                .BuildServiceProvider();

            var cacheActionFilterService = serviceProvider.GetRequiredService<ICacheActionFilterService>();
            var cacheService = serviceProvider.GetRequiredService<ICacheService>();

            Type[] cacheVariableKeys = { typeof(ICacheUserVariableKey) };
            var variableKeys = cacheActionFilterService.GetVariableKeys(cacheVariableKeys);
            var paramsList = new List<string> { "paramTest" };
            var cacheKey = cacheService.GenerateCacheKey(variableKeys, paramsList);

            var cacheMetaInfo = new CacheMetaInfo("eTag", "application/json", true);
            await cacheService.SetMetaInfoAsync(cacheKey, cacheMetaInfo.GetBytes(), ExpirationInSeconds, CancellationToken.None);

            var actionExecutingContext = CreateDefaultActionExecutingContext();
            actionExecutingContext.HttpContext.Request.Headers.Add(HeaderNames.IfNoneMatch, new[] { cacheMetaInfo.ETag });

            var checkExistingCacheValue = await cacheActionFilterService.CheckExistingCacheValue(cacheKey, actionExecutingContext);

            Assert.IsTrue(checkExistingCacheValue);
            Assert.AreEqual((int)HttpStatusCode.NotModified, ((StatusCodeResult)actionExecutingContext.Result)!.StatusCode);
        }

        [Test]
        public async Task GetExistingValueFromCacheTest()
        {
            await using var serviceProvider = InitServiceCollection()
                .AddDistributedCache()
                .AddSingleton<IDistributedCache, MemoryDistributedCache>()
                .AddTransient<ICacheUserVariableKey, CacheUserVariableKeyTest>()
                .BuildServiceProvider();

            var cacheActionFilterService = serviceProvider.GetRequiredService<ICacheActionFilterService>();
            var cacheService = serviceProvider.GetRequiredService<ICacheService>();

            Type[] cacheVariableKeys = { typeof(ICacheUserVariableKey) };
            var variableKeys = cacheActionFilterService.GetVariableKeys(cacheVariableKeys);
            var paramsList = new List<string> { "paramTest" };
            var cacheKey = cacheService.GenerateCacheKey(variableKeys, paramsList);

            var cacheMetaInfo = new CacheMetaInfo("eTag", "application/json", true);
            await cacheService.SetMetaInfoAsync(cacheKey, cacheMetaInfo.GetBytes(), ExpirationInSeconds, CancellationToken.None);

            var cacheValue = "cacheValue";
            var cacheValueByte = Encoding.UTF8.GetBytes(cacheValue);
            await cacheService.SetValueDataAsync(cacheKey, cacheValueByte, ExpirationInSeconds, CancellationToken.None);

            var actionExecutingContext = CreateDefaultActionExecutingContext();
            actionExecutingContext.HttpContext.Request.Headers.Add(HeaderNames.IfNoneMatch, new[] { "eTagOld" });

            var checkExistingCacheValue = await cacheActionFilterService.CheckExistingCacheValue(cacheKey, actionExecutingContext);

            Assert.IsTrue(checkExistingCacheValue);
            Assert.AreEqual(cacheValue, Encoding.UTF8.GetString(((FileContentResult)actionExecutingContext.Result)!.FileContents));
            Assert.IsTrue(actionExecutingContext.HttpContext.Response.Headers.ContainsKey(HeaderNames.ETag));
        }

        [Test]
        public async Task TryCacheValueWithoutDependenciesTest()
        {
            var serviceCollection = InitServiceCollection()
                .AddDistributedCache()
                .AddSingleton<IDistributedCache, MemoryDistributedCache>();
            serviceCollection.AddMvc();
            await using var serviceProvider = serviceCollection.BuildServiceProvider();

            var cacheActionFilterService = serviceProvider.GetRequiredService<ICacheActionFilterService>();
            var cacheService = serviceProvider.GetRequiredService<ICacheService>();

            var variableKeys = cacheActionFilterService.GetVariableKeys(Array.Empty<Type>());
            var paramsList = new List<string> { "paramTest" };
            var cacheKey = cacheService.GenerateCacheKey(variableKeys, paramsList);

            var actionExecutedContext = CreateDefaultActionExecutedContext();

            var cacheValue = "cacheValue";
            await using var buffer = new MemoryStream(Encoding.UTF8.GetBytes(cacheValue));
            actionExecutedContext.HttpContext.Response.Body = buffer;
            actionExecutedContext.Result = new OkObjectResult(cacheValue);
            actionExecutedContext.HttpContext.RequestServices = serviceProvider;

            var isValueCached = await cacheActionFilterService.TryCacheValue(cacheKey, ExpirationInSeconds, variableKeys, actionExecutedContext);

            Assert.IsTrue(isValueCached);
        }

        private static ActionExecutingContext CreateDefaultActionExecutingContext()
        {
            var actionContext = new ActionContext(new DefaultHttpContext(), Mock.Of<RouteData>(), Mock.Of<ActionDescriptor>(), Mock.Of<ModelStateDictionary>());
            var actionExecutingContext =
                new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), Mock.Of<Controller>());

            return actionExecutingContext;
        }

        private static ActionExecutedContext CreateDefaultActionExecutedContext()
        {
            var actionContext = new ActionContext(new DefaultHttpContext(), Mock.Of<RouteData>(), Mock.Of<ActionDescriptor>(), Mock.Of<ModelStateDictionary>());
            var actionExecutedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), Mock.Of<Controller>());

            return actionExecutedContext;
        }
    }
}