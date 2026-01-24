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
                    // Validación explícita: Fecha de nacimiento obligatoria
                    if (!nuevoUsuario.FechaNacimiento.HasValue || nuevoUsuario.FechaNacimiento.Value == default(DateTime))
                    {
                        ModelState.AddModelError("FechaNacimiento", "La fecha de nacimiento es obligatoria y debe ser válida.");
                        // Forzamos que ModelState no sea válido para que muestre los errores en la vista
                        ModelState.AddModelError(string.Empty, "Hay errores en el formulario.");
                        return View(nuevoUsuario);
                    }

                    // FechaNacimiento válida → ajustamos a UTC y quitamos hora
                    nuevoUsuario.FechaNacimiento = DateTime.SpecifyKind(
                        nuevoUsuario.FechaNacimiento.Value.Date,
                        DateTimeKind.Utc
                    );

                    // FechaIngreso (opcional, pero si viene la ajustamos)
                    if (nuevoUsuario.FechaIngreso.HasValue)
                    {
                        nuevoUsuario.FechaIngreso = DateTime.SpecifyKind(
                            nuevoUsuario.FechaIngreso.Value.Date,  // también quitamos hora si aplica
                            DateTimeKind.Utc
                        );
                    }

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
                    var errorReal = ex.InnerException?.Message ?? ex.Message;
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