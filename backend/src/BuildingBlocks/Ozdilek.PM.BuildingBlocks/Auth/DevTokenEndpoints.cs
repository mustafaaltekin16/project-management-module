using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Ozdilek.PM.BuildingBlocks.Auth;

public sealed record DevTokenRequest(string UserId, string DisplayName, string[] Roles);

/// <summary>
/// Local-only token minting for scripted/manual backend testing — never used by the real frontend
/// login flow (see AuthController.Login in UserDirectoryService for that). Only mapped when
/// <see cref="AuthOptions.Mode"/> is "Dev" and <see cref="AuthOptions.EnableDevTokenIssuer"/> is true.
/// </summary>
public static class DevTokenEndpoints
{
    public static IEndpointRouteBuilder MapDevTokenIssuer(this IEndpointRouteBuilder app, AuthOptions options)
    {
        if (options.Mode != "Dev" || !options.EnableDevTokenIssuer)
        {
            return app;
        }

        app.MapPost("/dev/token", (DevTokenRequest request) =>
        {
            var token = JwtTokenFactory.CreateToken(options, request.UserId, request.DisplayName, request.Roles, TimeSpan.FromHours(8));
            return Results.Ok(new { accessToken = token });
        }).WithName("IssueDevToken").WithTags("Dev");

        return app;
    }
}
