using FarmaciaSantaRita.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text;
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
    .Select(u => new
    {
        u.Idusuario,
        NombreCompleto = u.Nombre + " " + u.Apellido,
        u.Dni  // ← Agregar DNI
    })
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
        [Route("Vacaciones/GetDiasPermitidos")]          // Ruta explícita 1
        [Route("api/Vacaciones/GetDiasPermitidos")]
        public async Task<IActionResult> GetDiasPermitidos(int idUsuario)
        {
            var empleado = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Idusuario == idUsuario);

            if (empleado == null || !empleado.FechaIngreso.HasValue)
            {
                return Json(new { success = false, message = "No se encontró la fecha de ingreso del empleado" });
            }

            // Calcular antigüedad en años completos
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var ingreso = DateOnly.FromDateTime(empleado.FechaIngreso.Value);
            int antiguedadAnios = hoy.Year - ingreso.Year;
            if (hoy.Month < ingreso.Month || (hoy.Month == ingreso.Month && hoy.Day < ingreso.Day))
            {
                antiguedadAnios--;
            }

            int diasPermitidos;
            if (antiguedadAnios <= 5)
                diasPermitidos = 14;
            else if (antiguedadAnios <= 10)
                diasPermitidos = 21;
            else if (antiguedadAnios <= 20)
                diasPermitidos = 28;
            else
                diasPermitidos = 35;

            return Json(new
            {
                success = true,
                antiguedadAnios,
                diasPermitidos,
                mensaje = $"Con {antiguedadAnios} años de antigüedad, le corresponden {diasPermitidos} días corridos de vacaciones (LCT)."
            });
        }












        [HttpGet]
        public async Task<IActionResult> GetDatosEmpleado(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return Json(new { success = false, message = "Nombre requerido" });

            var empleado = await _context.Usuarios
                .Where(u => EF.Functions.ILike(u.Nombre + " " + u.Apellido, $"%{nombre.Trim()}%"))
                .Select(u => new
                {
                    success = true,
                    nombreCompleto = u.Nombre + " " + u.Apellido,
                    dni = u.Dni,
                    telefono = u.Telefono,
                    direccion = u.Direccion
                })
                .FirstOrDefaultAsync();

            if (empleado == null)
                return Json(new { success = false, message = "Empleado no encontrado" });

            return Json(empleado);
        }






        [HttpPost]
        public async Task<IActionResult> ApiCreate([FromBody] Vacacion vacacion)
        {
            // 1. Verificación básica de datos
            if (vacacion == null || vacacion.Idusuario == 0)
            {
                return Json(new { success = false, message = "Debe seleccionar un empleado válido." });
            }

            if (vacacion.FechaFin < vacacion.FechaInicio)
            {
                return Json(new { success = false, message = "La fecha de fin no puede ser anterior a la de inicio." });
            }

            try
            {
                // 2. CORRECCIÓN DÍAS A FAVOR: 
                // Calculamos cuántos días está ocupando según el calendario.
                int diasReales = (vacacion.FechaFin - vacacion.FechaInicio).Days + 1;

                // La resta es: (Días que le corresponden por ley) - (Días que eligió)
                // Ejemplo: 14 (permitidos) - 12 (elegidos) = 2 días a favor.
                vacacion.DiasFavor = vacacion.DiasVacaciones - diasReales;

                // 3. Guardar nombre histórico (viene del campo oculto en el frontend)
                vacacion.NombreEmpleadoRegistrado = vacacion.NombreEmpleadoFrontend?.Trim() ?? "Desconocido";

                // 4. Verificación de seguridad (Auditoría)
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { success = false, message = "Sesión expirada. Por favor inicie sesión nuevamente." });
                }

                // IMPORTANTE: Eliminé la línea "vacacion.Idusuario = idUsuarioLogueado" 
                // para que se mantenga el ID del empleado que elegiste en el Select.

                // 5. Ajuste de fechas para PostgreSQL (UTC)
                vacacion.FechaInicio = DateTime.SpecifyKind(vacacion.FechaInicio.Date, DateTimeKind.Utc);
                vacacion.FechaFin = DateTime.SpecifyKind(vacacion.FechaFin.Date, DateTimeKind.Utc);

                _context.Vacaciones.Add(vacacion);
                await _context.SaveChangesAsync();

                // 6. Respuesta al Frontend
                return Json(new
                {
                    success = true,
                    id = vacacion.IdVacaciones,
                    nombreEmpleado = vacacion.NombreEmpleadoRegistrado,
                    diasFavor = vacacion.DiasFavor // Enviamos el valor para actualizar la tabla
                });
            }
            catch (Exception ex)
            {
                // El InnerException suele dar más detalles si hay errores de base de datos
                var errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Error al guardar: " + errorMsg });
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
        public async Task<IActionResult> BuscarVacaciones(string? nombreEmpleado, string? fechaDesde, string? fechaHasta)
        {
            try
            {
                var query = _context.Vacaciones.AsNoTracking();

                // 1. Filtro por nombre (Tolerante)
                if (!string.IsNullOrWhiteSpace(nombreEmpleado))
                {
                    string textoBuscado = nombreEmpleado
                        .ToLowerInvariant()
                        .Normalize(NormalizationForm.FormD)
                        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                        .Aggregate("", (current, c) => current + c)
                        .Replace(" ", "").Replace(".", "").Replace(",", "");

                    // Cambia esto:
                    query = query.Where(v => EF.Functions.ILike(
                        EF.Functions.Unaccent(v.NombreEmpleadoRegistrado ?? ""),
                        $"%{nombreEmpleado.Trim()}%" // Busca el nombre con espacios
                    ));
                }

                // 2. Filtro por fechas (Recibiendo strings desde JS)
                if (!string.IsNullOrEmpty(fechaDesde) && !string.IsNullOrEmpty(fechaHasta))
                {
                    if (DateTime.TryParse(fechaDesde, out DateTime inicio) &&
                        DateTime.TryParse(fechaHasta, out DateTime fin))
                    {
                        var fechaInicioUtc = DateTime.SpecifyKind(inicio.Date, DateTimeKind.Utc);
                        var fechaFinUtc = DateTime.SpecifyKind(fin.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

                        query = query.Where(v => v.FechaInicio >= fechaInicioUtc && v.FechaFin <= fechaFinUtc);
                    }
                }

                // 3. Ejecución de la consulta
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

                // 4. Mapeo final para el Frontend
                var resultados = datosCrudos.Select(v => new
                {
                    v.IdVacaciones,
                    nombreEmpleado = v.NombreEmpleadoRegistrado ?? "Sin Nombre",
                    v.DiasVacaciones,
                    fechaInicio = v.FechaInicio.ToString("dd/MM/yyyy"),
                    fechaFin = v.FechaFin.ToString("dd/MM/yyyy"),
                    diasFavor = Math.Abs(v.DiasVacaciones - ((v.FechaFin - v.FechaInicio).Days + 1))
                }).ToList();

                return Json(resultados);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
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