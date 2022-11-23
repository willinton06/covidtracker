using CovidTracker.Components;
using CovidTracker.Shared.Common.RenderLocation;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CovidTracker.Generators.ApiClient;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args); 

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<ICurrentRenderLocation, ClientRenderLocation>();
builder.Services.AddBlazoredLocalStorageAsSingleton();
builder.Services.AddGeneratedApiClients();

await builder.Build().RunAsync();

Console.WriteLine($"{typeof(App).FullName} initialized");