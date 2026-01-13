using FarmaciaSantaRita.Models;
using FarmaciaSantaRita.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Security.Claims; // Añadido para Claims

ExcelPackage.License.SetNonCommercialOrganization("Drogueria Suiza");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();


// ?? CAMBIO 1: Configurar la autenticación basada en Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Ruta a la que redirigir si el usuario no está autenticado
        options.LoginPath = "/Login/Index";
        // Nombre del controlador/acción de la página de inicio de sesión
        options.AccessDeniedPath = "/Home/AccessDenied"; // Opcional: para usuarios con rol insuficiente
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Duración de la cookie
    });

// Se mantiene Session, aunque ya no la usaremos para autenticar, por si otras partes del código la necesitan.
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<FarmaciabdContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("FarmaciaContext"));
}
);

builder.Services.AddScoped<EncryptionService>(sp =>
    new EncryptionService(builder.Configuration["EncryptionKey"]));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ?? CAMBIO 2: DEBE ir antes de UseAuthorization
app.UseAuthentication();

app.UseAuthorization();
app.UseSession(); // Mover UseSession después de UseAuthorization o usar solo el mínimo necesario

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();