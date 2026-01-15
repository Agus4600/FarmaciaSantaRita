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
            // Limpiamos errores de campos que la BD genera o que no controlamos aquí
            ModelState.Remove("Idusuario");
            ModelState.Remove("Contraseña");
            ModelState.Remove("Eliminado");

            if (ModelState.IsValid)
            {
                try
                {
                    // --- ESTA ES LA LÍNEA CLAVE ---
                    // Convertimos la fecha a UTC para que PostgreSQL no se queje
                    nuevoUsuario.FechaNacimiento = DateTime.SpecifyKind(nuevoUsuario.FechaNacimiento.Value, DateTimeKind.Utc);
                    // ------------------------------

                    nuevoUsuario.Contraseña = _encryptionService.Encrypt(nuevoUsuario.ContraseñaPlana);
                    nuevoUsuario.Eliminado = false;
                    nuevoUsuario.Rol = "Usuario";

                    _context.Usuarios.Add(nuevoUsuario);
                    await _context.SaveChangesAsync();

                    TempData["ResultadoActualizacion"] = "Exito";
                    return RedirectToAction("Index", "Login");
                }
                catch (Exception ex)
                {
                    var errorReal = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    ViewBag.Error = "Error en la base de datos: " + errorReal;
                    return View(nuevoUsuario);
                }
            }

            // SI LLEGA AQUÍ, HAY ERRORES. Vamos a ver cuáles son:
            var errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            ViewBag.Error = "No se pudo crear la cuenta: " + string.Join(" | ", errores);

            return View(nuevoUsuario);
        }
    }
}