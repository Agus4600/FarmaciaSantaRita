using FarmaciaSantaRita.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

public class InasistenciaController : Controller
{
    private readonly FarmaciabdContext _context;

    public InasistenciaController(FarmaciabdContext context)
    {
        _context = context;
    }

    // DTO que ahora refleja los campos que el JS necesita enviar
    public class InasistenciaRegistro
    {
        public int IdUsuario { get; set; }
        public int IdEmpleado { get; set; }
        public string NombreEmpleado { get; set; } = null!;
        public string Motivo { get; set; } = null!;
        public string Turno { get; set; } = null!;
        public string Fecha { get; set; } = null!; // La fecha en formato YYYY-MM-DD
    }

    //"~/Views/Inasistencia/Inasistencia.cshtml"//

    [HttpGet]
    public async Task<IActionResult> GetFaltasPorTurno()
    {
        var conteos = await _context.Inasistencia
            .GroupBy(i => i.Turno)
            .Select(g => new
            {
                turno = g.Key ?? "Sin turno",
                total = g.Count()
            })
            .ToListAsync();

        // Forzamos los 3 turnos siempre
        var resultado = new[]
        {
        new { turno = "Mañana", total = conteos.FirstOrDefault(x => x.turno == "Mañana")?.total ?? 0 },
        new { turno = "Tarde", total = conteos.FirstOrDefault(x => x.turno == "Tarde")?.total ?? 0 },
        new { turno = "Completo", total = conteos.FirstOrDefault(x => x.turno == "Completo")?.total ?? 0 }
    };

        return Json(resultado);
    }









    [HttpGet]
    public IActionResult Inasistencia(string origen = null, int idProveedor = 0)
    {
        // ESTA ES LA PRUEBA QUE ACABA CON TODO
        var todos = _context.Usuarios
            .Where(u => u.Eliminado == false)
            .Select(u => new { u.Idusuario, u.Nombre, u.Apellido, RolExacto = "[" + (u.Rol ?? "NULL") + "]" })
            .ToList();

        ViewBag.TodosLosUsuarios = todos;

        // FORZAMOS QUE TRAIGA A LOS EMPLEADOS POR ID DIRECTO (los que tú sabes que existen)
        // CARGAR TODOS LOS EMPLEADOS Y JEFES (igual que en Vacaciones)
        ViewBag.Empleados = _context.Usuarios
        .Where(u => u.Rol == "Empleado/a" || u.Rol == "Jefe/a")
        .OrderBy(u => u.Apellido)
        .ThenBy(u => u.Nombre)
        .Select(u => new
        {
            u.Idusuario,
            NombreCompleto = u.Nombre + " " + u.Apellido,
            Dni = u.Dni
        })
        .ToList();

        ViewData["IdProveedor"] = idProveedor;
        return View();
    }



    [HttpGet]
    public async Task<IActionResult> GetDatosEmpleado(int id) // Cambiado de string nombre a int id
    {
        if (id <= 0)
            return Json(new { exito = false, message = "ID no válido" });

        var empleado = await _context.Usuarios
            .Where(u => u.Idusuario == id)
            .Select(u => new
            {
                exito = true, // Agregamos esta propiedad para que el JS la reconozca
                nombreCompleto = u.Nombre + " " + u.Apellido,
                dni = u.Dni,
                telefono = u.Telefono,
                direccion = u.Direccion
            })
            .FirstOrDefaultAsync();

        if (empleado == null)
            return Json(new { exito = false, message = "Empleado no encontrado" });

        return Json(empleado);
    }





    [HttpGet]
    public IActionResult BuscarInasistencias([FromQuery] string? fechaDesde,
                                          [FromQuery] string? fechaHasta,
                                          [FromQuery] string? nombreEmpleado,
                                          [FromQuery] string? turno)
    {
        DateTime? dtDesde = null;
        if (!string.IsNullOrEmpty(fechaDesde) && DateTime.TryParse(fechaDesde, out DateTime parsedDesde))
            dtDesde = parsedDesde.Date;

        DateTime? dtHasta = null;
        if (!string.IsNullOrEmpty(fechaHasta) && DateTime.TryParse(fechaHasta, out DateTime parsedHasta))
            dtHasta = parsedHasta.Date.AddDays(1).AddSeconds(-1);

        try
        {
            var consulta = _context.Inasistencia.AsQueryable();

            if (dtDesde.HasValue && dtHasta.HasValue)
            {
                DateOnly desde = DateOnly.FromDateTime(dtDesde.Value);
                DateOnly hasta = DateOnly.FromDateTime(dtHasta.Value.Date);
                consulta = consulta.Where(i => i.FechaInasistencia >= desde && i.FechaInasistencia <= hasta);
            }

            if (!string.IsNullOrWhiteSpace(turno))
            {
                consulta = consulta.Where(i => i.Turno == turno);
            }

            // TRAEMOS TODOS LOS DATOS (es rápido, son pocos)
            var resultados = consulta
                .OrderByDescending(i => i.FechaInasistencia)
                .Select(i => new
                {
                    IdInasistencia = i.Idinasistencias,
                    Fecha = i.FechaInasistencia.ToString("yyyy-MM-dd"),
                    NombreEmpleado = i.NombreEmpleado,
                    Turno = i.Turno,
                    Motivo = i.Motivo,
                    IdEmpleado = i.Idusuario
                })
                .ToList(); // Aquí ya está en memoria → podemos usar cualquier función

            // AHORA SÍ: filtro por nombre con acentos ignorados
            if (!string.IsNullOrWhiteSpace(nombreEmpleado))
            {
                var textoBuscado = nombreEmpleado.ToLower();
                resultados = resultados
                    .Where(r => (r.NombreEmpleado ?? "")
                        .Normalize(System.Text.NormalizationForm.FormD)
                        .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) !=
                                    System.Globalization.UnicodeCategory.NonSpacingMark)
                        .Aggregate("", (current, c) => current + c)
                        .Normalize(System.Text.NormalizationForm.FormC)
                        .ToLower()
                        .Contains(textoBuscado))
                    .ToList();
            }

