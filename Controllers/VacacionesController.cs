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
                vacacion.FechaInicio = DateTime.SpecifyKind(vacacion.FechaInicio, DateTimeKind.Utc);
                vacacion.FechaFin = DateTime.SpecifyKind(vacacion.FechaFin, DateTimeKind.Utc);

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
                
                return Json(new { success = false, message = "Error al guardar: " + ex.Message });
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
            try
            {
                var query = _context.Vacaciones.AsNoTracking();

                // Filtro por nombre (ILike es seguro y eficiente en Postgres)
                if (!string.IsNullOrEmpty(nombreEmpleado))
                {
                    query = query.Where(v => EF.Functions.ILike(v.NombreEmpleadoRegistrado ?? "", $"%{nombreEmpleado}%"));
                }

                if (fechaDesde.HasValue && fechaHasta.HasValue)
                {
                    var desdeUtc = DateTime.SpecifyKind(fechaDesde.Value.Date, DateTimeKind.Utc);
                    var hastaUtc = DateTime.SpecifyKind(fechaHasta.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

                    // Solo vacaciones COMPLETAMENTE DENTRO del rango
                    query = query.Where(v =>
                        v.FechaInicio >= desdeUtc && v.FechaFin <= hastaUtc);
                }

                // 1. Traemos SOLO los datos crudos (sin cálculos dentro de la consulta SQL)
                var datosCrudos = await query
                    .OrderByDescending(v => v.FechaInicio)
                    .Select(v => new
                    {
                        v.IdVacaciones,
                        v.NombreEmpleadoRegistrado,
                        v.DiasVacaciones,
                        v.FechaInicio,
                        v.FechaFin
                    })
                    .ToListAsync();

                // 2. Procesamos formato y cálculo de días FAVOR en MEMORIA (C#)
                var resultados = datosCrudos.Select(v => new
                {
                    v.IdVacaciones,
                    nombreEmpleado = v.NombreEmpleadoRegistrado ?? "Sin Nombre",
                    v.DiasVacaciones,
                    fechaInicio = v.FechaInicio.ToString("dd/MM/yyyy"),
                    fechaFin = v.FechaFin.ToString("dd/MM/yyyy"),
                    // Cálculo 100% en C#: Postgres no interviene aquí
                    diasFavor = Math.Abs(v.DiasVacaciones - ((v.FechaFin - v.FechaInicio).Days + 1))
                }).ToList();

                return Json(resultados);
            }
            catch (Exception ex)
            {
                // Para depuración: devuelve el error completo
                return StatusCode(500, new { success = false, message = ex.Message + " | Inner: " + (ex.InnerException?.Message ?? "sin inner") });
            }
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