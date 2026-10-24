using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Talle_DW.Common;
using Talle_DW.Data;
using Talle_DW.Security;
using Talle_DW.Services;
using Talle_DW.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ---------------- Base de datos ----------------
builder.Services.AddDbContext<TallerDbContext>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("TallerDW")));

// ---------------- Seguridad: JWT + permisos por módulo ----------------
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.Seccion));
var jwt = builder.Configuration.GetSection(JwtOptions.Seccion).Get<JwtOptions>()
          ?? throw new InvalidOperationException("Falta la sección 'Jwt' en appsettings.json.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Emisor,
            ValidateAudience = true,
            ValidAudience = jwt.Audiencia,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Clave)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        // Un usuario desactivado pierde el acceso aunque su token siga vigente
        opciones.Events = new JwtBearerEvents
        {
            OnTokenValidated = async contexto =>
            {
                var permisos = contexto.HttpContext.RequestServices.GetRequiredService<IPermisoService>();
                _ = int.TryParse(contexto.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId);
                var perfil = await permisos.ObtenerPerfilAsync(usuarioId);
                if (perfil is not { Activo: true })
                    contexto.Fail("El usuario está desactivado.");
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<IAuthorizationHandler, PermisoHandler>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<VersionCachePermisos>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, UsuarioActual>();
builder.Services.AddScoped<IPermisoService, PermisoService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// ---------------- Servicios de negocio ----------------
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBitacoraService, BitacoraService>();
builder.Services.AddScoped<IParametroService, ParametroService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IRolService, RolService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IVehiculoService, VehiculoService>();
builder.Services.AddScoped<IOrdenService, OrdenService>();
builder.Services.AddScoped<IDiagnosticoService, DiagnosticoService>();
builder.Services.AddScoped<IReparacionService, ReparacionService>();
builder.Services.AddScoped<IBodegaService, BodegaService>();
builder.Services.AddScoped<IInventarioService, InventarioService>();
builder.Services.AddScoped<ISolicitudService, SolicitudService>();
builder.Services.AddScoped<IFacturaService, FacturaService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// ---------------- API ----------------
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(opciones =>
    {
        // Errores de validación con el mismo formato que el resto: { mensaje, errores }
        opciones.InvalidModelStateResponseFactory = contexto =>
        {
            var errores = contexto.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    e => e.Key,
                    e => e.Value!.Errors
                        .Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage) ? "Valor no válido." : x.ErrorMessage)
                        .ToArray());
            var mensaje = errores.Values.SelectMany(v => v).FirstOrDefault() ?? "Los datos enviados no son válidos.";
            return new BadRequestObjectResult(new RespuestaError(mensaje, errores));
        };
    });

builder.Services.AddOpenApi(opciones => opciones.AddDocumentTransformer<EsquemaBearerTransformer>());

var origenes = builder.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];
builder.Services.AddCors(opciones => opciones.AddPolicy("Frontend", politica =>
    politica.WithOrigins(origenes).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseMiddleware<ManejoErroresMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opciones => opciones.WithTitle("Taller DW · API"));
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
