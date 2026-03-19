using FarmaciaSantaRita.Models;
using FarmaciaSantaRita.Services; // ← NUEVO: Para EncryptionService
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FarmaciaSantaRita.Controllers
{
    public class LoginController : Controller
    {
        private readonly FarmaciabdContext _context;
        private readonly EncryptionService _encryptionService; // ← Inyectamos el servicio


        public LoginController(FarmaciabdContext context, EncryptionService encryptionService)
        {
            _context = context;
            _encryptionService = encryptionService;
        }

        public IActionResult Index()
        {
            // Esto generará la versión encriptada de admin123 usando TU clave secreta
            string claveEncriptada = _encryptionService.Encrypt("admin123");

            // Lo mandamos a la consola para que lo puedas ver en los Logs de Render
            Console.WriteLine("CLAVE_PARA_NEON: " + claveEncriptada);

            return View();
        }




        [HttpPost]
        public async Task<IActionResult> Index(string nombreUsuario, string contraseña)
        {
            if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(contraseña))
            {
                ViewBag.MostrarRecordarCuenta = true;
                ViewBag.Error = "Debes completar todos los campos para iniciar sesión.";
                return View();
            }

            Console.WriteLine($"[LOGIN] Intento de login con usuario: '{nombreUsuario}'");

            var usuario = _context.Usuarios
                .FirstOrDefault(u => u.NombreUsuario == nombreUsuario);

            if (usuario == null)
            {
                Console.WriteLine("[LOGIN] Usuario no encontrado");
                ViewBag.Error = "Usuario o contraseña incorrectos";
                ViewBag.MostrarRecordarCuenta = true;
                return View();
            }

            Console.WriteLine($"[LOGIN] Usuario encontrado. Rol: {usuario.Rol}, Eliminado: {usuario.Eliminado}");

            if (usuario.Eliminado)
            {
                ViewBag.ShowDisabledModal = true;
                ViewBag.DisabledMessage = "Tu cuenta ha sido desactivada o eliminada, habla con tu jefe";
                ViewBag.MostrarRecordarCuenta = false;
                return View();
            }

            try
            {
                string contraseñaDesencriptada = _encryptionService.Decrypt(usuario.Contraseña);
                Console.WriteLine($"[LOGIN] Contraseña desencriptada: '{contraseñaDesencriptada}'");

                if (contraseña == contraseñaDesencriptada)
                {
                    Console.WriteLine("[LOGIN] Contraseña correcta - Login exitoso");

                    // NUEVO: Verificar si el rol está pendiente o no asignado
                    if (string.IsNullOrWhiteSpace(usuario.Rol) || usuario.Rol == "Pendiente")
                    {
                        ViewBag.ShowRolPendienteModal = true;  // ← Bandera para mostrar el Swal
                        ViewBag.MostrarRecordarCuenta = false;
                        return View();
                    }

                    // Si tiene rol válido → procede con el login normal
                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Idusuario.ToString()),
                new Claim(ClaimTypes.Name, usuario.NombreUsuario),
                new Claim(ClaimTypes.Role, usuario.Rol ?? "")
            };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                    };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity), authProperties);

                    return RedirectToAction("Index", "PanelSeleccion");
                }
                else
                {
                    Console.WriteLine($"[LOGIN] Contraseña NO coincide. Ingresada: '{contraseña}' vs Desencriptada: '{contraseñaDesencriptada}'");
                    ViewBag.Error = "Usuario o contraseña incorrectos";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOGIN] ERROR AL DESENCRIPTAR: {ex.Message}\nStackTrace: {ex.StackTrace}");
                ViewBag.Error = "Error interno al procesar la contraseña. Contacta al administrador.";
            }

            ViewBag.MostrarRecordarCuenta = true;
            return View();
        }



        // Acción TEMPORAL solo para desarrollo: crear usuarios de prueba
        // ¡Borrar o comentar en producción!
        [HttpGet]
        public IActionResult CrearUsuariosPrueba()
        {
            var usuariosNuevos = new List<Usuario>
    {
        // SuperAdmin (rol especial)
        new Usuario
        {
            Nombre = "Super",
            Apellido = "Admin",
            NombreUsuario = "superadmin",
            Dni = "00000000",
            Contraseña = _encryptionService.Encrypt("admin123"), // Encripta "admin123"
            Rol = "SuperAdmin",
            Eliminado = false,
            Telefono = "123456789",
            CorreoUsuario = "superadmin@farmaciasantarita.com",
            FechaNacimiento = new DateTime(1990, 1, 1),
            FechaIngreso = DateTime.UtcNow,
            Direccion = "Dirección Admin"
        },

        // Jefe/a de prueba
        new Usuario
        {
            Nombre = "Alejandra",
            Apellido = "Ovejero",
            NombreUsuario = "Ale31",
            Dni = "42502029",
            Contraseña = _encryptionService.Encrypt("jefe123"), // Cambia la contraseña real
            Rol = "Jefe/a",
            Eliminado = false,
            Telefono = "03865644770",
            CorreoUsuario = "ale205@gmail.com",
            FechaNacimiento = new DateTime(1975, 12, 31),
            FechaIngreso = new DateTime(2002, 1, 1),
            Direccion = "25 de mayo 52, Villa Quinteros"
        },

        // Empleado/a de prueba
        new Usuario
        {
            Nombre = "Juan",
            Apellido = "Pérez",
            NombreUsuario = "juanp",
            Dni = "30123456",
            Contraseña = _encryptionService.Encrypt("empleado123"),
            Rol = "Empleado/a",
            Eliminado = false,
            Telefono = "3816543210",
            CorreoUsuario = "juanp@farmaciasantarita.com",
            FechaNacimiento = new DateTime(1995, 5, 15),
            FechaIngreso = DateTime.UtcNow.AddYears(-1),
            Direccion = "Av. Siempre Viva 123"
        }
    };

            foreach (var usuario in usuariosNuevos)
            {
                // Evitar duplicados
                if (!_context.Usuarios.Any(u => u.NombreUsuario == usuario.NombreUsuario))
                {
                    _context.Usuarios.Add(usuario);
                }
            }

            _context.SaveChanges();

            return Content("Usuarios de prueba creados exitosamente:\n" +
                           "- superadmin / admin123 (SuperAdmin)\n" +
                           "- Ale31 / jefe123 (Jefe/a)\n" +
                           "- juanp / empleado123 (Empleado/a)\n" +
                           "¡Borra esta acción en producción!");
        }



        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }
}