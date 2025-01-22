using Dex.ResponseSigning.Jws;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.ResponseSigning.Filters;

/// <summary>
/// Фильтр для подписи ответа сервиса
/// </summary>
public sealed class SignResponseFilterAttribute : Attribute, IAsyncResultFilter
{
    private const int SuccessfullStatusCode = 200;

    /// <inheritdoc/>
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var makeJwsService =
            context.HttpContext.RequestServices.GetRequiredService<IJwsSignatureService>();

        if (context.Result is ObjectResult { StatusCode: SuccessfullStatusCode } objectResult)
        {
            if (objectResult.Value is null)
            {
                throw new InvalidOperationException("Cannot sign empty payload.");
            }

            objectResult.Value =
                await makeJwsService.SignDataAsync(objectResult.Value, context.HttpContext.RequestAborted);
        }

        await next();
    }
}