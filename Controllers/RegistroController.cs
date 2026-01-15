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
            // Ignoramos campos que no vienen del formulario para que no bloqueen la validación
            ModelState.Remove("Idusuario");
            ModelState.Remove("Contraseña");
            ModelState.Remove("Eliminado");

            if (ModelState.IsValid)
            {
                try
                {
                    // Encriptamos la contraseña usando el campo temporal ContraseñaPlana
                    nuevoUsuario.Contraseña = _encryptionService.Encrypt(nuevoUsuario.ContraseñaPlana);

                    // Forzamos valores por defecto para que la BD no dé error
                    nuevoUsuario.Eliminado = false;
                    nuevoUsuario.Rol = "Usuario"; // O el rol por defecto que prefieras

                    _context.Usuarios.Add(nuevoUsuario);
                    await _context.SaveChangesAsync();

                    TempData["ResultadoActualizacion"] = "Exito";
                    // Una vez creado, lo mandamos al Login para que pruebe su cuenta
                    return RedirectToAction("Index", "Login");
                }
                catch (Exception ex)
                {
                    // Capturamos el error real de la base de datos (InnerException)
                    var errorReal = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    ViewBag.Error = "Error en la base de datos: " + errorReal;
                    return View(nuevoUsuario);
                }
            }
            // Si llegamos aquí es porque el formulario tiene errores (ej: falta un campo)
            var listaErrores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            ViewBag.Error = "Campos incompletos: " + string.Join(", ", listaErrores);

            return View(nuevoUsuario);
        }
    }
}