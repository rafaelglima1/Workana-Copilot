using FreelaHunter.Api;
using FreelaHunter.Application;
using FreelaHunter.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

app.MapGet("/health", () => Results.Ok("Healthy"))
   .WithName("HealthCheck");

app.MapOpportunityEndpoints();
app.MapFreelancerProfileEndpoints();

app.Run();

public partial class Program { }
