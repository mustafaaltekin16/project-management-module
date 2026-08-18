using Ozdilek.PM.BuildingBlocks.Auth;
using Ozdilek.PM.BuildingBlocks.Logging;
using Ozdilek.PM.BuildingBlocks.Web;
using Ozdilek.PM.ApiGateway;

var builder = WebApplication.CreateBuilder(args);

builder.UseCwaSerilog("ApiGateway");

var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
builder.Services.AddCwaHealthChecks();
builder.Services.AddCwaAuth(builder.Configuration);
builder.Services.AddHttpClient<DirectorySessionValidator>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:UserDirectoryService"] ?? "http://localhost:6005");
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Dev-only permissive CORS so the Angular dev server (any localhost port) can call the gateway
// directly from the browser. Bearer-token auth means no cookies are involved, so AllowAnyOrigin is
// safe here without AllowCredentials. Tighten this to specific origins before any real deployment.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseMiddleware<ActiveDirectorySessionMiddleware>();
app.UseAuthorization();

// The gateway is the single entry point clients (Angular, mobile, ...) talk to — the dev token issuer
// (local-only; see AuthOptions) is mapped here so there is exactly one place to obtain a token from.
app.MapDevTokenIssuer(authOptions);

app.MapCwaHealthChecks();
app.MapReverseProxy();

app.Run();
