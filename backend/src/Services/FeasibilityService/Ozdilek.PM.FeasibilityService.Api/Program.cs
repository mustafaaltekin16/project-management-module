using Microsoft.EntityFrameworkCore;
using Ozdilek.PM.BuildingBlocks.Auth;
using Ozdilek.PM.BuildingBlocks.Logging;
using Ozdilek.PM.BuildingBlocks.Web;
using Ozdilek.PM.FeasibilityService.Infrastructure;
using Ozdilek.PM.FeasibilityService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.UseCwaSerilog("FeasibilityService");

builder.Services.AddCwaJsonControllers();
builder.Services.AddOpenApi();
builder.Services.AddCwaAuth(builder.Configuration);
builder.Services.AddCwaHealthChecks();
builder.Services.AddFeasibilityServiceInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FeasibilityDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapCwaHealthChecks();

app.Run();
