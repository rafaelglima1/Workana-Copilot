using FreelaHunter.Api;
using FreelaHunter.Application;
using FreelaHunter.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok("Healthy"))
   .WithName("HealthCheck");

app.MapOpportunityEndpoints();

app.Run();

public partial class Program { }
