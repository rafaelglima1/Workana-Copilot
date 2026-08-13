using FreelaHunter.Web.Components;
using FreelaHunter.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<FreelaHunterApiClient>(client =>
{
    var apiUrl = builder.Configuration["ApiUrl"] ?? "http://localhost:5092";
    client.BaseAddress = new Uri(apiUrl);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
