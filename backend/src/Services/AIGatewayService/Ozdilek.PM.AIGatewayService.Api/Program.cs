using Microsoft.EntityFrameworkCore;
using Ozdilek.PM.AIGatewayService.Infrastructure;
using Ozdilek.PM.AIGatewayService.Infrastructure.Persistence;
using Ozdilek.PM.BuildingBlocks.Auth;
using Ozdilek.PM.BuildingBlocks.Logging;
using Ozdilek.PM.BuildingBlocks.Security;
using Ozdilek.PM.BuildingBlocks.Web;

var builder = WebApplication.CreateBuilder(args);

builder.UseCwaSerilog("AIGatewayService");

builder.Services.AddCwaJsonControllers();
builder.Services.AddOpenApi();
builder.Services.AddCwaAuth(builder.Configuration);
builder.Services.AddCwaHealthChecks();
builder.Services.AddAIGatewayServiceInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AIGatewayDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<PromptSanitizationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapCwaHealthChecks();

app.Run();
