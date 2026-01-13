using FarmaciaSantaRita.Models;
using FarmaciaSantaRita.Services; // ← NUEVO: Para EncryptionService
using Microsoft.AspNetCore.Mvc;

namespace FarmaciaSantaRita.Controllers
{
    public class RecordarController : Controller
    {
        private readonly FarmaciabdContext _context;
        private readonly EncryptionService _encryptionService; // ← Inyectamos el servicio

        public RecordarController(FarmaciabdContext context, EncryptionService encryptionService)
        {
            _context = context;
            _encryptionService = encryptionService; // ← Lo recibimos
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(string dni)
        {
            if (string.IsNullOrEmpty(dni))
            {
                ViewBag.Mensaje = "Por favor, ingresa tu DNI.";
                return View();
            }

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Dni == dni);

            if (usuario != null)
            {
                ViewBag.NombreUsuario = usuario.NombreUsuario;

                // Detectar si es hash BCrypt (empieza con $2a$, $2b$, $2y$)
                if (usuario.Contraseña.StartsWith("$2a$") || usuario.Contraseña.StartsWith("$2b$") || usuario.Contraseña.StartsWith("$2y$"))
                {
                    ViewBag.Contraseña = "[Contraseña antigua protegida - Contacta al administrador para restablecerla]";
                }
                else
                {
                    // Es AES → desencriptar
                    ViewBag.Contraseña = _encryptionService.Decrypt(usuario.Contraseña);
                }
            }
            else
            {
                ViewBag.Mensaje = "No se encontró ninguna cuenta con ese DNI.";
            }

            return View();
        }
    }
}