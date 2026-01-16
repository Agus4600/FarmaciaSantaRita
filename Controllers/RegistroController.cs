using FarmaciaSantaRita.Models;
using FarmaciaSantaRita.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FarmaciaSantaRita.Controllers
{
    public class RegistroController : Controller
    {
        private readonly FarmaciabdContext _context;
        private readonly EncryptionService _encryptionService;

        public RegistroController(FarmaciabdContext context, EncryptionService encryptionService)
        {
            _context = context;
            _encryptionService = encryptionService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(Usuario nuevoUsuario)
        {
            // Limpiamos errores de campos automáticos
            ModelState.Remove("Idusuario");
            ModelState.Remove("Contraseña");
            ModelState.Remove("Eliminado");

            if (ModelState.IsValid)
            {
                try
                {
                    // Fix para PostgreSQL (sin el .Value porque FechaNacimiento no es nullable)
                    nuevoUsuario.FechaNacimiento = DateTime.SpecifyKind(nuevoUsuario.FechaNacimiento, DateTimeKind.Utc);

                    // Encriptación segura
                    if (!string.IsNullOrEmpty(nuevoUsuario.ContraseñaPlana))
                    {
                        nuevoUsuario.Contraseña = _encryptionService.Encrypt(nuevoUsuario.ContraseñaPlana);
                    }
                    else
                    {
                        nuevoUsuario.Contraseña = "";
                    }

                    nuevoUsuario.Eliminado = false;

                    _context.Usuarios.Add(nuevoUsuario);
                    await _context.SaveChangesAsync();

                    TempData["ResultadoActualizacion"] = "Exito";
                    return RedirectToAction("Index", "Login");
                }
                catch (Exception ex)
                {
                    // Captura el error real de la base de datos (por si el DNI está duplicado, etc.)
                    var errorReal = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    ViewBag.Error = "Error en la base de datos: " + errorReal;
                    return View(nuevoUsuario);
                }
            }

            // Si hay errores de validación en el formulario
            var errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            ViewBag.Error = "No se pudo crear la cuenta: " + string.Join(" | ", errores);

            return View(nuevoUsuario);
        }
    }
}