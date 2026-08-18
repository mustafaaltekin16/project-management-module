using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Ozdilek.PM.Contracts.Web;
using Ozdilek.PM.SharedKernel.Exceptions;

namespace Ozdilek.PM.BuildingBlocks.Web;

/// <summary>
/// Central proxy/error handling for a service: turns exceptions into the shared <see cref="ApiResponse{T}"/>
/// envelope with the right HTTP status, instead of leaking stack traces to callers behind the gateway.
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    // camelCase to match the System.Text.Json output controllers produce (see AddCwaJsonControllers) —
    // otherwise a successful response is {"success":true,...} but an error response is {"Success":false,...}.
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var statusCode = ex switch
            {
                NotFoundException => HttpStatusCode.NotFound,
                AuthenticationFailedException => HttpStatusCode.Unauthorized,
                DomainException => HttpStatusCode.BadRequest,
                UnauthorizedAccessException => HttpStatusCode.Forbidden,
                _ => HttpStatusCode.InternalServerError
            };

            if (statusCode == HttpStatusCode.InternalServerError)
            {
                logger.LogError(ex, "Unhandled exception while processing {Path}", context.Request.Path);
            }
            else
            {
                logger.LogWarning(ex, "Handled exception ({StatusCode}) while processing {Path}", statusCode, context.Request.Path);
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var payload = ApiResponse<object>.Fail(statusCode == HttpStatusCode.InternalServerError
                ? "Beklenmeyen bir hata oluştu."
                : ex.Message);

            await context.Response.WriteAsync(JsonConvert.SerializeObject(payload, JsonSettings));
        }
    }
}
