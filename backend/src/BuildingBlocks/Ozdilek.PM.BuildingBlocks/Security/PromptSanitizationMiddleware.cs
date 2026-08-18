using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Ozdilek.PM.SharedKernel.Security;

namespace Ozdilek.PM.BuildingBlocks.Security;

/// <summary>
/// Middleware-level guard for any endpoint that eventually forwards its request body to an LLM
/// provider (see AIGatewayService). Reads the raw body, redacts KVKK-sensitive data with
/// <see cref="PiiRegexFilter"/>, logs what categories were found (never the raw match), and rewrites
/// the request body with the redacted text before the controller/model binder sees it — so sensitive
/// data never reaches the outbound prompt-building step in the first place.
/// </summary>
public sealed class PromptSanitizationMiddleware(RequestDelegate next, ILogger<PromptSanitizationMiddleware> logger)
{
    private static readonly string[] GuardedPathPrefixes = ["/api/ai-suggestions"];

    public async Task InvokeAsync(HttpContext context)
    {
        var isGuardedPath = GuardedPathPrefixes.Any(prefix =>
            context.Request.Path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));

        if (!isGuardedPath || context.Request.Method is not ("POST" or "PUT" or "PATCH"))
        {
            await next(context);
            return;
        }

        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        var matches = PiiRegexFilter.Detect(body);
        if (matches.Count > 0)
        {
            var categories = matches.Select(m => m.Category).Distinct();
            logger.LogWarning(
                "Prompt sanitization redacted {Categories} from request to {Path} before it reaches the LLM abstraction layer.",
                string.Join(",", categories), context.Request.Path);

            var redactedBody = PiiRegexFilter.Redact(body);
            var bytes = System.Text.Encoding.UTF8.GetBytes(redactedBody);
            context.Request.Body = new MemoryStream(bytes);
            context.Request.ContentLength = bytes.Length;
        }

        await next(context);
    }
}
