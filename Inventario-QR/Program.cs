using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Inventario_QR.Data;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Configuración de Razor Pages y páginas protegidas
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/Index");
    options.Conventions.AuthorizePage("/CreateProduct");
    options.Conventions.AuthorizePage("/ProductChecklist");
    options.Conventions.AuthorizePage("/PrintQRs");
});

// Configuración de autenticación por Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        // En Docker la app se sirve solo por HTTP, por lo que la cookie
        // no debe marcarse como "Secure" o el navegador nunca la enviaria.
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.HttpOnly = true;
    });

// Las claves de Data Protection viven en memoria por defecto: si el contenedor
// se reinicia, todas las sesiones abiertas se invalidarian. Se persisten en disco.
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    Directory.CreateDirectory(dataProtectionKeysPath);
    builder.Services
        .AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
        .SetApplicationName("Inventario-QR");
}

// Configuración de la base de datos PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "No se encontro la cadena de conexion 'DefaultConnection'. Definala en appsettings.json o via la variable de entorno 'ConnectionStrings__DefaultConnection'.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.EnableRetryOnFailure()));

var app = builder.Build();

// El esquema nunca se ha aplicado automaticamente en este proyecto: sin esto,
// un contenedor recien creado arranca contra una base de datos vacia y el login
// falla porque la tabla 'access' no existe.
if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    var retryAttempts = app.Configuration.GetValue<int>("Database:RetryAttempts");
    if (retryAttempts <= 0) retryAttempts = 1;
    var retryDelay = app.Configuration.GetValue<int>("Database:RetryDelaySeconds");
    if (retryDelay <= 0) retryDelay = 5;

    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Migraciones");
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    for (var attempt = 1; ; attempt++)
    {
        try
        {
            logger.LogInformation("Aplicando migraciones de EF Core (intento {Attempt}/{RetryAttempts})...", attempt, retryAttempts);
            context.Database.Migrate();
            logger.LogInformation("Esquema de base de datos actualizado correctamente.");
            break;
        }
        catch (Exception ex) when (attempt < retryAttempts)
        {
            var wait = TimeSpan.FromSeconds(retryDelay);
            logger.LogWarning(ex, "PostgreSQL no disponible. Reintentando en {Wait}...", wait);
            Thread.Sleep(wait);
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Detras de Docker Compose la app solo escucha HTTP, por lo que la redireccion
// a HTTPS dejaria el sitio inaccesible si no hay un terminador TLS delante.
if (app.Configuration.GetValue<bool>("HttpsRedirection:Enabled"))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

// El orden es vital: Authentication SIEMPRE debe ir antes de Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
