using FarmaciaSantaRita.Models;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Authorization;

namespace FarmaciaSantaRita.Controllers
{
    // El atributo [Authorize] protege todas las acciones
    [Authorize]
    public class BoletaController : Controller
    {
        private readonly FarmaciabdContext _context;

        public BoletaController(FarmaciabdContext context)
        {
            _context = context;
        }

        // ************************************************
        // * ACCIÓN PRINCIPAL (Index) - Carga la Vista
        // ************************************************
        [HttpGet]
        public IActionResult Index(int idProveedor)
        {
            // 1. Buscar el proveedor por su ID para establecer el contexto
            var proveedor = _context.Proveedors.FirstOrDefault(p => p.Idproveedor == idProveedor);

            if (proveedor == null)
            {
                return NotFound($"Error: Proveedor con ID {idProveedor} no encontrado.");
            }

            // 2. Usar ViewBag para pasar el ID y nombre a la vista.
            ViewBag.IdProveedor = idProveedor;
            ViewBag.NombreProveedor = proveedor.NombreProveedor;

            // 3. Devuelve la vista /Views/Boleta/Index.cshtml (La lista de boletas se cargará
            //    por una llamada AJAX separada a GetBoletas, una vez que la vista esté lista).
            return View();
        }

        // ************************************************
        // * NUEVA ACCIÓN: Obtiene la lista de Boletas (JSON)
        // ************************************************
        [HttpGet]
        public IActionResult GetBoletas(int idProveedor)
        {
            // Asegurarse de que el ID del proveedor es válido
            if (idProveedor <= 0)
            {
                return BadRequest(new { success = false, message = "ID de proveedor inválido." });
            }

            try
            {
                // Obtener solo las boletas del proveedor específico
                var boletas = _context.Boleta
                    .Where(b => b.Idproveedor == idProveedor)
                    // Seleccionar solo las propiedades necesarias y darles formato
                    .Select(b => new
                    {
                        Idboleta = b.Idboleta,
                        // Formateamos la fecha para la vista
                        Fecha = b.Fecha.ToString("dd/MM/yyyy"),
                        ImporteFinal = b.ImporteFinal,
                        Transfer = b.Transfer,
                        Categoria = b.Categoria,
                        Detalle = b.Detalle
                    })
                    .OrderByDescending(b => b.Fecha) // Ordenar por fecha para mejor visualización
                    .ToList();

                // Devolver la lista de boletas en formato JSON
                return Json(new { success = true, data = boletas });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error al obtener boletas: {ex.Message}" });
            }
        }


        // DTO para recibir desde AJAX (Fecha como string) - opcional pero útil
        public class BoletaDto
        {
            public int Idusuario { get; set; }
            public int Idproveedor { get; set; }
            public string Fecha { get; set; } = string.Empty; // "yyyy-MM-dd"
            public int ImporteFinal { get; set; }
            public string Transfer { get; set; } = string.Empty;
            public string Categoria { get; set; } = string.Empty;
            public string? Detalle { get; set; }
        }

        



        // ------------------ CargarExcel: lee Fecha / Importe / Transfer y añade Categoria según el nombre de la hoja ------------------
        [HttpPost]
        public IActionResult CargarExcel(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest("No se seleccionó ningún archivo.");

            try
            {
                var categorias = new List<object>();

                using (var stream = new MemoryStream())
                {
                    archivo.CopyTo(stream);
                    using var package = new ExcelPackage(stream);
                    foreach (var hoja in package.Workbook.Worksheets)
                    {
                        string categoria = hoja.Name;
                        var registros = new List<object>();

                        int filaInicio = 3; // empezamos en fila 3
                        int ultimaFila = hoja.Dimension?.End.Row ?? 0;

                        for (int fila = filaInicio; fila <= ultimaFila; fila++)
                        {
                            string fecha = hoja.Cells[fila, 1].Text;
                            string importe = hoja.Cells[fila, 2].Text;
                            string transfer = hoja.Cells[fila, 3].Text;

                            if (string.IsNullOrWhiteSpace(fecha) &&
                                string.IsNullOrWhiteSpace(importe) &&
                                string.IsNullOrWhiteSpace(transfer))
                                continue;

                            registros.Add(new
                            {
                                Fecha = fecha,
                                Importe = importe,
                                Transfer = transfer
                            });
                        }

                        categorias.Add(new
                        {
                            Categoria = categoria,
                            Datos = registros
                        });
                    }
                }

                return Json(categorias);
            }
            catch (Exception ex)
            {
                return BadRequest("Error leyendo Excel: " + ex.Message);
            }
        }

        // ************************************************
        // * ACCIÓN MODIFICADA: Guardar Boleta (usa DateTime)
        // ************************************************
        [HttpPost]
        public async Task<IActionResult> GuardarBoleta([FromBody] BoletaDto data)
        {
            Console.WriteLine("GuardarBoleta llamado");
            Console.WriteLine($" - IdUsuario: {data.Idusuario}");
            Console.WriteLine($" - IdProveedor: {data.Idproveedor}");
            Console.WriteLine($" - Fecha raw: {data.Fecha}");
            Console.WriteLine($" - Importe: {data.ImporteFinal}");
            Console.WriteLine($" - Transfer: {data.Transfer}");
            Console.WriteLine($" - Categoria: {data.Categoria}");

            try
            {
                if (string.IsNullOrEmpty(data.Fecha) || !DateTime.TryParseExact(data.Fecha, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime fechaParsed))
                {
                    Console.WriteLine("Fecha inválida");
                    return BadRequest(new { exito = false, mensaje = "Fecha inválida" });
                }

                fechaParsed = DateTime.SpecifyKind(fechaParsed, DateTimeKind.Utc);

                var boleta = new Boletum
                {
                    Idusuario = data.Idusuario,
                    Idproveedor = data.Idproveedor,
                    Fecha = fechaParsed,
                    ImporteFinal = data.ImporteFinal,
                    Transfer = data.Transfer,
                    Categoria = data.Categoria,
                    Detalle = data.Detalle ?? "",
                    Eliminado = false
                };

                _context.Boleta.Add(boleta);
                var cambios = await _context.SaveChangesAsync();
                Console.WriteLine($"SaveChanges guardó {cambios} filas. Nuevo ID: {boleta.Idboleta}");

                return Json(new { exito = true, idBoleta = boleta.Idboleta });
            }
            catch (Exception ex)
            {
                Console.WriteLine("EXCEPCIÓN EN GuardarBoleta: " + ex.ToString());
                return Json(new { exito = false, mensaje = ex.Message });
            }
        }










