using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Ozdilek.PM.BuildingBlocks.Web;

/// <summary>
/// This module never issues its own service-to-service tokens (see <c>AuthOptions</c>). Instead, when
/// one service needs to call another synchronously (e.g. AIGatewayService reading project details from
/// ProjectService), it forwards the caller's own bearer token downstream. The downstream service
/// validates it exactly like any other inbound request — no separate client-credentials flow needed.
/// </summary>
public sealed class BearerTokenForwardingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var incoming = httpContextAccessor.HttpContext?.Request.Headers.Authorization ?? StringValues.Empty;
        if (!StringValues.IsNullOrEmpty(incoming))
        {
            request.Headers.TryAddWithoutValidation("Authorization", incoming.ToString());
        }

        return base.SendAsync(request, cancellationToken);
    }
}
