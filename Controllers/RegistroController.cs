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
            if (ModelState.IsValid)
            {
                try
                {
                    nuevoUsuario.Contraseña = _encryptionService.Encrypt(nuevoUsuario.ContraseñaPlana);
                    nuevoUsuario.ContraseñaPlana = null;
                    _context.Usuarios.Add(nuevoUsuario);
                    await _context.SaveChangesAsync();

                    TempData["ResultadoActualizacion"] = "Exito";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ViewBag.Error = $"Error detallado: {ex.Message}";
                    if (ex.InnerException != null)
                        ViewBag.Error += $" | Detalle: {ex.InnerException.Message}";
                    return View(nuevoUsuario);
                }
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                ViewBag.Error = "Errores de validación: " + string.Join(" | ", errors);
            }
            return View(nuevoUsuario);
        }
    }
}