            return Json(resultados);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, mensaje = "Error: " + ex.Message });
        }
    }



    [HttpGet]
    [Authorize(Roles = "Jefe/a, Empleado/a")]
    public IActionResult GetDbInfo(int idProveedor)
    {
        var lista = _context.Inasistencia
            .OrderByDescending(x => x.FechaInasistencia)
            .Select(x => new {
                idInasistencia = x.Idinasistencias,
                fecha = x.FechaInasistencia.ToString("yyyy-MM-dd"),
                nombreEmpleado = x.NombreEmpleado,
                turno = x.Turno,
                motivo = x.Motivo,
                idEmpleado = x.Idusuario
            })
            .ToList();

        return Json(lista);
    }






    // ----------------------------------------------------
    // ACCIÓN POST: Registro de Inasistencia 
    // ----------------------------------------------------
    [HttpPost]
    [Authorize(Roles = "Jefe/a")]
    public IActionResult RegistrarInasistencia([FromBody] InasistenciaRegistro model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage ?? e.Exception?.Message).Where(m => !string.IsNullOrEmpty(m)).ToList();
            string mensajeError = errors.Any() ? "Datos incompletos o inválidos (ModelState): " + string.Join("; ", errors) : "Datos incompletos o inválidos.";
            return Json(new { exito = false, mensaje = mensajeError });
        }

        try
        {
            // 1. Conversión de fecha
            if (!DateTime.TryParse(model.Fecha, out DateTime fechaDateTime))
            {
                return Json(new { exito = false, mensaje = "Formato de fecha inválido." });
            }
            DateOnly fechaInasistencia = DateOnly.FromDateTime(fechaDateTime);
            int idEmpleadoFaltante = model.IdEmpleado;

            // 2. Verificación de la existencia del ID (Regla de Clave Foránea)
            var usuarioExiste = _context.Usuarios.Any(u => u.Idusuario == idEmpleadoFaltante);
            if (!usuarioExiste)
            {
                // Este mensaje es CLARO sobre la causa.
                return Json(new { exito = false, mensaje = $"ERROR DE REGLA DE DB (FK): El ID de empleado ({idEmpleadoFaltante}) no existe en la tabla Usuarios. No se puede registrar." });
            }

            // 3. Creación del objeto
            var nuevaInasistencia = new Inasistencium
            {
                Idusuario = idEmpleadoFaltante,
                NombreEmpleado = model.NombreEmpleado,
                Motivo = model.Motivo,
                FechaInasistencia = fechaInasistencia,
                Turno = model.Turno
            };

            // 4. Guardar en DB
            _context.Inasistencia.Add(nuevaInasistencia);
            _context.SaveChanges(); // 🚨 ESTE MÉTODO LANZA LA EXCEPCIÓN SI FALLA LA DB

            // 5. Respuesta exitosa (Devuelve el ID generado para confirmación irrefutable)
            return Json(new { exito = true, idInasistencia = nuevaInasistencia.Idinasistencias, mensaje = $"Registro guardado. ID generado por DB: {nuevaInasistencia.Idinasistencias}" });
        }
        catch (Exception ex)
        {
            // 🚨 CAMBIO CLAVE: Búsqueda agresiva del mensaje de error más profundo.
            string dbError = ex.Message;

            // Desenrollamos la excepción para encontrar el mensaje específico de SQL Server.
            Exception inner = ex.InnerException;
            while (inner != null)
            {
                dbError = inner.Message;
                inner = inner.InnerException;
            }

            return Json(new { exito = false, mensaje = "ERROR DE PERSISTENCIA DB: " + dbError });
        }
    }


    // ----------------------------------------------------
    // ACCIÓN DELETE: EliminarInasistencia
    // ----------------------------------------------------
    [HttpDelete]
    [Authorize(Roles = "Jefe/a")]
    public IActionResult EliminarInasistencia([FromQuery] int id) // Se recibe por query string
    {
        try
        {
            var inasistencia = _context.Inasistencia.Find(id);
            if (inasistencia == null)
            {
                return Json(new { success = false, mensaje = "Registro no encontrado." });
            }

            _context.Inasistencia.Remove(inasistencia);
            _context.SaveChanges();

            return Json(new { success = true, mensaje = "Registro eliminado correctamente." });
        }
        catch (Exception ex)
        {
            string errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            return Json(new { success = false, mensaje = "Error al eliminar: " + errorMessage });
        }
    }
}