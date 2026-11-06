using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using RecipeTracker.Core;
using RecipeTracker.Core.Ports;
using RecipeTracker.Web;
using RecipeTracker.Web.Adapters;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

builder.Services.AddSingleton<IRecordStore, DexieRecordStore>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<AppCore>();

await builder.Build().RunAsync();
