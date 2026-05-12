using events.Di;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

//var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var configuredPort)
//    ? configuredPort
//    : 8082;
//builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(port));

builder.Services.AddEventContracts(builder.Configuration);

// Add services to the container.
builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy("OK"));
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = r => r.Name == "self"
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
