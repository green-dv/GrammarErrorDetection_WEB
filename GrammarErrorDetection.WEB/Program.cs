using GrammarErrorDetection.WEB;
using GrammarErrorDetection.Infraestructure.ApiClients;
using GrammarErrorDetection.Infraestructure.Interfaces;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<IApiClient, ApiClient>();

await builder.Build().RunAsync();
