using Blazored.LocalStorage;
using CovidTracker.Server.Library.Clients;
using CovidTracker.Shared.Common.RenderLocation;
using CovidTracker.Generators.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

AddServices(builder.Services);
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseWebAssemblyDebugging();
}
else
{
	app.UseExceptionHandler("/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToPage("/_Host");

app.Run();

static void AddServices(IServiceCollection services)
{
	services.AddSingleton<ICurrentRenderLocation, PreRenderServerRenderLocation>()
		.AddScoped<CovidTrackingApiClient>()
		.AddHttpClient()
		.AddFusionCache()
		.AddLibraryGeneratedControllers()
		.AddBlazoredLocalStorage();
}

// Allows program to be used in tests
public partial class Program { }