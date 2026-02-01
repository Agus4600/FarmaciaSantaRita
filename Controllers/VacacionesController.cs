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
    .OrderByDescending(v => v.IdVacaciones)  // ← CAMBIO: por ID descendente
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







        // Ruta EXACTA que debe coincidir con el fetch en JS: /Vacaciones/CheckVacacionesSolapadas
        [HttpGet]  // ← Correcto, con la "a"
        public async Task<IActionResult> CheckVacacionesSolapadas(int idEmpleadoNuevo, string fechaInicio, string fechaFin)
        {
            Console.WriteLine($"CheckVacacionesSolapadas llamado: id={idEmpleadoNuevo}, inicio={fechaInicio}, fin={fechaFin}");
            if (idEmpleadoNuevo <= 0 || string.IsNullOrEmpty(fechaInicio) || string.IsNullOrEmpty(fechaFin))
                return Json(new { permitido = false, mensaje = "Datos incompletos" });

            if (!DateTime.TryParse(fechaInicio, out DateTime inicioNuevo) ||
                !DateTime.TryParse(fechaFin, out DateTime finNuevo))
                return Json(new { permitido = false, mensaje = "Fechas inválidas" });

            inicioNuevo = DateTime.SpecifyKind(inicioNuevo.Date, DateTimeKind.Utc);
            finNuevo = DateTime.SpecifyKind(finNuevo.Date, DateTimeKind.Utc);

            // 1. Solapamiento con el MISMO empleado
            var solapaMismo = await _context.Vacaciones
                .AnyAsync(v => v.Idusuario == idEmpleadoNuevo &&
                               v.FechaInicio <= finNuevo &&
                               v.FechaFin >= inicioNuevo);

            if (solapaMismo)
            {
                var conflicto = await _context.Vacaciones
                    .Where(v => v.Idusuario == idEmpleadoNuevo &&
                                v.FechaInicio <= finNuevo &&
                                v.FechaFin >= inicioNuevo)
                    .Select(v => new { Inicio = v.FechaInicio.ToString("dd/MM/yyyy"), Fin = v.FechaFin.ToString("dd/MM/yyyy") })
                    .FirstOrDefaultAsync();

                return Json(new
                {
                    permitido = false,
                    mensaje = $"¡Atención! Ya tiene vacaciones del {conflicto?.Inicio} al {conflicto?.Fin}. No se puede superponer."
                });
            }

            // 2. Solapamiento con CUALQUIER OTRO empleado (cobertura total)
            var solapaOtro = await _context.Vacaciones
                .AnyAsync(v => v.Idusuario != idEmpleadoNuevo &&
                               v.FechaInicio <= finNuevo &&
                               v.FechaFin >= inicioNuevo);

            if (solapaOtro)
            {
                var conflictoOtro = await _context.Vacaciones
                    .Where(v => v.Idusuario != idEmpleadoNuevo &&
                                v.FechaInicio <= finNuevo &&
                                v.FechaFin >= inicioNuevo)
                    .Select(v => new { Nombre = v.NombreEmpleadoRegistrado, Inicio = v.FechaInicio.ToString("dd/MM/yyyy"), Fin = v.FechaFin.ToString("dd/MM/yyyy") })
                    .FirstOrDefaultAsync();

                return Json(new
                {
                    permitido = false,
                    mensaje = $"¡Atención! {conflictoOtro?.Nombre} ya tiene vacaciones del {conflictoOtro?.Inicio} al {conflictoOtro?.Fin}. No se puede registrar otro empleado en este período."
                });
            }

            return Json(new { permitido = true });
        }















        [HttpGet]
        public async Task<IActionResult> GetDatosEmpleado(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return Json(new { success = false, message = "Nombre requerido" });

            var empleado = await _context.Usuarios
                .Where(u => EF.Functions.ILike(u.Nombre + " " + u.Apellido, $"%{nombre.Trim()}%"))
                .FirstOrDefaultAsync();

            if (empleado == null)
                return Json(new { success = false, message = "Empleado no encontrado" });

            // Calcular días de vacaciones legales según antigüedad
            int diasVacacionesLegales = 14; // default
            if (empleado.FechaIngreso.HasValue)
            {
                var hoy = DateOnly.FromDateTime(DateTime.Today);
                var ingreso = DateOnly.FromDateTime(empleado.FechaIngreso.Value);
                int antiguedadAnios = hoy.Year - ingreso.Year;
                if (hoy.Month < ingreso.Month || (hoy.Month == ingreso.Month && hoy.Day < ingreso.Day))
                    antiguedadAnios--;

                if (antiguedadAnios <= 5) diasVacacionesLegales = 14;
                else if (antiguedadAnios <= 10) diasVacacionesLegales = 21;
                else if (antiguedadAnios <= 20) diasVacacionesLegales = 28;
                else diasVacacionesLegales = 35;
            }

            return Json(new
            {
                success = true,
                nombreCompleto = empleado.Nombre + " " + empleado.Apellido,
                dni = empleado.Dni,
                telefono = empleado.Telefono,
                direccion = empleado.Direccion,
                diasVacacionesLegales  // ← Campo agregado
            });
        }

        [HttpPost]

        public async Task<IActionResult> ApiCreate([FromBody] Vacacion vacacion)
        {
            if (vacacion == null || vacacion.Idusuario == 0)
            {
                return Json(new { success = false, message = "Debe seleccionar un empleado válido." });
            }
            if (vacacion.FechaFin < vacacion.FechaInicio)
            {
                return Json(new { success = false, message = "La fecha de fin no puede ser anterior a la de inicio." });
            }
            // Normalizamos las fechas a UTC para la validación de solapamiento
            var inicioUtc = DateTime.SpecifyKind(vacacion.FechaInicio.Date, DateTimeKind.Utc);
            var finUtc = DateTime.SpecifyKind(vacacion.FechaFin.Date, DateTimeKind.Utc);

            // VALIDACIÓN DE SOLAPAMIENTO (No permite que nadie más esté de vacaciones en este rango)
            var solapamiento = await _context.Vacaciones
                .Where(v => inicioUtc <= v.FechaFin && finUtc >= v.FechaInicio)
                .Select(v => new { v.NombreEmpleadoRegistrado, v.FechaInicio, v.FechaFin })
                .FirstOrDefaultAsync();

            if (solapamiento != null)
            {
                return Json(new
                {
                    success = false,
                    message = $"No se puede registrar: {solapamiento.NombreEmpleadoRegistrado} ya tiene vacaciones asignadas del {solapamiento.FechaInicio:dd/MM/yyyy} al {solapamiento.FechaFin:dd/MM/yyyy}."
                });
            }
            try
            {
                int diasRealesTomados = (vacacion.FechaFin - vacacion.FechaInicio).Days + 1;
                vacacion.DiasFavor = Math.Abs(vacacion.DiasVacaciones - diasRealesTomados);
                vacacion.NombreEmpleadoRegistrado = vacacion.NombreEmpleadoFrontend?.Trim() ?? "Desconocido";
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { success = false, message = "Sesión expirada. Por favor inicie sesión nuevamente." });
                }
                vacacion.FechaInicio = DateTime.SpecifyKind(vacacion.FechaInicio.Date, DateTimeKind.Utc);
                vacacion.FechaFin = DateTime.SpecifyKind(vacacion.FechaFin.Date, DateTimeKind.Utc);
                _context.Vacaciones.Add(vacacion);
                await _context.SaveChangesAsync();
                return Json(new
                {
                    success = true,
                    id = vacacion.IdVacaciones,
                    nombreEmpleado = vacacion.NombreEmpleadoRegistrado,
                    diasFavor = vacacion.DiasFavor
                });
            }
            catch (Exception ex)
            {
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

                // 1. Filtro por nombre (Optimizado)

                if (!string.IsNullOrWhiteSpace(nombreEmpleado))

                {

                    query = query.Where(v => EF.Functions.ILike(

                        EF.Functions.Unaccent(v.NombreEmpleadoRegistrado ?? ""),

                        $"%{nombreEmpleado.Trim()}%"

                    ));

                }

                // 2. Filtro por fechas

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

                // 3. Ejecución de la consulta (TRAEMOS EL CAMPO DiasFavor de la BD)

                var datosCrudos = await query

                    .OrderByDescending(v => v.IdVacaciones)

                    .Select(v => new

                    {

                        v.IdVacaciones,

                        v.NombreEmpleadoRegistrado,

                        v.DiasVacaciones,

                        v.FechaInicio,

                        v.FechaFin,

                        v.DiasFavor // ← Traemos el valor real guardado

                    })

                    .ToListAsync();

                // 4. Mapeo final para el Frontend (SIN CÁLCULOS EXTRAS)

                var resultados = datosCrudos.Select(v => new

                {

                    v.IdVacaciones,

                    nombreEmpleado = v.NombreEmpleadoRegistrado ?? "Sin Nombre",

                    v.DiasVacaciones,

                    fechaInicio = v.FechaInicio.ToString("dd/MM/yyyy"),

                    fechaFin = v.FechaFin.ToString("dd/MM/yyyy"),

                    diasFavor = Math.Abs(v.DiasFavor)

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