        // Modificar EliminarBoleta para que marque Eliminado = true (soft delete)
        [HttpDelete]
        public async Task<IActionResult> EliminarBoleta(int id)
        {
            try
            {
                var boleta = await _context.Boleta.FindAsync(id);
                if (boleta == null)
                    return Json(new { success = false, message = "La boleta no existe." });

                // ← Comentá o borrá esto si querés permitir eliminar de cualquier proveedor
                // if (boleta.Idproveedor != int.Parse(ViewBag.IdProveedor?.ToString() ?? "0"))
                //     return Unauthorized("No tienes permiso para eliminar esta boleta.");

                boleta.Eliminado = true;           // ← Marca como eliminada (soft delete)
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }





        // Nueva acción: Restaurar boleta (poner Eliminado = false)
        [HttpPost]
        public async Task<IActionResult> RestaurarBoleta(int id)
        {
            try
            {
                var boleta = await _context.Boleta.FindAsync(id);
                if (boleta == null)
                    return Json(new { success = false, message = "La boleta no existe." });

                if (boleta.Idproveedor != int.Parse(ViewBag.IdProveedor?.ToString() ?? "0"))
                    return Unauthorized("No tienes permiso para restaurar esta boleta.");

                boleta.Eliminado = false;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Boleta restaurada correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        // Nueva acción: Obtener boletas eliminadas del proveedor actual
        [HttpGet]
        public IActionResult GetBoletasEliminadas(string idProveedor)  // ← Cambia a string para que acepte query string
        {
            Console.WriteLine($"GetBoletasEliminadas llamado - idProveedor raw: '{idProveedor}'");

            if (!int.TryParse(idProveedor, out int proveedorId) || proveedorId <= 0)
            {
                Console.WriteLine("ID proveedor inválido o no parseable");
                return BadRequest(new { success = false, message = "ID de proveedor inválido." });
            }

            try
            {
                var eliminadas = _context.Boleta
                    .Where(b => b.Idproveedor == proveedorId && b.Eliminado == true)
                    .Select(b => new
                    {
                        Idboleta = b.Idboleta,
                        Fecha = b.Fecha.ToString("dd/MM/yyyy"),
                        ImporteFinal = "$ " + (b.ImporteFinal > 0 ? b.ImporteFinal.ToString("N0") : "0"),
                        Transfer = b.Transfer ?? "No"
                    })
                    .OrderByDescending(b => b.Fecha)
                    .ToList();

                Console.WriteLine($"Eliminadas encontradas para proveedor {proveedorId}: {eliminadas.Count}");

                return Json(new { success = true, data = eliminadas });
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR GRAVE en GetBoletasEliminadas: " + ex.ToString());
                return StatusCode(500, new { success = false, message = "Error interno al consultar boletas eliminadas." });
            }
        }








        // ExportarExcel (igual que antes)...
        [HttpGet]
        public IActionResult ExportarExcel(int idProveedor)
        {
            var datos = _context.Boleta.Where(b => b.Idproveedor == idProveedor).ToList();

            if (!datos.Any())
                return BadRequest("No hay boletas para exportar.");

            var proveedor = _context.Proveedors.FirstOrDefault(p => p.Idproveedor == idProveedor);
            string nombreProveedor = proveedor?.NombreProveedor ?? "Proveedor";
            string fechaActual = DateTime.Now.ToString("dd-MM-yyyy HH-mm");
            string nombreArchivo = $"{nombreProveedor} {fechaActual}.xlsx";

            ExcelPackage.License.SetNonCommercialOrganization("Drogueria Suiza");

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Boletas");

            ws.Cells["A1"].Value = "Fecha";
            ws.Cells["B1"].Value = "Importe";
            ws.Cells["C1"].Value = "Transfer";
            ws.Cells["A1:C1"].Style.Font.Bold = true;

            int row = 2;
            int conTransfer = 0;
            int sinTransfer = 0;

            foreach (var b in datos)
            {
                ws.Cells[row, 1].Value = b.Fecha.ToString("dd/MM/yyyy");
                ws.Cells[row, 2].Value = b.ImporteFinal;
                ws.Cells[row, 3].Value = b.Transfer;

                if (b.Transfer?.ToUpper() == "SI")
                    conTransfer++;
                else
                    sinTransfer++;

                row++;
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            ws.Cells["E2"].Value = "Resumen Transfer";
            ws.Cells["E2"].Style.Font.Bold = true;
            ws.Cells["E3"].Value = "Con Transfer (SI):";
            ws.Cells["F3"].Value = conTransfer;
            ws.Cells["E4"].Value = "Sin Transfer (NO):";
            ws.Cells["F4"].Value = sinTransfer;

            var bytes = package.GetAsByteArray();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombreArchivo);
        }
    }
}