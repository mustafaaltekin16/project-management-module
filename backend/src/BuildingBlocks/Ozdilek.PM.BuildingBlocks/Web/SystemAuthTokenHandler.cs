using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Ozdilek.PM.BuildingBlocks.Auth;

namespace Ozdilek.PM.BuildingBlocks.Web;

/// <summary>
/// Like <see cref="BearerTokenForwardingHandler"/> (forwards the caller's own bearer token when one is
/// in flight), but ALSO covers calls made with no real user request behind them at all — a MassTransit
/// consumer reacting to an event, or a background/scheduled job. In those cases there is no
/// <see cref="IHttpContextAccessor.HttpContext"/> to forward a token from, so this mints a short-lived
/// "system" token instead, so the downstream service's plain <c>[Authorize]</c> endpoints (no specific
/// role required) still accept the call. Only use this for endpoints that don't require a specific role —
/// the minted token carries none.
///
/// Dev-mode only: <see cref="JwtTokenFactory"/> signs with <see cref="AuthOptions.DevSigningKey"/>, which
/// downstream services only accept when <c>Auth:Mode=Dev</c> (see <c>CwaAuthExtensions</c>). In
/// ExternalOidc/production this would need a real client-credentials flow against the corporate identity
/// provider instead — this module doesn't have a service-account story for that yet.
/// </summary>
public sealed class SystemAuthTokenHandler(IHttpContextAccessor httpContextAccessor, AuthOptions authOptions) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var incoming = httpContextAccessor.HttpContext?.Request.Headers.Authorization ?? StringValues.Empty;
        if (!StringValues.IsNullOrEmpty(incoming))
        {
            request.Headers.TryAddWithoutValidation("Authorization", incoming.ToString());
        }
        else
        {
            var token = JwtTokenFactory.CreateToken(authOptions, "system", "Sistem", [], TimeSpan.FromMinutes(5));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
