using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Ozdilek.PM.ApiGateway;

internal enum DirectorySessionState
{
    Active,
    Inactive,
    Unavailable
}

internal sealed class DirectorySessionValidator(HttpClient httpClient)
{
    public async Task<DirectorySessionState> ValidateAsync(
        string employeeId,
        string bearerToken,
        CancellationToken ct)
    {
        if (!Guid.TryParse(employeeId, out var id))
        {
            return DirectorySessionState.Inactive;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/employees/{id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        using var response = await httpClient.SendAsync(request, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return DirectorySessionState.Inactive;
        }
        if (!response.IsSuccessStatusCode)
        {
            return DirectorySessionState.Unavailable;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (!document.RootElement.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("isActive", out var isActive))
        {
            return DirectorySessionState.Unavailable;
        }

        return isActive.GetBoolean()
            ? DirectorySessionState.Active
            : DirectorySessionState.Inactive;
    }
}

internal sealed class ActiveDirectorySessionMiddleware(
    RequestDelegate next,
    ILogger<ActiveDirectorySessionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, DirectorySessionValidator validator)
    {
        if (context.Request.Path.StartsWithSegments("/api/auth") ||
            context.Request.Path.StartsWithSegments("/dev/token") ||
            context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var employeeId = context.User.FindFirst("sub")?.Value;
        var authorization = context.Request.Headers.Authorization.ToString();
        var bearerToken = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization["Bearer ".Length..].Trim()
            : string.Empty;

        DirectorySessionState state;
        try
        {
            state = await validator.ValidateAsync(
                employeeId ?? string.Empty,
                bearerToken,
                context.RequestAborted);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            logger.LogError(ex, "Employee session could not be validated for {EmployeeId}", employeeId);
            state = DirectorySessionState.Unavailable;
        }

        if (state == DirectorySessionState.Active)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = state == DirectorySessionState.Inactive
            ? StatusCodes.Status401Unauthorized
            : StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "application/json";
        var error = state == DirectorySessionState.Inactive
            ? "Oturum sahibi artık aktif değil. Lütfen yeniden giriş yapın."
            : "Oturum doğrulama servisine şu anda ulaşılamıyor.";
        await context.Response.WriteAsJsonAsync(new
        {
            success = false,
            data = (object?)null,
            errors = new[] { error }
        });
    }
}
