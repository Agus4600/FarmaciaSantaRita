using FarmaciaSantaRita.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace FarmaciaSantaRita.Controllers
{
    [Authorize]
    public class RestCuentaEmpleadoController : Controller
    {
        private readonly ILogger<RestCuentaEmpleadoController> _logger;
        private readonly FarmaciabdContext _context;

        public RestCuentaEmpleadoController(
            ILogger<RestCuentaEmpleadoController> logger,
            FarmaciabdContext context)
        {
            _logger = logger;
            _context = context;
        }


        // =====================================================
        // LISTA DE EMPLEADOS ELIMINADOS (SOFT DELETE)
        // =====================================================
        [HttpGet]
        public IActionResult Index(int idProveedor, string vista)
        {
            var empleadosEliminados = _context.Usuarios
                .Where(u => u.Rol == "Empleado/a" && u.Eliminado == true)
                .ToList();

            ViewData["IdProveedor"] = idProveedor;
            ViewData["vista"] = vista;

            // Asegúrate de que esta ruta sea correcta en tu proyecto
            return View("~/Views/Actualizar/RestCuentaEmpleado.cshtml", empleadosEliminados);
        }


        // =====================================================
        // RESTAURAR CUENTAS
        // =====================================================
        [HttpPost]
        [Consumes("application/json")]
        public IActionResult RestaurarCuentas([FromBody] RestauracionRequest request)
        {
            var usuarios = _context.Usuarios
                .Where(u => request.idsUsuarios.Contains(u.Idusuario))
                .ToList();

            foreach (var u in usuarios)
                u.Eliminado = false; // Se marca como no eliminado (activo)

            _context.SaveChanges();

            return Json(new { success = true, message = $"Se han restaurado {usuarios.Count} cuentas(s) exitosamente." });
        }


        // =====================================================
        // ELIMINAR PERMANENTEMENTE (HARD DELETE)
        // =====================================================
        [HttpPost]
        [Consumes("application/json")]
        public IActionResult EliminarPermanentementeCuentas([FromBody] RestauracionRequest request)
        {
            if (request == null || !request.idsUsuarios.Any())
            {
                return Json(new { success = false, message = "No se enviaron IDs de usuarios para eliminar." });
            }

            var idJefeClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idJefeClaim, out int idJefe))
            {
                return Json(new { success = false, message = "Error de autenticación: No se pudo identificar al Jefe/a." });
            }

            try
            {
                int boletasReasignadas = 0;
                int comprasReasignadas = 0;
                int vacacionesReasignadas = 0;
                int inasistenciasReasignadas = 0; // ← NUEVO: Contador para inasistencias

                using var transaction = _context.Database.BeginTransaction();

                foreach (var idEmpleado in request.idsUsuarios)
                {
                    // Reasignar boletas
                    var boletas = _context.Boleta.Where(b => b.Idusuario == idEmpleado).ToList();
                    foreach (var b in boletas) { b.Idusuario = idJefe; boletasReasignadas++; }

                    // Reasignar compras
                    var compras = _context.Compras.Where(c => c.Idusuario == idEmpleado).ToList();
                    foreach (var c in compras) { c.Idusuario = idJefe; comprasReasignadas++; }

                    // Reasignar vacaciones
                    var vacaciones = _context.Vacaciones.Where(v => v.Idusuario == idEmpleado).ToList();
                    foreach (var v in vacaciones) { v.Idusuario = idJefe; vacacionesReasignadas++; }

                    // ← AQUÍ ESTÁ LA SOLUCIÓN: Reasignar inasistencias
                    var inasistencias = _context.Inasistencia.Where(i => i.Idusuario == idEmpleado).ToList();
                    foreach (var i in inasistencias)
                    {
                        i.Idusuario = idJefe;
                        inasistenciasReasignadas++;
                    }

                    // Eliminar el usuario
                    var usuario = _context.Usuarios.FirstOrDefault(u => u.Idusuario == idEmpleado);
                    if (usuario != null)
                    {
                        _context.Usuarios.Remove(usuario);
                    }
                }

                _context.SaveChanges();
                transaction.Commit();

                // Mensaje final más completo
                var partes = new List<string> { "Cuenta(s) eliminada(s) permanentemente." };
                if (boletasReasignadas > 0) partes.Add($"{boletasReasignadas} boleta(s)");
                if (comprasReasignadas > 0) partes.Add($"{comprasReasignadas} compra(s) de clientes");
                if (vacacionesReasignadas > 0) partes.Add($"{vacacionesReasignadas} registro(s) de vacaciones");
                if (inasistenciasReasignadas > 0) partes.Add($"{inasistenciasReasignadas} registro(s) de inasistencias");

                string mensaje = string.Join(". Se reasignaron ", partes);
                if (partes.Count == 1) // Solo el mensaje base
                    mensaje += ". No se encontraron registros para reasignar.";

                return Json(new { success = true, message = mensaje });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar permanentemente cuentas de empleados.");
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "Error al procesar la eliminación: " + innerMsg });
            }
        }








        // Modelo para recibir las IDs de la solicitud AJAX
        public class RestauracionRequest
        {
            public List<int> idsUsuarios { get; set; }
        }
    }
}