using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Ozdilek.PM.BuildingBlocks.Auth;

/// <summary>
/// Wires up JWT bearer validation for a service. This is intentionally a "validate only" setup —
/// see <see cref="AuthOptions"/> for why. Every service in the module calls this the same way so
/// authorization behaves identically regardless of which microservice handles the request.
/// </summary>
public static class CwaAuthExtensions
{
    public static IServiceCollection AddCwaAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
        services.AddSingleton(options);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(bearer =>
            {
                // Without this, JwtBearer's legacy inbound-claim-type mapping rewrites short claim
                // names ("role", "sub") to long XML-schema URIs, silently breaking RoleClaimType="role"
                // and NameClaimType="sub" below (RequireRole would never match anything).
                bearer.MapInboundClaims = false;

                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = options.Mode == "ExternalOidc",
                    ValidateAudience = true,
                    ValidAudience = options.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    RoleClaimType = "role",
                    NameClaimType = "sub"
                };

                if (options.Mode == "ExternalOidc" && !string.IsNullOrWhiteSpace(options.Authority))
                {
                    // Real deployment: authority is the corporate OIDC provider; signing keys come from its JWKS endpoint.
                    bearer.Authority = options.Authority;
                    bearer.RequireHttpsMetadata = true;
                }
                else
                {
                    // Dev/local: validate against a shared symmetric key instead of an external authority.
                    bearer.TokenValidationParameters.IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.DevSigningKey));
                    bearer.RequireHttpsMetadata = false;
                }
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.CanManageProjects, policy => policy.RequireRole(Roles.Admin, Roles.ProjectManager))
            .AddPolicy(Policies.CanManageDirectory, policy => policy.RequireRole(Roles.Admin))
            .AddPolicy(Policies.CanApprove, policy => policy.RequireRole(Roles.Admin, Roles.Approver, Roles.ProjectManager))
            .AddPolicy(Policies.CanDeleteProjects, policy => policy.RequireRole(Roles.Admin));

        return services;
    }
}
