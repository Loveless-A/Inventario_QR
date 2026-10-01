using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Inventario_QR.Data;

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
    });

// Configuración de la base de datos PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// El orden es vital: Authentication SIEMPRE debe ir antes de Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();