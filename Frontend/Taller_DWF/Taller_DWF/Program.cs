using System.Globalization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Taller_DWF;
using Taller_DWF.Auth;
using Taller_DWF.Services;
using Taller_DWF.Services.Api;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// URL del backend (wwwroot/appsettings.json)
var apiUrl = builder.Configuration["ApiUrl"] ?? "https://localhost:7150/";
builder.Services.AddSingleton(_ => new HttpClient { BaseAddress = new Uri(apiUrl) });

// Sesión, autorización y servicios de interfaz
builder.Services.AddSingleton<SesionService>();
builder.Services.AddSingleton<NotificacionService>();
builder.Services.AddSingleton<DialogoService>();
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, TallerAuthStateProvider>();
builder.Services.AddScoped<ApiClient>();

// Clientes de la API por módulo
builder.Services.AddScoped<AuthApi>();
builder.Services.AddScoped<ClientesApi>();
builder.Services.AddScoped<VehiculosApi>();
builder.Services.AddScoped<OrdenesApi>();
builder.Services.AddScoped<ReparacionesApi>();
builder.Services.AddScoped<DiagnosticosApi>();
builder.Services.AddScoped<SolicitudesApi>();
builder.Services.AddScoped<BodegaApi>();
builder.Services.AddScoped<InventarioApi>();
builder.Services.AddScoped<DespachosApi>();
builder.Services.AddScoped<FacturasApi>();
builder.Services.AddScoped<UsuariosApi>();
builder.Services.AddScoped<RolesApi>();
builder.Services.AddScoped<ParametrosApi>();
builder.Services.AddScoped<BitacoraApi>();

CultureInfo.DefaultThreadCurrentCulture = Ui.Cultura;
CultureInfo.DefaultThreadCurrentUICulture = Ui.Cultura;

var host = builder.Build();
await host.Services.GetRequiredService<SesionService>().InicializarAsync();
await host.RunAsync();
