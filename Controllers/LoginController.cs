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

            var usuario = _context.Usuarios
                .FirstOrDefault(u => u.NombreUsuario == nombreUsuario);

            if (usuario != null)
            {
                // 🔥 DESENCRIPTAR LA CONTRASEÑA GUARDADA Y COMPARAR
                string contraseñaDesencriptada = _encryptionService.Decrypt(usuario.Contraseña);

                if (contraseña == contraseñaDesencriptada)
                {
                    if (usuario.Eliminado)
                    {
                        ViewBag.ShowDisabledModal = true;
                        ViewBag.DisabledMessage = "Tu cuenta ha sido desactivada o eliminada, habla con tu jefe";
                        ViewBag.MostrarRecordarCuenta = false;
                        return View();
                    }

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

                    return RedirectToAction("Index", "Proveedores");
                }
            }

            ViewBag.Error = "Usuario o contraseña incorrectos";
            ViewBag.MostrarRecordarCuenta = true;
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }
}