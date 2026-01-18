using FarmaciaSantaRita.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FarmaciaSantaRita.Controllers
{
    public class VacacionesController : Controller
    {
        private readonly FarmaciabdContext _context;

        public VacacionesController(FarmaciabdContext context)
        {
            _context = context;
        }

        // GET: Lista todas las vacaciones
        public async Task<IActionResult> Index()
        {
            ViewBag.Empleados = await _context.Usuarios
                .Where(u => u.Rol == "Empleado/a" || u.Rol == "Jefe/a")
                .OrderBy(u => u.Apellido)
                .Select(u => new { u.Idusuario, NombreCompleto = u.Nombre + " " + u.Apellido })
                .ToListAsync();

            var vacaciones = await _context.Vacaciones
                .AsNoTracking()
                .OrderByDescending(v => v.FechaInicio)
                .Select(v => new Vacacion
                {
                    IdVacaciones = v.IdVacaciones,
                    DiasVacaciones = v.DiasVacaciones,
                    FechaInicio = v.FechaInicio,
                    FechaFin = v.FechaFin,
                    DiasFavor = v.DiasFavor,
                    NombreEmpleadoRegistrado = v.NombreEmpleadoRegistrado ?? "Sin nombre"
                })
                .ToListAsync();

            return View(vacaciones);
        }





        [HttpGet]
        public async Task<IActionResult> GetEmpleados()
        {
            var empleados = await _context.Usuarios
                .Where(u => u.Rol == "Empleado/a" || u.Rol == "Jefe/a")
                .OrderBy(u => u.Apellido)
                .ThenBy(u => u.Nombre)
                .Select(u => new
                {
                    idusuario = u.Idusuario,
                    nombre = u.Nombre + " " + u.Apellido
                })
                .ToListAsync();

            return Json(empleados);
        }

        // API POST: Crea vacación desde JavaScript (botón "Agregar")






        [HttpPost]
        public async Task<IActionResult> ApiCreate([FromBody] Vacacion vacacion)
        {
            if (vacacion == null || vacacion.Idusuario == 0)
            {
                return Json(new { success = false, message = "Datos inválidos." });
            }

            if (vacacion.FechaFin < vacacion.FechaInicio)
            {
                return Json(new { success = false, message = "La fecha de fin no puede ser anterior a la de inicio." });
            }

            try
            {
                // Calcular días reales y días a favor
                int diasReales = (vacacion.FechaFin - vacacion.FechaInicio).Days + 1;
                vacacion.DiasFavor = Math.Abs(vacacion.DiasVacaciones - diasReales);

                // === GUARDAR EL NOMBRE REAL DEL EMPLEADO EN LA BD (histórico) ===
                vacacion.NombreEmpleadoRegistrado = vacacion.NombreEmpleadoFrontend?.Trim() ?? "Desconocido";

                // === GUARDAR QUIÉN REGISTRÓ LA VACACIÓN (auditoría) ===
                int idUsuarioLogueado = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                if (idUsuarioLogueado == 0)
                {
                    return Json(new { success = false, message = "Usuario no autenticado." });
                }
                vacacion.Idusuario = idUsuarioLogueado;

                _context.Vacaciones.Add(vacacion);
                await _context.SaveChangesAsync();

                // Devolver éxito con el ID y el nombre para mostrar en la tabla
                return Json(new
                {
                    success = true,
                    id = vacacion.IdVacaciones,
                    nombreEmpleado = vacacion.NombreEmpleadoRegistrado
                });
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                var fullError = $"Error al guardar: {innerMessage}\nStackTrace: {ex.StackTrace}";
                return Json(new { success = false, message = fullError });
            }
        }








        [HttpDelete]
        public async Task<IActionResult> EliminarVacacion(int id)
        {
            try
            {
                var vacacion = await _context.Vacaciones.FindAsync(id);
                if (vacacion == null)
                    return Json(new { success = false, message = "Registro no encontrado." });

                _context.Vacaciones.Remove(vacacion);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Empleados = await _context.Usuarios
                .Where(u => u.Rol == "Empleado/a" || u.Rol == "Jefe/a")
                .OrderBy(u => u.Apellido)
                .ThenBy(u => u.Nombre)
                .Select(u => new { u.Idusuario, NombreCompleto = u.Nombre + " " + u.Apellido })
                .ToListAsync();

            return View();
        }




        [HttpGet]
        public async Task<IActionResult> BuscarVacaciones(string? nombreEmpleado, DateTime? fechaDesde, DateTime? fechaHasta)
        {
            var query = _context.Vacaciones.AsQueryable();

            // Filtro por nombre y apellido (Ignorando acentos y mayúsculas)
            if (!string.IsNullOrEmpty(nombreEmpleado))
            {
                // SQL Server Collation: Modern_Spanish_CI_AI 
                // CI = Case Insensitive (ignora mayúsculas)
                // AI = Accent Insensitive (ignora acentos)
                query = query.Where(v => EF.Functions.Collate(v.NombreEmpleadoRegistrado, "Modern_Spanish_CI_AI")
                                         .Contains(EF.Functions.Collate(nombreEmpleado, "Modern_Spanish_CI_AI")));
            }

            // Filtro por rango de fechas
            if (fechaDesde.HasValue && fechaHasta.HasValue)
            {
                // Comparamos solo la parte fecha (Date) para evitar problemas de horas
                query = query.Where(v => v.FechaInicio.Date >= fechaDesde.Value.Date &&
                                         v.FechaInicio.Date <= fechaHasta.Value.Date);
            }

            var resultados = await query
                .OrderByDescending(v => v.FechaInicio)
                .Select(v => new
                {
                    v.IdVacaciones,
                    nombreEmpleado = v.NombreEmpleadoRegistrado ?? "Sin Nombre",
                    diasVacaciones = v.DiasVacaciones,
                    // Aquí corregimos el formato de fecha para que el JS lo reciba limpio
                    fechaInicio = v.FechaInicio.ToString("dd/MM/yyyy"),
                    fechaFin = v.FechaFin.ToString("dd/MM/yyyy"),
                    diasFavor = Math.Abs(v.DiasVacaciones - ((v.FechaFin - v.FechaInicio).Days + 1))
                })
                .ToListAsync();

            return Json(resultados);
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Vacacion vacacion)
        {
            if (vacacion.FechaFin < vacacion.FechaInicio)
            {
                ModelState.AddModelError("FechaFin", "La fecha de fin no puede ser anterior a la de inicio.");
            }

            if (ModelState.IsValid)
            {
                int diasReales = (vacacion.FechaFin - vacacion.FechaInicio).Days + 1;
                vacacion.DiasFavor = Math.Abs(vacacion.DiasVacaciones - diasReales);

                // === GUARDAR QUIÉN REGISTRÓ LA VACACIÓN ===
                int idUsuarioLogueado = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                if (idUsuarioLogueado == 0)
                {
                    ModelState.AddModelError("", "Usuario no autenticado.");
                }
                else
                {
                    vacacion.Idusuario = idUsuarioLogueado;
                }

                _context.Add(vacacion);
                await _context.SaveChangesAsync();
                TempData["MensajeExito"] = "Vacaciones registradas correctamente.";
                return RedirectToAction(nameof(Index));
            }

            // === SI HAY ERRORES, DEVOLVEMOS LA VISTA CON LOS DATOS ===
            ViewBag.Empleados = await _context.Usuarios
                .Where(u => u.Rol == "Empleado/a" || u.Rol == "Jefe/a")
                .OrderBy(u => u.Apellido)
                .ThenBy(u => u.Nombre)
                .Select(u => new { u.Idusuario, NombreCompleto = u.Nombre + " " + u.Apellido })
                .ToListAsync();

            return View(vacacion);
        }
    }
}