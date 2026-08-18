using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Ozdilek.PM.BuildingBlocks.Web;

public static class JsonExtensions
{
    /// <summary>
    /// MVC controllers with enums serialized as their string names rather than raw integers. Every
    /// service uses this so a cross-service HTTP call (e.g. AIGatewayService reading ProjectService's
    /// `type` field) doesn't have to guess at numeric enum ordinals.
    /// </summary>
    public static IMvcBuilder AddCwaJsonControllers(this IServiceCollection services) =>
        services.AddControllers().AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
}
