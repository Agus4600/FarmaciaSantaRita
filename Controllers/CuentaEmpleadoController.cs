using FarmaciaSantaRita.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Security.Claims;
using System.Collections.Generic;
using System;
using Microsoft.EntityFrameworkCore;

namespace FarmaciaSantaRita.Controllers
{
    [Authorize]
    public class CuentaEmpleadoController : Controller
    {
        private readonly ILogger<CuentaEmpleadoController> _logger;
        private readonly FarmaciabdContext _context;

        public CuentaEmpleadoController(
            ILogger<CuentaEmpleadoController> logger,
            FarmaciabdContext context)
        {
            _logger = logger;
            _context = context;
        }

        // =====================================================
        // LISTA DE EMPLEADOS ACTIVOS (CUENTAEMPLEADO)
        // =====================================================

        [HttpGet]
        public IActionResult Gestionar(int idProveedor, string vista)
        {
            var idUsuarioClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idUsuarioClaim, out int idUsuario))
                return RedirectToAction("Index", "Login");

            var usuarioActual = _context.Usuarios.FirstOrDefault(u => u.Idusuario == idUsuario);

            if (usuarioActual == null || usuarioActual.Rol != "Jefe/a")
            {
                TempData["MensajeError"] = "Acceso denegado.";
                return RedirectToAction("ActualizarCuenta", "Actualizar", new { idProveedor, vista });
            }

            // Solo empleados activos
            List<Usuario> empleados = _context.Usuarios
                .Where(u => u.Rol == "Empleado/a" && u.Eliminado == false)
                .ToList();

            ViewData["IdProveedor"] = idProveedor;
            ViewData["vista"] = vista;

            return View("~/Views/Actualizar/CuentaEmpleado.cshtml", empleados);
        }


        // =====================================================
        // ELIMINAR CUENTAS (SOFT DELETE)
        // =====================================================

        [HttpPost]
        [Consumes("application/json")]
        public IActionResult EliminarCuentas([FromBody] EliminacionRequest request)
        {
            if (request == null || request.idsUsuarios == null || !request.idsUsuarios.Any())
            {
                return Json(new { success = false, message = "No se enviaron IDs." });
            }

            try
            {
                var usuarios = _context.Usuarios
                    .Where(u => request.idsUsuarios.Contains(u.Idusuario))
                    .ToList();

                if (!usuarios.Any())
                {
                    return Json(new { success = false, message = "No se encontraron usuarios." });
                }

                // 🔥 SOFT DELETE
                foreach (var u in usuarios)
                {
                    u.Eliminado = true;
                }

                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Cuenta(s) eliminadas correctamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar empleados.");
                return Json(new { success = false, message = "Error inesperado al eliminar." });
            }
        }


        public class EliminacionRequest
        {
            public List<int> idsUsuarios { get; set; }
        }
    }
}
