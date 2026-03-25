using FarmaciaSantaRita.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace FarmaciaSantaRita.Controllers
{
    public class ProveedoresController : Controller
    {
        private readonly FarmaciabdContext _context;
        private readonly IAntiforgery _antiforgery;
        private readonly ILogger<ProveedoresController> _logger;

        // ⭐ CONSTANTE PARA DROGUERÍA SUIZA NORMALIZADA ⭐
        private const string NOMBRE_SUIZA_NORMALIZADO = "drogueriasuiza";

        public ProveedoresController(FarmaciabdContext context, IAntiforgery antiforgery, ILogger<ProveedoresController> logger)
        {
            _context = context;
            _antiforgery = antiforgery;
            _logger = logger;
        }

        // ==========================================================
        // 🛠️ FUNCIÓN DE NORMALIZACIÓN (SOLO PARA COMPARACIÓN)
        // ==========================================================
        private string NormalizarNombreProveedor(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
            string normalizado = new string(
                texto.Normalize(System.Text.NormalizationForm.FormD)
                     .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) !=
                                 System.Globalization.UnicodeCategory.NonSpacingMark)
                     .ToArray()
            );
            normalizado = normalizado.ToLowerInvariant();
            normalizado = Regex.Replace(normalizado, @"[^\w]", "");
            return normalizado;
        }

        // ==========================================================
        // 🔹 ACCIONES DE VISTA (GET)
        // ==========================================================
        public IActionResult Index()
        {
            var proveedores = _context.Proveedors.ToList()
                .Where(p => !p.Eliminado)
                .ToList();
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            ViewData["RequestVerificationToken"] = tokens.RequestToken;
            return View(proveedores);
        }

        public IActionResult VistaDrogueriaSuiza(int idProveedor, string nombreMostrar)
        {
            ViewData["IdProveedor"] = idProveedor;
            ViewData["NombreProveedor"] = nombreMostrar;
            int userId = 0;
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdString != null && int.TryParse(userIdString, out int parsedId))
            {
                userId = parsedId;
            }
            ViewBag.IdUsuarioAutenticado = userId;
            return View("Drogueria");
        }

        [HttpGet]
        public IActionResult ProveedorSecundario(int idProveedor, string nombreMostrar)
        {
            ViewData["IdProveedor"] = idProveedor;
            ViewData["NombreProveedor"] = nombreMostrar;
            return View();
        }





        [HttpGet]
        public IActionResult Boletas(string proveedor)
        {
            if (string.IsNullOrWhiteSpace(proveedor))
            {
                TempData["MensajeError"] = "Proveedor no válido.";
                return RedirectToAction("Index");
            }

            var proveedorNormalizado = NormalizarNombreProveedor(proveedor);

            // ⭐ LÓGICA ESPECIAL PARA DROGUERÍA SUIZA ⭐
            if (proveedorNormalizado == NOMBRE_SUIZA_NORMALIZADO)
            {
                var prov = _context.Proveedors
                    .FirstOrDefault(p => NormalizarNombreProveedor(p.NombreProveedor) == NOMBRE_SUIZA_NORMALIZADO 
                                      && !p.Eliminado);

                if (prov != null)
                {
                    return RedirectToAction("Drogueria", "Proveedores",
                        new { idProveedor = prov.Idproveedor, nombreMostrar = "Droguería Suiza" });
                }
                else
                {
                    TempData["MensajeError"] = "Droguería Suiza no está disponible actualmente.";
                    return RedirectToAction("Index");
                }
            }

            // Proveedores normales
            var proveedorEncontrado = _context.Proveedors
                .FirstOrDefault(p => NormalizarNombreProveedor(p.NombreProveedor) == proveedorNormalizado && !p.Eliminado);

            if (proveedorEncontrado == null)
            {
                TempData["MensajeError"] = "Proveedor no encontrado.";
                return RedirectToAction("Index");
            }

            return RedirectToAction("ProveedorSecundario", "Proveedores",
                new { idProveedor = proveedorEncontrado.Idproveedor, nombreMostrar = proveedorEncontrado.NombreProveedor });
        }




        [HttpGet]
        public IActionResult Inasistencia(string origen = null, int idProveedor = 0)
        {
            return RedirectToAction("Inasistencia", "Inasistencia", new { origen, idProveedor });
        }





        [HttpGet]
        public IActionResult VolverALaVistaOriginal(string origen, int idProveedor)
        {
            if (idProveedor <= 0)
                return RedirectToAction("Index");

            var proveedor = _context.Proveedors
                .FirstOrDefault(p => p.Idproveedor == idProveedor);

            if (proveedor == null)
                return RedirectToAction("Index");

            if (!string.IsNullOrWhiteSpace(origen) &&
                origen.Equals("Drogueria", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Drogueria", new
                {
                    idProveedor = idProveedor,
                    nombreMostrar = "Droguería Suiza"
                });
            }

            return RedirectToAction("ProveedorSecundario", new
            {
                idProveedor = idProveedor,
                nombreMostrar = proveedor.NombreProveedor
            });
        }

        [HttpGet]
        public IActionResult Ingresar()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Registrar()
        {
            return View();
        }




        [HttpGet]
        public IActionResult Restaurar()
        {
            var proveedoresEliminados = _context.Proveedors
                .Where(p => p.Eliminado && p.NombreProveedor != "Proveedor Eliminado")
                .ToList();

            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            ViewData["RequestVerificationToken"] = tokens.RequestToken;
            return View(proveedoresEliminados);
        }

        [HttpGet]
        public IActionResult IngresarProveedor(int? idProveedorSeleccionado, string vista)
        {
            if (idProveedorSeleccionado == null || idProveedorSeleccionado == 0)
                return RedirectToAction("Index");

            var proveedor = _context.Proveedors.Find(idProveedorSeleccionado);
            if (proveedor == null) return RedirectToAction("Index");

            string nombreMostrar = proveedor.NombreProveedor;

            if (!string.IsNullOrEmpty(vista))
            {
                if (vista == "Drogueria")
                    return RedirectToAction("VistaDrogueriaSuiza", new { idProveedor = idProveedorSeleccionado, nombreMostrar = proveedor.NombreProveedor });
                if (vista == "ProveedorSecundario")
                    return RedirectToAction("ProveedorSecundario", new { idProveedor = idProveedorSeleccionado, nombreMostrar = proveedor.NombreProveedor });
            }

            // ⭐ LÓGICA AUTOMÁTICA PARA DROGUERÍA SUIZA ⭐
            var nombreNormalizado = NormalizarNombreProveedor(proveedor.NombreProveedor);
            if (nombreNormalizado == NOMBRE_SUIZA_NORMALIZADO && !proveedor.Eliminado)
            {
                return RedirectToAction("VistaDrogueriaSuiza", 
                    new { idProveedor = idProveedorSeleccionado, nombreMostrar = "Droguería Suiza" });
            }

            return RedirectToAction("ProveedorSecundario", 
                new { idProveedor = idProveedorSeleccionado, nombreMostrar = proveedor.NombreProveedor });
        }






        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Registrar(Proveedor proveedor)
        {
            if (string.IsNullOrWhiteSpace(proveedor.NombreProveedor) ||
                string.IsNullOrWhiteSpace(proveedor.TelefonoProveedor) ||
                string.IsNullOrWhiteSpace(proveedor.CorreoProveedor))
            {
                TempData["MensajeError"] = "Por favor completa todos los campos.";
                return RedirectToAction("Registrar");
            }

            // Validación de correo
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!emailRegex.IsMatch(proveedor.CorreoProveedor))
            {
                TempData["MensajeError"] = "El formato del correo electrónico no es válido.";
                return RedirectToAction("Registrar");
            }

            try
            {
                // 1. Verificar si ya existe un proveedor con el mismo nombre (activo o eliminado)
                var proveedorExistente = _context.Proveedors
                    .IgnoreQueryFilters()
                    .FirstOrDefault(p => p.NombreProveedor.Trim().ToLower() == proveedor.NombreProveedor.Trim().ToLower());

                if (proveedorExistente != null)
                {
                    // Si existe pero está eliminado → lo reactivamos
                    if (proveedorExistente.Eliminado)
                    {
                        proveedorExistente.Eliminado = false;
                        proveedorExistente.EstadoProveedor = "Activo";
                        proveedorExistente.TelefonoProveedor = proveedor.TelefonoProveedor;
                        proveedorExistente.CorreoProveedor = proveedor.CorreoProveedor;
                        _context.SaveChanges();

                        // Reasignar boletas huérfanas (las que quedaron sin proveedor)
                        var boletasHuérfanas = _context.Boleta
                            .Where(b => b.Idproveedor == 0 || b.Idproveedor == null) // boletas sin proveedor
                            .ToList();

                        foreach (var boleta in boletasHuérfanas)
                        {
                            boleta.Idproveedor = proveedorExistente.Idproveedor;
                        }

                        _context.SaveChanges();

                        TempData["MensajeExito"] = "Proveedor reactivado correctamente. Las boletas huérfanas han sido reasignadas.";
                        return RedirectToAction("Registrar");
                    }
                    else
                    {
                        TempData["MensajeError"] = "Ya existe un proveedor activo con ese nombre.";
                        return RedirectToAction("Registrar");
                    }
                }

                // 2. Si no existe → lo creamos normalmente
                proveedor.EstadoProveedor = "Activo";
                proveedor.Eliminado = false;

                _context.Proveedors.Add(proveedor);
                _context.SaveChanges();

                TempData["MensajeExito"] = "Proveedor registrado correctamente.";
                return RedirectToAction("Registrar");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar proveedor");
                TempData["MensajeError"] = "Error interno: " + (ex.InnerException?.Message ?? ex.Message);
                return RedirectToAction("Registrar");
            }
        }






        // ... (EliminarSeleccionados, EliminarPermanente, RestaurarSeleccionados permanecen iguales)
        // Solo los copio para que el archivo esté completo.

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarSeleccionados([FromBody] List<int> ids)
        {
            if (ids == null || ids.Count == 0)
                return BadRequest(new { mensaje = "No se recibieron proveedores para eliminar." });

            try
            {
                var proveedoresAEliminar = _context.Proveedors
                    .Where(p => ids.Contains(p.Idproveedor))
                    .ToList();

                if (proveedoresAEliminar.Count == 0)
                    return NotFound(new { mensaje = "No se encontraron los proveedores seleccionados." });

                foreach (var proveedor in proveedoresAEliminar)
                {
                    proveedor.Eliminado = true;
                    proveedor.EstadoProveedor = "Eliminado";
                }

                _context.SaveChanges();
                return Ok(new { mensaje = "Proveedor(es) eliminado(s) correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al eliminar los proveedores: " + ex.Message });
            }
        }






        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarPermanente([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
                return BadRequest(new { mensaje = "No se recibieron proveedores para eliminar." });

            try
            {
                var proveedoresAEliminar = await _context.Proveedors
                    .IgnoreQueryFilters()
                    .Where(p => ids.Contains(p.Idproveedor))
                    .ToListAsync();

                if (!proveedoresAEliminar.Any())
                    return NotFound(new { mensaje = "No se encontraron los proveedores seleccionados." });

                // Solo marcamos como eliminado (soft delete) - NO borramos las boletas
                foreach (var proveedor in proveedoresAEliminar)
                {
                    proveedor.Eliminado = true;
                    proveedor.EstadoProveedor = "Eliminado Permanentemente";
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = $"Se eliminaron {proveedoresAEliminar.Count} proveedor(es) permanentemente. " +
                              "Las boletas asociadas se mantendrán y se reasignarán automáticamente si se registra nuevamente el mismo nombre."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar permanentemente proveedores.");
                return StatusCode(500, new { mensaje = "Error interno al eliminar: " + ex.Message });
            }
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RestaurarSeleccionados([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
                return BadRequest(new { mensaje = "No se recibieron proveedores." });

            try
            {
                // Importante: IgnoreQueryFilters() permite encontrar registros con 'Eliminado = true'
                var proveedores = _context.Proveedors
                    .IgnoreQueryFilters()
                    .Where(p => ids.Contains(p.Idproveedor))
                    .ToList();

                foreach (var p in proveedores)
                {
                    p.Eliminado = false;
                    p.EstadoProveedor = "Activo";
                }

                _context.SaveChanges();
                return Ok(new { mensaje = "Proveedor(es) restaurado(s) correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error en el servidor: " + ex.Message });
            }
        }
    }
}