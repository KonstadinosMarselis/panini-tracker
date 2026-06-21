using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PaniniTracker.Core.Services;
using PaniniTracker.Web;
using PaniniTracker.Web.Services;
using Blazor.DownloadFileFast;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<CollectionService>();
builder.Services.AddScoped<BrowserStorageService>();

await builder.Build().RunAsync();
