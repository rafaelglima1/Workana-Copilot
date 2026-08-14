using FreelaHunter.Api;
using FreelaHunter.Application;
using FreelaHunter.Infrastructure;
using FreelaHunter.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddHealthChecks();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
    await initializer.InitializeAsync(CancellationToken.None);
}

app.MapGet("/health", () => Results.Ok("Healthy"))
   .WithName("HealthCheck");

app.MapHealthChecks("/health/ready").WithName("HealthCheckReady");

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.MapOpportunityEndpoints();
app.MapOpportunityInboxEndpoints();
app.MapFreelancerProfileEndpoints();
app.MapProposalStrategyEndpoints();

app.Run();

public partial class Program { }
